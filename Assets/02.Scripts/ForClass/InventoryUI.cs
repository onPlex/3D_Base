using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 획득한 ItemData 목록을 Unity UI Text에 연출감 있게 렌더링합니다.
/// 헤더+개수, 타입 태그 목록, 빈 상태 안내, 최근 획득 강조(NEW)를 지원합니다.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private Text inventoryListText;

    [Header("문구")]
    [SerializeField] private string headerMessage = "인벤토리";
    [SerializeField] private string emptyMessage = "획득한 아이템 없음";
    [SerializeField] private string emptyHintMessage = "아이템에 다가가 [E]로 획득하세요";

    [Header("연출 색상 (Rich Text)")]
    [SerializeField] private Color headerColor = new Color(1.0f, 0.96f, 0.82f);
    [SerializeField] private Color countColor = new Color(1.0f, 0.84f, 0.42f);
    [SerializeField] private Color typeTagColor = new Color(0.62f, 0.79f, 1.0f);
    [SerializeField] private Color emptyColor = new Color(0.6f, 0.63f, 0.65f);
    [SerializeField] private Color hintColor = new Color(0.48f, 0.5f, 0.52f);
    [SerializeField] private Color newBadgeColor = new Color(1.0f, 0.84f, 0.42f);

    [Header("최근 획득 강조")]
    [Tooltip("방금 획득한 아이템에 NEW 배지를 표시하는 시간(초). 0이면 비활성화")]
    [SerializeField] private float newBadgeDurationSeconds = 2.0f;

    private ItemData recentlyAddedItem;
    private int previousItemCount;

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged += HandleInventoryChanged;
        }

        previousItemCount = inventory != null ? inventory.Items.Count : 0;
        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= HandleInventoryChanged;
        }

        CancelInvoke(nameof(ClearRecentBadge));
    }

    /// <summary>
    /// 인벤토리 변경 시 최근 획득 아이템을 판별하고 NEW 배지 타이머를 시작합니다.
    /// </summary>
    private void HandleInventoryChanged()
    {
        if (inventory != null && inventory.Items.Count > previousItemCount && inventory.Items.Count > 0)
        {
            recentlyAddedItem = inventory.Items[inventory.Items.Count - 1];

            if (newBadgeDurationSeconds > 0.0f)
            {
                CancelInvoke(nameof(ClearRecentBadge));
                Invoke(nameof(ClearRecentBadge), newBadgeDurationSeconds);
            }
        }

        previousItemCount = inventory != null ? inventory.Items.Count : 0;
        RefreshDisplay();
    }

    private void ClearRecentBadge()
    {
        recentlyAddedItem = null;
        RefreshDisplay();
    }

    /// <summary>
    /// 현재 아이템 목록으로 인벤토리 텍스트를 다시 만듭니다.
    /// </summary>
    public void RefreshDisplay()
    {
        if (inventoryListText == null || inventory == null)
        {
            return;
        }

        StringBuilder builder = new StringBuilder();
        AppendHeader(builder, inventory.Items.Count);

        if (inventory.Items.Count == 0)
        {
            builder.Append('\n');
            builder.Append(Colorize(emptyMessage, emptyColor));

            if (!string.IsNullOrEmpty(emptyHintMessage))
            {
                builder.Append('\n');
                builder.Append($"<size=12>{Colorize(emptyHintMessage, hintColor)}</size>");
            }

            inventoryListText.text = builder.ToString();
            return;
        }

        for (int index = 0; index < inventory.Items.Count; index++)
        {
            ItemData item = inventory.Items[index];
            builder.Append('\n');
            AppendItemLine(builder, item);
        }

        inventoryListText.text = builder.ToString();
    }

    private void AppendHeader(StringBuilder builder, int itemCount)
    {
        builder.Append($"<b>{Colorize(headerMessage, headerColor)}</b>");
        builder.Append(' ');
        builder.Append(Colorize($"({itemCount})", countColor));
    }

    private void AppendItemLine(StringBuilder builder, ItemData item)
    {
        if (item == null)
        {
            builder.Append("- (missing item)");
            return;
        }

        builder.Append("• ");
        builder.Append(Colorize($"[{item.TypeDisplayName}]", typeTagColor));
        builder.Append(' ');
        builder.Append(item.DisplayName);

        if (recentlyAddedItem == item)
        {
            builder.Append("  ");
            builder.Append(Colorize("NEW", newBadgeColor));
        }
    }

    private static string Colorize(string text, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    }
}
