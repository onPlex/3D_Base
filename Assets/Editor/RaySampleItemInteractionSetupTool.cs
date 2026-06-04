using System.IO;
using System.Reflection;
using StarterAssets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Creates the ScriptableObject + RayCast item interaction answer in RaySample.unity.
/// </summary>
[InitializeOnLoad]
public static class RaySampleItemInteractionSetupTool
{
    private const string SetupPendingMarkerPath = "Assets/01.Scenes/RaySample/.item_interaction_setup_pending";
    private const string RaySampleScenePath = "Assets/01.Scenes/RaySample.unity";
    private const string ItemDataFolder = "Assets/01.Scenes/RaySample/ItemData";

    private const string MaterialLitPath = "Assets/01.Scenes/RaySample/Lit.mat";
    private const string MaterialWhitePath = "Assets/01.Scenes/RaySample/White.mat";
    private const string MaterialBlackPath = "Assets/01.Scenes/RaySample/Black.mat";
    private const string MaterialGoldPath = "Assets/01.Scenes/RaySample/Gold.mat";
    private const string EventSystemPrefabPath =
        "Assets/StarterAssets/Mobile/Prefabs/EventSystem/UI_EventSystem.prefab";

    static RaySampleItemInteractionSetupTool()
    {
        EditorApplication.delayCall += TryRunPendingSetup;
    }

    private static void TryRunPendingSetup()
    {
        if (!File.Exists(SetupPendingMarkerPath))
        {
            return;
        }

        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += TryRunPendingSetup;
            return;
        }

