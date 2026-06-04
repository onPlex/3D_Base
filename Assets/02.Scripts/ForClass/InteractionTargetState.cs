/// <summary>
/// 마우스 RayCast로 감지한 InteractableItem의 상호작용 가능 상태입니다.
/// </summary>
public enum InteractionTargetState
{
    /// <summary>대상 없음 (Miss, UI 위 포인터 등)</summary>
    None,

    /// <summary>아이템은 감지했으나 플레이어와 거리가 interactionRange 초과</summary>
    OutOfRange,

    /// <summary>근접 거리 내, 획득 가능</summary>
    InRangeCanPickup,

    /// <summary>근접 거리 내, 획득 불가</summary>
    InRangeBlocked
}
