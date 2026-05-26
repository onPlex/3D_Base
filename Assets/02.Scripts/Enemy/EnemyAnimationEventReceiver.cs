using UnityEngine;

public class EnemyAnimationEventReceiver : MonoBehaviour
{
    // Enemy 이동 애니메이션의 Footstep Event를 수신한다.
    // 현재는 경고 제거와 확장 포인트 확보를 위해 빈 구현으로 유지한다.
    public void OnFootstep(AnimationEvent animationEvent)
    {
    }

    // RPG controller의 FootL AnimationEvent를 수신한다.
    public void FootL()
    {
    }

    // RPG controller의 FootR AnimationEvent를 수신한다.
    public void FootR()
    {
    }

    // RPG controller의 Hit AnimationEvent를 수신한다.
    // 현재는 공격 판정 로직을 별도 시스템에서 처리하므로 빈 핸들러로 유지한다.
    public void Hit()
    {
    }
}
