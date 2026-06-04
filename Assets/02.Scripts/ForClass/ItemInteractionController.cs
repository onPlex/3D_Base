using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 마우스 포인터 ScreenPointToRay로 InteractableItem을 감지하고, 플레이어 근접 거리 내에서만 E키 획득을 허용합니다.
/// InventoryPanel만 UI Ray 차단, 타겟 hysteresis로 깜빡임을 완화합니다.
/// </summary>
public class ItemInteractionController : MonoBehaviour
{
    [Header("RayCast 설정")]
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float raycastMaxDistance = 30.0f;
    [SerializeField] private float interactionRange = 3.0f;
    [SerializeField] private LayerMask hitLayerMask = ~0;
    [SerializeField] private QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore;
    [SerializeField] private bool skipPlayerColliders = true;

    [Header("타겟 안정화")]
    [SerializeField] private int targetStableFrames = 2;
    [SerializeField] private int targetReleaseDelayFrames = 3;

    [Header("UI Ray 차단 (Inventory만)")]
    [SerializeField] private RectTransform[] blockingUiRoots;

    [Header("연결 컴포넌트")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private ItemInteractionUI interactionUI;
    [SerializeField] private ItemInteractionPlayMonitor playMonitor;

    [Header("시각화 - LineRenderer")]
    [SerializeField] private bool showRayLine = true;
    [SerializeField] private float rayLineWidth = 0.03f;

    [Header("시각화 - 아이템 하이라이트")]
    [SerializeField] private bool showItemHighlight = true;
    [SerializeField] private Color highlightPickupColor = new Color(0.2f, 1.0f, 0.3f, 1.0f);
    [SerializeField] private Color highlightBlockedColor = new Color(1.0f, 0.25f, 0.2f, 1.0f);

    [Header("시각화 - Pointer Reticle")]
    [SerializeField] private Graphic pointerReticle;
    [SerializeField] private Color reticleDefaultColor = new Color(1.0f, 1.0f, 1.0f, 0.6f);
    [SerializeField] private Color reticlePickupColor = new Color(0.2f, 1.0f, 0.3f, 0.9f);
    [SerializeField] private Color reticleBlockedColor = new Color(1.0f, 0.25f, 0.2f, 0.9f);
    [SerializeField] private Color reticleOutOfRangeColor = new Color(1.0f, 0.55f, 0.2f, 0.85f);

    [Header("디버그")]
    [SerializeField] private bool drawDebugRay = true;
    [SerializeField] private Color debugHitColor = Color.green;
    [SerializeField] private Color debugMissColor = Color.yellow;
    [SerializeField] private float debugRayDuration = 0.1f;

    private InteractableItem currentItem;
    private InteractableItem rayTargetItem;
    private InteractionTargetState currentTargetState = InteractionTargetState.None;

    private InteractableItem rawTargetItem;
    private InteractionTargetState rawTargetState = InteractionTargetState.None;
    private InteractableItem stableTargetItem;
    private InteractionTargetState stableTargetState = InteractionTargetState.None;
    private InteractableItem pendingTargetItem;
    private InteractionTargetState pendingTargetState = InteractionTargetState.None;
    private int pendingFrameCount;
    private int framesWithoutTarget;

    private InteractableItem previousHighlightedItem;
    private InteractionTargetState previousHighlightState = InteractionTargetState.None;
    private Renderer highlightedRenderer;
    private LineRenderer rayLine;
    private RectTransform reticleRectTransform;
    private Canvas reticleCanvas;
    private MaterialPropertyBlock highlightPropertyBlock;
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
    private PointerEventData pointerEventData;
    private string lastLoggedSuspendReason;

    private void Awake()
    {
        highlightPropertyBlock = new MaterialPropertyBlock();

        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }

        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }

        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
        }

        if (interactionUI == null)
        {
            interactionUI = FindFirstObjectByType<ItemInteractionUI>();
        }

        if (playMonitor == null)
        {
            playMonitor = GetComponent<ItemInteractionPlayMonitor>();
        }

        if (playMonitor == null)
        {
            playMonitor = FindFirstObjectByType<ItemInteractionPlayMonitor>();
        }

        if (blockingUiRoots == null || blockingUiRoots.Length == 0)
        {
            TryAssignBlockingUiFromInteractionUi();
        }

        if (pointerReticle != null)
        {
            reticleRectTransform = pointerReticle.rectTransform;
            reticleCanvas = pointerReticle.canvas != null
                ? pointerReticle.canvas
                : pointerReticle.GetComponentInParent<Canvas>();
        }

        SetupRayLine();
    }

    private void TryAssignBlockingUiFromInteractionUi()
    {
        if (interactionUI == null || interactionUI.InventoryPanelRect == null)
        {
            return;
        }

        blockingUiRoots = new[] { interactionUI.InventoryPanelRect };
    }

    private void OnValidate()
    {
        raycastMaxDistance = Mathf.Max(0.1f, raycastMaxDistance);
        interactionRange = Mathf.Max(0.1f, interactionRange);
        debugRayDuration = Mathf.Max(0.0f, debugRayDuration);
        rayLineWidth = Mathf.Max(0.001f, rayLineWidth);
        targetStableFrames = Mathf.Max(1, targetStableFrames);
        targetReleaseDelayFrames = Mathf.Max(0, targetReleaseDelayFrames);
    }

    private void OnDisable()
    {
        ClearHighlight();
        SetRayLineVisible(false);
        SetPointerReticleVisible(false);
        ResetTargetStability();
    }

    private void Update()
    {
        if (ShouldSuspendInteraction(out string suspendReason))
        {
            LogSuspendedIfNeeded(suspendReason);
            ClearInteractionState();
            playMonitor?.LogRayVerbose(
                InteractionTargetState.None,
                InteractionTargetState.None,
                null,
                suspendReason.Contains("Ui"),
                IsCameraLookHeld());
            return;
        }

        lastLoggedSuspendReason = string.Empty;

        DetectTarget(
            out InteractableItem detectedItem,
            out RaycastHit? itemHit,
            out InteractionTargetState targetState);

        rawTargetItem = detectedItem;
        rawTargetState = targetState;

        UpdateTargetStability(detectedItem, targetState);

        currentTargetState = stableTargetState;
        rayTargetItem = stableTargetItem;
        currentItem = IsInteractableState(stableTargetState) ? stableTargetItem : null;

        UpdateVisualFeedback(stableTargetItem, stableTargetState, itemHit);
        interactionUI?.UpdateDisplay(stableTargetItem, stableTargetState);
        TryPickupOnInput();

        playMonitor?.LogRayVerbose(
            rawTargetState,
            stableTargetState,
            rawTargetItem,
            false,
            IsCameraLookHeld());
    }

    /// <summary>
    /// 우클릭 시점 회전 중이거나 Inventory UI 위 포인터일 때 월드 상호작용을 일시 중단합니다.
    /// </summary>
    private bool ShouldSuspendInteraction(out string suspendReason)
    {
        if (IsCameraLookHeld())
        {
            suspendReason = "CameraLook";
            return true;
        }

        if (TryGetBlockingUiHitObjectName(out string hitObjectName))
        {
            suspendReason = "UiBlock";
            playMonitor?.LogUiBlock(hitObjectName);
            return true;
        }

        suspendReason = string.Empty;
        return false;
    }

    private void LogSuspendedIfNeeded(string suspendReason)
    {
        if (string.IsNullOrEmpty(suspendReason) || suspendReason == lastLoggedSuspendReason)
        {
            return;
        }

        lastLoggedSuspendReason = suspendReason;
        playMonitor?.LogSuspended(suspendReason);
    }

    private void ClearInteractionState()
    {
        rawTargetItem = null;
        rawTargetState = InteractionTargetState.None;
        ResetTargetStability();

        currentTargetState = InteractionTargetState.None;
        rayTargetItem = null;
        currentItem = null;

        ClearHighlight();
        interactionUI?.Hide();
        SetRayLineVisible(false);
        SetPointerReticleVisible(!IsCameraLookHeld());

        if (pointerReticle != null && pointerReticle.enabled)
        {
            pointerReticle.color = reticleDefaultColor;
            UpdatePointerReticlePosition();
        }
    }

    private void ResetTargetStability()
    {
        pendingTargetItem = null;
        pendingTargetState = InteractionTargetState.None;
        pendingFrameCount = 0;
        framesWithoutTarget = 0;
        stableTargetItem = null;
        stableTargetState = InteractionTargetState.None;
    }

    /// <summary>
    /// raw 타겟을 N프레임 안정화하고, Miss 시 release delay 후 해제합니다.
    /// </summary>
    private void UpdateTargetStability(InteractableItem rawItem, InteractionTargetState rawState)
    {
        InteractionTargetState previousStableState = stableTargetState;
        InteractableItem previousStableItem = stableTargetItem;

        bool hasRawTarget = rawItem != null && rawState != InteractionTargetState.None;

        if (hasRawTarget)
        {
            framesWithoutTarget = 0;

            if (rawItem == pendingTargetItem && rawState == pendingTargetState)
            {
                pendingFrameCount++;
            }
            else
            {
                pendingTargetItem = rawItem;
                pendingTargetState = rawState;
                pendingFrameCount = 1;
            }

            if (pendingFrameCount >= targetStableFrames)
            {
                stableTargetItem = pendingTargetItem;
                stableTargetState = pendingTargetState;
            }
        }
        else
        {
            pendingTargetItem = null;
            pendingTargetState = InteractionTargetState.None;
            pendingFrameCount = 0;
            framesWithoutTarget++;

            if (framesWithoutTarget >= targetReleaseDelayFrames)
            {
                stableTargetItem = null;
                stableTargetState = InteractionTargetState.None;
            }
        }

        if (stableTargetItem != previousStableItem || stableTargetState != previousStableState)
        {
            float distance = stableTargetItem != null
                ? GetDistanceToPlayer(stableTargetItem)
                : 0.0f;
            playMonitor?.LogTargetChanged(
                stableTargetItem,
                rawState,
                stableTargetState,
                distance);
        }
    }

    private float GetDistanceToPlayer(InteractableItem item)
    {
        if (item == null || playerTransform == null)
        {
            return 0.0f;
        }

        return Vector3.Distance(playerTransform.position, item.transform.position);
    }

    /// <summary>
    /// 마우스 스크린 좌표에서 ScreenPointToRay를 쏘고 InteractableItem과 상호작용 상태를 판정합니다.
    /// </summary>
    private void DetectTarget(
        out InteractableItem detectedItem,
        out RaycastHit? itemHit,
        out InteractionTargetState targetState)
    {
        detectedItem = null;
        itemHit = null;
        targetState = InteractionTargetState.None;

        if (interactionCamera == null)
        {
            return;
        }

        Ray ray = BuildPointerRay();
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            raycastMaxDistance,
            hitLayerMask,
            queryTriggerInteraction);

        if (hits.Length == 0)
        {
            DrawDebugRay(ray, null, InteractionTargetState.None);
            return;
        }

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int index = 0; index < hits.Length; index++)
        {
            if (ShouldSkipCollider(hits[index].collider))
            {
                continue;
            }

            InteractableItem item = hits[index].collider.GetComponentInParent<InteractableItem>();
            if (item == null)
            {
                continue;
            }

            detectedItem = item;
            itemHit = hits[index];
            targetState = EvaluateTargetState(item);
            DrawDebugRay(ray, hits[index], targetState);
            return;
        }

        DrawDebugRay(ray, null, InteractionTargetState.None);
    }

    private Ray BuildPointerRay()
    {
        Vector2 pointerPosition = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        return interactionCamera.ScreenPointToRay(pointerPosition);
    }

    /// <summary>
    /// blockingUiRoots(InventoryPanel 등) 위에 포인터가 있는지 GraphicRaycaster로 검사합니다.
    /// InteractionPanel은 raycastTarget=false이므로 여기 걸리지 않습니다.
    /// </summary>
    private bool TryGetBlockingUiHitObjectName(out string hitObjectName)
    {
        hitObjectName = null;

        if (blockingUiRoots == null || blockingUiRoots.Length == 0 || EventSystem.current == null)
        {
            return false;
        }

        if (pointerEventData == null)
        {
            pointerEventData = new PointerEventData(EventSystem.current);
        }

        pointerEventData.position = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;

        for (int rootIndex = 0; rootIndex < blockingUiRoots.Length; rootIndex++)
        {
            RectTransform root = blockingUiRoots[rootIndex];
            if (root == null)
            {
                continue;
            }

            GraphicRaycaster raycaster = root.GetComponentInParent<GraphicRaycaster>();
            if (raycaster == null)
            {
                continue;
            }

            uiRaycastResults.Clear();
            raycaster.Raycast(pointerEventData, uiRaycastResults);

            for (int hitIndex = 0; hitIndex < uiRaycastResults.Count; hitIndex++)
            {
                Transform hitTransform = uiRaycastResults[hitIndex].gameObject.transform;
                if (hitTransform == root || hitTransform.IsChildOf(root))
                {
                    hitObjectName = uiRaycastResults[hitIndex].gameObject.name;
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsCameraLookHeld()
    {
        return Mouse.current != null && Mouse.current.rightButton.isPressed;
    }

    private bool ShouldSkipCollider(Collider collider)
    {
        if (!skipPlayerColliders)
        {
            return false;
        }

        if (collider.CompareTag("Player"))
        {
            return true;
        }

        if (playerTransform != null && collider.transform.IsChildOf(playerTransform))
        {
            return true;
        }

        return collider.GetComponent<CharacterController>() != null;
    }

    private InteractionTargetState EvaluateTargetState(InteractableItem item)
    {
        if (item == null || item.itemData == null)
        {
            return InteractionTargetState.None;
        }

        if (playerTransform == null)
        {
            return item.itemData.canPickup
                ? InteractionTargetState.InRangeCanPickup
                : InteractionTargetState.InRangeBlocked;
        }

        float distanceToPlayer = Vector3.Distance(playerTransform.position, item.transform.position);
        if (distanceToPlayer > interactionRange)
        {
            return InteractionTargetState.OutOfRange;
        }

        return item.itemData.canPickup
            ? InteractionTargetState.InRangeCanPickup
            : InteractionTargetState.InRangeBlocked;
    }

    private static bool IsInteractableState(InteractionTargetState state)
    {
        return state == InteractionTargetState.InRangeCanPickup
            || state == InteractionTargetState.InRangeBlocked;
    }

    private void UpdateVisualFeedback(
        InteractableItem detectedItem,
        InteractionTargetState targetState,
        RaycastHit? itemHit)
    {
        UpdateRayLine(detectedItem, targetState, itemHit);
        UpdateItemHighlight(detectedItem, targetState);
        UpdatePointerReticle(targetState);
    }

    private void SetupRayLine()
    {
        if (!showRayLine)
        {
            return;
        }

        GameObject lineObject = new GameObject("RayLineRenderer");
        lineObject.transform.SetParent(transform, false);

        rayLine = lineObject.AddComponent<LineRenderer>();
        rayLine.useWorldSpace = true;
        rayLine.positionCount = 2;
        rayLine.widthMultiplier = rayLineWidth;
        rayLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rayLine.receiveShadows = false;
        rayLine.numCapVertices = 4;

        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader == null)
        {
            lineShader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (lineShader != null)
        {
            rayLine.material = new Material(lineShader);
        }
    }

    private void UpdateRayLine(
        InteractableItem detectedItem,
        InteractionTargetState targetState,
        RaycastHit? itemHit)
    {
        if (rayLine == null || !showRayLine || interactionCamera == null)
        {
            return;
        }

        Ray ray = BuildPointerRay();
        Vector3 origin = ray.origin;
        Vector3 endPoint = itemHit.HasValue
            ? itemHit.Value.point
            : origin + ray.direction * raycastMaxDistance;

        Color lineColor = GetRayColor(targetState);

        rayLine.enabled = true;
        rayLine.SetPosition(0, origin);
        rayLine.SetPosition(1, endPoint);
        rayLine.startColor = lineColor;
        rayLine.endColor = lineColor;
    }

    private void UpdateItemHighlight(InteractableItem detectedItem, InteractionTargetState targetState)
    {
        if (highlightPropertyBlock == null)
        {
            return;
        }

        if (!showItemHighlight || !IsInteractableState(targetState))
        {
            ClearHighlight();
            return;
        }

        if (detectedItem == previousHighlightedItem && targetState == previousHighlightState)
        {
            return;
        }

        ClearHighlight();

        if (detectedItem == null)
        {
            return;
        }

        Renderer targetRenderer = detectedItem.GetComponentInChildren<Renderer>();
        if (targetRenderer == null)
        {
            return;
        }

        bool canPickup = detectedItem.itemData != null && detectedItem.itemData.canPickup;
        Color tintColor = canPickup ? highlightPickupColor : highlightBlockedColor;

        targetRenderer.GetPropertyBlock(highlightPropertyBlock);
        highlightPropertyBlock.SetColor("_BaseColor", tintColor);
        highlightPropertyBlock.SetColor("_Color", tintColor);
        targetRenderer.SetPropertyBlock(highlightPropertyBlock);

        previousHighlightedItem = detectedItem;
        previousHighlightState = targetState;
        highlightedRenderer = targetRenderer;
    }

    private void ClearHighlight()
    {
        if (highlightedRenderer != null)
        {
            highlightedRenderer.SetPropertyBlock(null);
            highlightedRenderer = null;
        }

        previousHighlightedItem = null;
        previousHighlightState = InteractionTargetState.None;
    }

    private void UpdatePointerReticle(InteractionTargetState targetState)
    {
        if (pointerReticle == null || reticleRectTransform == null)
        {
            return;
        }

        if (IsCameraLookHeld())
        {
            SetPointerReticleVisible(false);
            return;
        }

        SetPointerReticleVisible(true);
        pointerReticle.color = GetReticleColor(targetState);
        UpdatePointerReticlePosition();
    }

    private void UpdatePointerReticlePosition()
    {
        if (reticleCanvas != null && reticleRectTransform != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                reticleCanvas.transform as RectTransform,
                Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero,
                reticleCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : reticleCanvas.worldCamera,
                out Vector2 localPoint))
        {
            reticleRectTransform.anchoredPosition = localPoint;
        }
    }

    private void SetPointerReticleVisible(bool isVisible)
    {
        if (pointerReticle != null)
        {
            pointerReticle.enabled = isVisible;
        }
    }

    private void SetRayLineVisible(bool isVisible)
    {
        if (rayLine != null)
        {
            rayLine.enabled = isVisible;
        }
    }

    private Color GetRayColor(InteractionTargetState targetState)
    {
        switch (targetState)
        {
            case InteractionTargetState.InRangeCanPickup:
                return debugHitColor;
            case InteractionTargetState.InRangeBlocked:
                return highlightBlockedColor;
            case InteractionTargetState.OutOfRange:
                return reticleOutOfRangeColor;
            default:
                return debugMissColor;
        }
    }

    private Color GetReticleColor(InteractionTargetState targetState)
    {
        switch (targetState)
        {
            case InteractionTargetState.InRangeCanPickup:
                return reticlePickupColor;
            case InteractionTargetState.InRangeBlocked:
                return reticleBlockedColor;
            case InteractionTargetState.OutOfRange:
                return reticleOutOfRangeColor;
            default:
                return reticleDefaultColor;
        }
    }

    private void DrawDebugRay(Ray ray, RaycastHit? itemHit, InteractionTargetState targetState)
    {
        if (!drawDebugRay)
        {
            return;
        }

        Vector3 endPoint = itemHit.HasValue
            ? itemHit.Value.point
            : ray.origin + ray.direction * raycastMaxDistance;

        Debug.DrawLine(ray.origin, endPoint, GetRayColor(targetState), debugRayDuration);
    }

    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1.0f, 0.35f);
            Gizmos.DrawWireSphere(playerTransform.position, interactionRange);
        }

        if (currentItem != null)
        {
            Gizmos.color = currentTargetState == InteractionTargetState.InRangeCanPickup
                ? Color.green
                : Color.red;
            Gizmos.DrawWireCube(currentItem.transform.position, Vector3.one * 0.35f);
        }
    }

    private void TryPickupOnInput()
    {
        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
        {
            return;
        }

        if (currentTargetState != InteractionTargetState.InRangeCanPickup || currentItem == null)
        {
            playMonitor?.LogPickup(currentItem, false, $"state={currentTargetState}");
            return;
        }

        if (currentItem.itemData == null)
        {
            Debug.LogWarning("[ItemInteractionController] InteractableItem에 ItemData가 연결되지 않았습니다.", currentItem);
            playMonitor?.LogPickup(currentItem, false, "missing ItemData");
            return;
        }

        if (inventory == null)
        {
            Debug.LogWarning("[ItemInteractionController] Inventory가 연결되지 않았습니다.", this);
            playMonitor?.LogPickup(currentItem, false, "missing Inventory");
            return;
        }

        inventory.AddItem(currentItem.itemData, playMonitor);
        playMonitor?.LogPickup(currentItem, true, "added to inventory");

        ClearHighlight();
        currentItem.gameObject.SetActive(false);
        currentItem = null;
        rayTargetItem = null;
        rawTargetItem = null;
        currentTargetState = InteractionTargetState.None;
        ResetTargetStability();
        interactionUI?.Hide();
        UpdatePointerReticle(InteractionTargetState.None);
        UpdateRayLine(null, InteractionTargetState.None, null);
    }
}
