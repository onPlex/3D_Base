using UnityEngine;

/// <summary>
/// RaySample 아이템 상호작용 Play 모드 모니터링 로그를 출력합니다.
/// Inspector에서 레벨·스로틀을 조정해 상황별 디버깅에 사용합니다.
/// </summary>
public class ItemInteractionPlayMonitor : MonoBehaviour
{
    private const string LogPrefix = "[ItemInteraction]";

    [Header("모니터 설정")]
    [SerializeField] private bool enablePlayMonitor = true;
    [SerializeField] private ItemInteractionLogLevel logLevel = ItemInteractionLogLevel.Events;
    [SerializeField] private float verboseLogIntervalSeconds = 0.5f;
    [SerializeField] private bool logToConsole = true;

    private float nextVerboseLogTime;

    public bool IsEnabled => enablePlayMonitor && logLevel != ItemInteractionLogLevel.Off;

    public bool IsVerboseEnabled =>
        IsEnabled && logLevel == ItemInteractionLogLevel.Verbose;

    public bool IsEventsEnabled =>
        IsEnabled && logLevel >= ItemInteractionLogLevel.Events;

    /// <summary>
    /// 안정 타겟·상태가 변경되었을 때 호출합니다.
    /// </summary>
    public void LogTargetChanged(
        InteractableItem item,
        InteractionTargetState rawState,
        InteractionTargetState stableState,
        float distanceToPlayer)
    {
        if (!IsEventsEnabled)
        {
            return;
        }

        string itemName = item != null && item.itemData != null ? item.itemData.DisplayName : "(none)";
        WriteLog(
            $"TargetChanged | item={itemName} | raw={rawState} → stable={stableState} | dist={distanceToPlayer:F2}m");
    }

    /// <summary>
    /// 상호작용이 일시 중단되었을 때 호출합니다.
    /// </summary>
    public void LogSuspended(string reason)
    {
        if (!IsEventsEnabled)
        {
            return;
        }

        WriteLog($"Suspended | reason={reason}");
    }

    /// <summary>
    /// InventoryPanel 등 차단 UI 위에 포인터가 있을 때 호출합니다.
    /// </summary>
    public void LogUiBlock(string hitObjectName)
    {
        if (!IsEventsEnabled)
        {
            return;
        }

        WriteLog($"UiBlock | hit={hitObjectName}");
    }

    /// <summary>
    /// UI 패널 표시/숨김이 바뀔 때 호출합니다.
    /// </summary>
    public void LogUiVisibility(bool isVisible, string panelName, InteractionTargetState state)
    {
        if (!IsEventsEnabled)
        {
            return;
        }

        WriteLog($"UiVisibility | panel={panelName} | visible={isVisible} | state={state}");
    }

    /// <summary>
    /// E키 획득 시도 결과를 기록합니다.
    /// </summary>
    public void LogPickup(InteractableItem item, bool success, string detail)
    {
        if (!IsEventsEnabled)
        {
            return;
        }

        string itemName = item != null && item.itemData != null ? item.itemData.DisplayName : "(none)";
        WriteLog($"Pickup | item={itemName} | success={success} | {detail}");
    }

    /// <summary>
    /// Inventory에 아이템이 추가되었을 때 호출합니다.
    /// </summary>
    public void LogInventoryAdd(ItemData itemData, int totalCount)
    {
        if (!IsEventsEnabled)
        {
            return;
        }

        string itemName = itemData != null ? itemData.DisplayName : "(null)";
        WriteLog($"InventoryAdd | item={itemName} | total={totalCount}");
    }

    /// <summary>
    /// Verbose 레벨 Ray 요약 로그 (스로틀 적용).
    /// </summary>
    public void LogRayVerbose(
        InteractionTargetState rawState,
        InteractionTargetState stableState,
        InteractableItem rawItem,
        bool isUiBlocked,
        bool isCameraLookHeld)
    {
        if (!IsVerboseEnabled || !logToConsole)
        {
            return;
        }

        if (Time.unscaledTime < nextVerboseLogTime)
        {
            return;
        }

        nextVerboseLogTime = Time.unscaledTime + verboseLogIntervalSeconds;

        string itemName = rawItem != null && rawItem.itemData != null
            ? rawItem.itemData.DisplayName
            : "(none)";

        WriteLog(
            $"RayVerbose | raw={rawState} stable={stableState} | item={itemName} | uiBlock={isUiBlocked} | rmbLook={isCameraLookHeld}");
    }

    private void WriteLog(string message)
    {
        if (!logToConsole)
        {
            return;
        }

        Debug.Log($"{LogPrefix} {message}", this);
    }
}