        File.Delete(SetupPendingMarkerPath);
        SetupRaySampleItemInteractionInternal(promptBeforeOpeningScene: false);
    }

    [MenuItem("Tools/ForClass/Setup RaySample Item Interaction")]
    public static void SetupRaySampleItemInteraction()
    {
        SetupRaySampleItemInteractionInternal(promptBeforeOpeningScene: true);
    }

    public static void SetupRaySampleItemInteractionBatch()
    {
        SetupRaySampleItemInteractionInternal(promptBeforeOpeningScene: false);
        EditorApplication.Exit(0);
    }

    private static void SetupRaySampleItemInteractionInternal(bool promptBeforeOpeningScene)
    {
        EnsureFolderPath(ItemDataFolder);

        ItemData healingPotion = GetOrCreateItemData(
            "ItemData_HealingPotion",
            "작은 회복 포션",
            "체력을 조금 회복할 수 있는 붉은 포션입니다.",
            ItemType.Potion,
            true);

        ItemData oldSword = GetOrCreateItemData(
            "ItemData_OldSword",
            "낡은 검",
            "오래 사용한 검입니다. 기본적인 공격에 사용할 수 있습니다.",
            ItemType.Weapon,
            true);

        ItemData dungeonKey = GetOrCreateItemData(
            "ItemData_DungeonKey",
            "던전 열쇠",
            "특정 문을 열 수 있는 녹슨 열쇠입니다.",
            ItemType.Key,
            true);

        ItemData questScroll = GetOrCreateItemData(
            "ItemData_QuestScroll",
            "퀘스트 두루마리",
            "오래된 글씨가 적힌 두루마리입니다. 퀘스트 진행에 필요합니다.",
            ItemType.QuestItem,
            true);

        ItemData sealedRelic = GetOrCreateItemData(
            "ItemData_SealedRelic",
            "봉인된 유물",
            "강력한 마력으로 봉인되어 있어 지금은 획득할 수 없습니다.",
            ItemType.QuestItem,
            false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (promptBeforeOpeningScene && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EditorSceneManager.OpenScene(RaySampleScenePath, OpenSceneMode.Single);

        RemoveExistingSetupRoot("InteractableItems");
        RemoveExistingSetupRoot("ItemInteractionSystem");
        RemoveExistingSetupRoot("UI");
        RemoveExistingEventSystem();

        Material litMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialLitPath);
        Material whiteMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialWhitePath);
        Material blackMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialBlackPath);
        Material goldMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialGoldPath);

        GameObject itemsRoot = new GameObject("InteractableItems");

        CreateInteractableItem(
            itemsRoot.transform,
            "Item_HealingPotion",
            PrimitiveType.Capsule,
            new Vector3(9.5f, 0.5f, -1.0f),
            Vector3.one,
            healingPotion,
            litMaterial);

        CreateInteractableItem(
            itemsRoot.transform,
            "Item_OldSword",
            PrimitiveType.Cube,
            new Vector3(8.5f, 0.5f, 0.5f),
            new Vector3(0.2f, 0.2f, 1.0f),
            oldSword,
            whiteMaterial);

        CreateInteractableItem(
            itemsRoot.transform,
            "Item_DungeonKey",
            PrimitiveType.Cube,
            new Vector3(6.5f, 0.3f, -0.5f),
            new Vector3(0.3f, 0.3f, 0.6f),
            dungeonKey,
            goldMaterial);

        CreateInteractableItem(
            itemsRoot.transform,
            "Item_QuestScroll",
            PrimitiveType.Sphere,
            new Vector3(10.0f, 0.5f, -3.0f),
            Vector3.one * 0.5f,
            questScroll,
            blackMaterial);

        CreateInteractableItem(
            itemsRoot.transform,
            "Item_SealedRelic",
            PrimitiveType.Capsule,
            new Vector3(5.5f, 0.5f, 0.0f),
            Vector3.one * 0.7f,
            sealedRelic,
            goldMaterial);

        GameObject systemRoot = new GameObject("ItemInteractionSystem");
        Inventory inventory = systemRoot.AddComponent<Inventory>();

        CreateUiHierarchy(
            out ItemInteractionUI interactionUI,
            out InventoryUI inventoryUI,
            out Image pointerReticleImage,
            out RectTransform inventoryPanelRect);

        ItemInteractionController controller = systemRoot.AddComponent<ItemInteractionController>();
        ItemInteractionCursorInput cursorInput = systemRoot.AddComponent<ItemInteractionCursorInput>();
        ItemInteractionPlayMonitor playMonitor = systemRoot.AddComponent<ItemInteractionPlayMonitor>();

        Camera mainCamera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        GameObject playerObject = GameObject.FindWithTag("Player");
        LayerMask allLayers = ~0;

        SetPrivateField(controller, "interactionCamera", mainCamera);
        SetPrivateField(controller, "playerTransform", playerObject != null ? playerObject.transform : null);
        SetPrivateField(controller, "raycastMaxDistance", 30.0f);
        SetPrivateField(controller, "interactionRange", 3.0f);
        SetPrivateField(controller, "hitLayerMask", allLayers);
        SetPrivateField(controller, "queryTriggerInteraction", QueryTriggerInteraction.Ignore);
        SetPrivateField(controller, "skipPlayerColliders", true);
        SetPrivateField(controller, "inventory", inventory);
        SetPrivateField(controller, "interactionUI", interactionUI);
        SetPrivateField(controller, "showRayLine", true);
        SetPrivateField(controller, "rayLineWidth", 0.03f);
        SetPrivateField(controller, "showItemHighlight", true);
        SetPrivateField(controller, "highlightPickupColor", new Color(0.2f, 1.0f, 0.3f, 1.0f));
        SetPrivateField(controller, "highlightBlockedColor", new Color(1.0f, 0.25f, 0.2f, 1.0f));
        SetPrivateField(controller, "pointerReticle", pointerReticleImage);
        SetPrivateField(controller, "reticleDefaultColor", new Color(1.0f, 1.0f, 1.0f, 0.6f));
        SetPrivateField(controller, "reticlePickupColor", new Color(0.2f, 1.0f, 0.3f, 0.9f));
        SetPrivateField(controller, "reticleBlockedColor", new Color(1.0f, 0.25f, 0.2f, 0.9f));
        SetPrivateField(controller, "reticleOutOfRangeColor", new Color(1.0f, 0.55f, 0.2f, 0.85f));
        SetPrivateField(controller, "drawDebugRay", true);
        SetPrivateField(controller, "targetStableFrames", 2);
        SetPrivateField(controller, "targetReleaseDelayFrames", 3);
        SetPrivateField(controller, "playMonitor", playMonitor);
        SetPrivateField(playMonitor, "enablePlayMonitor", true);
        SetPrivateField(playMonitor, "logLevel", ItemInteractionLogLevel.Events);

        if (playerObject != null && playerObject.TryGetComponent(out StarterAssetsInputs playerInputs))
        {
            playerInputs.cursorLocked = false;
            EditorUtility.SetDirty(playerInputs);
            SetPrivateField(cursorInput, "starterAssetsInputs", playerInputs);
        }

        SetPrivateField(inventoryUI, "inventory", inventory);
        SetPrivateField(interactionUI, "tooFarPromptMessage", "가까이 가서 상호작용하세요");
        SetPrivateField(controller, "blockingUiRoots", new[] { inventoryPanelRect });

        EnsureEventSystem();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        AssetDatabase.SaveAssets();
        Debug.Log("[RaySampleItemInteractionSetupTool] RaySample item interaction setup complete.");
    }

    private static ItemData GetOrCreateItemData(
        string assetName,
        string itemName,
        string description,
        ItemType itemType,
        bool canPickup)
    {
        string assetPath = $"{ItemDataFolder}/{assetName}.asset";
        ItemData existingData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        if (existingData != null)
        {
            existingData.itemName = itemName;
            existingData.description = description;
            existingData.itemType = itemType;
            existingData.canPickup = canPickup;
            EditorUtility.SetDirty(existingData);
            return existingData;
        }

        ItemData newData = ScriptableObject.CreateInstance<ItemData>();
        newData.itemName = itemName;
        newData.description = description;
        newData.itemType = itemType;
        newData.canPickup = canPickup;
        AssetDatabase.CreateAsset(newData, assetPath);
        return newData;
    }

    private static void CreateInteractableItem(
        Transform parent,
        string objectName,
        PrimitiveType primitiveType,
        Vector3 position,
        Vector3 scale,
        ItemData itemData,
        Material material)
    {
        GameObject itemObject = GameObject.CreatePrimitive(primitiveType);
        itemObject.name = objectName;
        itemObject.transform.SetParent(parent, false);
        itemObject.transform.position = position;
        itemObject.transform.localScale = scale;

        if (material != null && itemObject.TryGetComponent(out Renderer renderer))
        {
            renderer.sharedMaterial = material;
        }

        InteractableItem interactableItem = itemObject.AddComponent<InteractableItem>();
        interactableItem.itemData = itemData;
    }

    private static void CreateUiHierarchy(
        out ItemInteractionUI interactionUI,
        out InventoryUI inventoryUI,
        out Image pointerReticleImage,
        out RectTransform inventoryPanelRect)
    {
        GameObject canvasRoot = new GameObject("UI");
        Canvas canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasRoot.AddComponent<CanvasScaler>();
        canvasRoot.AddComponent<GraphicRaycaster>();

        pointerReticleImage = CreatePointerReticleImage(canvasRoot.transform);

        GameObject interactionPanel = CreateUiPanel(
            canvasRoot.transform,
            "InteractionPanel",
            new Vector2(0.5f, 0.15f),
            new Vector2(0.5f, 0.15f),
            new Vector2(0.5f, 0.5f),
            new Vector2(420.0f, 160.0f),
            new Color(0.0f, 0.0f, 0.0f, 0.65f),
            blocksRaycast: false);

        Text itemNameText = CreateUiText(
            interactionPanel.transform,
            "ItemNameText",
            new Vector2(0.5f, 0.78f),
            new Vector2(380.0f, 36.0f),
            22,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            "아이템 이름",
            blocksRaycast: false,
            richText: true);

        Text itemDescriptionText = CreateUiText(
            interactionPanel.transform,
            "ItemDescriptionText",
            new Vector2(0.5f, 0.48f),
            new Vector2(380.0f, 56.0f),
            16,
            FontStyle.Normal,
            TextAnchor.UpperCenter,
            "아이템 설명",
            blocksRaycast: false,
            richText: false);

        Text promptText = CreateUiText(
            interactionPanel.transform,
            "PromptText",
            new Vector2(0.5f, 0.18f),
            new Vector2(380.0f, 28.0f),
            18,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            "획득하기",
            blocksRaycast: false,
            richText: true);

        interactionUI = canvasRoot.AddComponent<ItemInteractionUI>();
        SetPrivateField(interactionUI, "interactionPanel", interactionPanel);
        SetPrivateField(interactionUI, "itemNameText", itemNameText);
        SetPrivateField(interactionUI, "itemDescriptionText", itemDescriptionText);
        SetPrivateField(interactionUI, "promptText", promptText);

        interactionPanel.SetActive(false);

        GameObject inventoryPanel = CreateUiPanel(
            canvasRoot.transform,
            "InventoryPanel",
            new Vector2(0.02f, 0.98f),
            new Vector2(0.02f, 0.98f),
            new Vector2(0.0f, 1.0f),
            new Vector2(260.0f, 220.0f),
            new Color(0.0f, 0.0f, 0.0f, 0.55f),
            blocksRaycast: true);

        inventoryPanelRect = inventoryPanel.GetComponent<RectTransform>();
        SetPrivateField(interactionUI, "inventoryPanelRect", inventoryPanelRect);

        Text inventoryListText = CreateUiText(
            inventoryPanel.transform,
            "InventoryListText",
            new Vector2(0.5f, 0.5f),
            new Vector2(230.0f, 190.0f),
            16,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            "인벤토리",
            blocksRaycast: false,
            richText: true);

        inventoryUI = canvasRoot.AddComponent<InventoryUI>();
        SetPrivateField(inventoryUI, "inventoryListText", inventoryListText);
    }

    /// <summary>
    /// 마우스를 따라가는 Pointer Reticle Image를 생성합니다. 런타임에 스크린 좌표로 위치가 갱신됩니다.
    /// </summary>
    private static Image CreatePointerReticleImage(Transform parent)
    {
        GameObject reticleObject = new GameObject("PointerReticle", typeof(RectTransform), typeof(Image));
        reticleObject.transform.SetParent(parent, false);

        RectTransform rectTransform = reticleObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(12.0f, 12.0f);
        rectTransform.anchoredPosition = Vector2.zero;

        Image image = reticleObject.GetComponent<Image>();
        image.color = new Color(1.0f, 1.0f, 1.0f, 0.6f);
        image.raycastTarget = false;

        return image;
    }

    private static GameObject CreateUiPanel(
        Transform parent,
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta,
        Color backgroundColor,
        bool blocksRaycast)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.anchoredPosition = Vector2.zero;

        Image image = panelObject.GetComponent<Image>();
        image.color = backgroundColor;
        image.raycastTarget = blocksRaycast;

        return panelObject;
    }

    private static Text CreateUiText(
        Transform parent,
        string objectName,
        Vector2 anchorCenter,
        Vector2 sizeDelta,
        int fontSize,
        FontStyle fontStyle,
        TextAnchor alignment,
        string defaultText,
        bool blocksRaycast,
        bool richText)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorCenter;
        rectTransform.anchorMax = anchorCenter;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.anchoredPosition = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.supportRichText = richText;
        text.text = defaultText;
        text.raycastTarget = blocksRaycast;

        return text;
    }

    private static void RemoveExistingSetupRoot(string rootName)
    {
        GameObject existingRoot = GameObject.Find(rootName);
        if (existingRoot != null)
        {
            Object.DestroyImmediate(existingRoot);
        }
    }

    private static void RemoveExistingEventSystem()
    {
        EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        for (int index = 0; index < eventSystems.Length; index++)
        {
            Object.DestroyImmediate(eventSystems[index].gameObject);
        }
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EventSystemPrefabPath);
        if (eventSystemPrefab == null)
        {
            Debug.LogWarning(
                "[RaySampleItemInteractionSetupTool] UI_EventSystem prefab not found: " +
                EventSystemPrefabPath);
            return;
        }

        PrefabUtility.InstantiatePrefab(eventSystemPrefab);
    }

    private static void EnsureFolderPath(string assetPath)
    {
        string[] segments = assetPath.Split('/');
        if (segments.Length == 0 || segments[0] != "Assets")
        {
            throw new IOException("Asset path must start with Assets.");
        }

        string currentPath = "Assets";
        for (int segmentIndex = 1; segmentIndex < segments.Length; segmentIndex++)
        {
            string nextSegment = segments[segmentIndex];
            string nextPath = currentPath + "/" + nextSegment;
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, nextSegment);
            }

            currentPath = nextPath;
        }
    }

    private static void SetPrivateField<TTarget>(TTarget targetObject, string fieldName, object value)
    {
        FieldInfo targetField = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (targetField == null)
        {
            Debug.LogWarning($"[RaySampleItemInteractionSetupTool] Missing field: {typeof(TTarget).Name}.{fieldName}");
            return;
        }

        targetField.SetValue(targetObject, value);
    }
}
