using UnityEngine;

/// <summary>
/// ScriptableObject asset that stores item data independently from scene objects.
/// </summary>
[CreateAssetMenu(fileName = "NewItemData", menuName = "ForClass/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("UI에 표시할 아이템 이름")]
    public string itemName;

    [Tooltip("UI에 표시할 설명 문장")]
    [TextArea(2, 4)]
    public string description;

    [Header("분류")]
    public ItemType itemType;

    [Header("획득 설정")]
    [Tooltip("false이면 RayCast로 감지되지만 E 키 획득은 불가능합니다.")]
    public bool canPickup = true;

    public string DisplayName => string.IsNullOrWhiteSpace(itemName) ? name : itemName;

    /// <summary>
    /// UI 표시용 한글 타입 라벨입니다. (인벤토리 태그, 프롬프트 배지 등)
    /// </summary>
    public string TypeDisplayName
    {
        get
        {
            switch (itemType)
            {
                case ItemType.Potion:
                    return "포션";
                case ItemType.Weapon:
                    return "무기";
                case ItemType.Key:
                    return "열쇠";
                case ItemType.QuestItem:
                    return "퀘스트";
                default:
                    return "아이템";
            }
        }
    }
}
