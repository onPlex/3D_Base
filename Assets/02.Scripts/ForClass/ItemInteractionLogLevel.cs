/// <summary>
/// ItemInteraction Play 모니터링 로그 출력 수준입니다.
/// </summary>
public enum ItemInteractionLogLevel
{
    /// <summary>로그 없음</summary>
    Off = 0,

    /// <summary>상태 전환, 획득, Suspend, UI 차단 등 이벤트만</summary>
    Events = 1,

    /// <summary>Events + Ray Hit 요약 (스로틀)</summary>
    Verbose = 2
}
