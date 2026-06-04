using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 마우스 RayCast로 감지한 아이템의 이름, 설명, 상호작용 안내 문구를 표시합니다.
/// 근접 거리·획득 가능 여부에 따라 prompt와 색상이 달라지며, Rich Text로 키캡·타입 배지를 연출합니다.
/// </summary>
public class ItemInteractionUI : MonoBehaviour
{
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private Text itemNameText;
    [SerializeField] private Text itemDescriptionText;
    [SerializeField] private Text promptText;
    [SerializeField] private RectTransform inventoryPanelRect;

    [Header("안내 문구")]
    [SerializeField] private string interactionKeyLabel = "E";
    [SerializeField] private string pickupPromptMessage = "획득하기";
    [SerializeField] private string cannotPickupMessage = "획득할 수 없습니다";
    [SerializeField] private string tooFarPromptMessage = "가까이 가서 상호작용하세요";

    [Header("표시 색상")]
    [SerializeField] private Color pickupPromptColor = new Color(0.49f, 1.0f, 0.54f);
    [SerializeField] private Color cannotPickupPromptColor = new Color(1.0f, 0.48f, 0.42f);
    [SerializeField] private Color tooFarPromptColor = new Color(0.78f, 0.78f, 0.78f);
    [SerializeField] private Color keyCapColor = new Color(1.0f, 0.88f, 0.54f);
    [SerializeField] private Color itemTypeBadgeColor = new Color(0.62f, 0.79f, 1.0f);

    private InteractableItem lastDisplayedItem;
    private InteractionTargetState lastDisplayedState = InteractionTargetState.None;
    private bool isPanelVisible;
    private ItemInteractionPlayMonitor playMonitor;

    /// <summary>
    /// InventoryPanel RectTransform — Controller의 blockingUiRoots에 연결합니다.
    /// </summary>
    public RectTransform InventoryPanelRect => inventoryPanelRect;

    private void Awake()
    {
        playMonitor = GetComponentInParent<ItemInteractionPlayMonitor>();
        if (playMonitor == null)
        {
            playMonitor = FindFirstObjectByType<ItemInteractionPlayMonitor>();
        }
    }

    /// <summary>
    /// 감지 상태에 따라 상호작용 UI를 갱신하거나 숨깁니다.
    /// </summary>
    public void UpdateDisplay(InteractableItem targetItem, InteractionTargetState targetState)
    {
        if (targetItem == null || targetItem.itemData == null || targetState == InteractionTargetState.None)
        {
            Hide();
            return;
        }

        if (lastDisplayedItem == targetItem && lastDisplayedState == targetState && isPanelVisible)
        {
            return;
        }

        ItemData data = targetItem.itemData;

        if (interactionPanel != null && !interactionPanel.activeSelf)
        {
            interactionPanel.SetActive(true);
            playMonitor?.LogUiVisibility(true, "InteractionPanel", targetState);
        }

        if (itemNameText != null)
        {
            itemNameText.text = BuildNameLine(data);
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = data.description;
        }

        if (promptText != null)
        {
            promptText.text = BuildPromptForState(targetState);
        }

        lastDisplayedItem = targetItem;
        lastDisplayedState = targetState;
        isPanelVisible = true;
    }

    /// <summary>
    /// 하위 호환: 근접·획득 가능으로 간주합니다.
    /// </summary>
    public void UpdateDisplay(InteractableItem targetItem)
    {
        if (targetItem == null || targetItem.itemData == null)
        {
            Hide();
            return;
        }

        InteractionTargetState assumedState = targetItem.itemData.canPickup
            ? InteractionTargetState.InRangeCanPickup
            : InteractionTargetState.InRangeBlocked;
        UpdateDisplay(targetItem, assumedState);
    }

    /// <summary>
    /// 아이템 이름과 타입 배지를 함께 구성합니다.
    /// </summary>
    private string BuildNameLine(ItemData data)
    {
        string typeBadge = $"<size=14>{Colorize($"[{data.TypeDisplayName}]", itemTypeBadgeColor)}</size>";
        return $"<b>{data.DisplayName}</b>   {typeBadge}";
    }

    /// <summary>
    /// 상태별 prompt를 Rich Text로 구성합니다. 획득 가능 시 키캡을 강조합니다.
    /// </summary>
    private string BuildPromptForState(InteractionTargetState targetState)
    {
        switch (targetState)
        {
            case InteractionTargetState.OutOfRange:
                return Colorize(tooFarPromptMessage, tooFarPromptColor);
            case InteractionTargetState.InRangeCanPickup:
                string keyCap = $"<b>{Colorize($"[ {interactionKeyLabel} ]", keyCapColor)}</b>";
                return $"{keyCap} {Colorize(pickupPromptMessage, pickupPromptColor)}";
            case InteractionTargetState.InRangeBlocked:
                return Colorize(cannotPickupMessage, cannotPickupPromptColor);
            default:
                return string.Empty;
        }
    }

    private static string Colorize(string text, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    }

    /// <summary>
    /// 대상이 없을 때 InteractionPanel을 숨깁니다.
    /// </summary>
    public void Hide()
    {
        if (!isPanelVisible && interactionPanel != null && !interactionPanel.activeSelf)
        {
            return;
        }

        if (interactionPanel != null)
        {
            interactionPanel.SetActive(false);
        }

        if (isPanelVisible)
        {
            playMonitor?.LogUiVisibility(false, "InteractionPanel", InteractionTargetState.None);
        }

        lastDisplayedItem = null;
        lastDisplayedState = InteractionTargetState.None;
        isPanelVisible = false;
    }
}
