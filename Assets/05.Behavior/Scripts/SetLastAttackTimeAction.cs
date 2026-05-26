using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Set Last Attack Time", story: "Set [LastAttackTime] to current time", 
    category: "Action/AI", id: "e89cdca60acaeefbc45f1d0852eab428")]
public partial class SetLastAttackTimeAction : Action
{
    [SerializeReference] public BlackboardVariable<float> LastAttackTime;
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    
    // 공격 애니메이션이 실제로 재생될 시간을 확보하기 위한 내부 대기 상태입니다.
    private bool isWaitingForAttackWindow;
    private float attackWindowEndTime;

    // 기획 요청 범위(0.4 ~ 0.8초) 내에서 랜덤 대기 후 공격 시간을 기록합니다.
    private const float MinAttackWindowSeconds = 0.4f;
    private const float MaxAttackWindowSeconds = 0.8f;

    // 실행 중인 노드의 소유 GameObject를 사용해 개체별 Self를 정확히 복구한다.
    private GameObject ResolveFallbackSelf()
    {
        return GameObject;
    }

    protected override Status OnUpdate()
    {
        if (Self != null && Self.Value == null)
        {
            Self.Value = ResolveFallbackSelf();
        }

        EnemyAIDebugVisualizer debugVisualizer = Self?.Value != null
            ? Self.Value.GetComponent<EnemyAIDebugVisualizer>()
            : null;
        EnemyAIConsoleMonitor monitor = Self?.Value != null
            ? Self.Value.GetComponent<EnemyAIConsoleMonitor>()
            : null;

        if (!isWaitingForAttackWindow)
        {
            float waitSeconds = UnityEngine.Random.Range(MinAttackWindowSeconds, MaxAttackWindowSeconds);
            attackWindowEndTime = Time.time + waitSeconds;
            isWaitingForAttackWindow = true;
            if (debugVisualizer != null)
                debugVisualizer.ReportBranch("Attack", $"Attack windup {waitSeconds:F2}s");
            if (monitor != null)
            {
                monitor.ReportAction("SetLastAttackTimeAction", $"windup={waitSeconds:F2}s");
                monitor.ReportBranch("Attack", "Attack windup");
            }
            return Status.Running;
        }

        if (Time.time < attackWindowEndTime)
            return Status.Running;

        // Trigger 발행 전에 RPG 컨트롤러의 공격 분기 파라미터를 먼저 맞춘다.
        EnemyRpgAnimatorDriver animatorDriver = Self?.Value != null
            ? Self.Value.GetComponent<EnemyRpgAnimatorDriver>()
            : null;
        if (animatorDriver != null)
        {
            animatorDriver.PrepareAttackParameters();
        }

        LastAttackTime.Value = Time.time;
        isWaitingForAttackWindow = false;
        if (debugVisualizer != null)
            debugVisualizer.ReportAttackTriggered();
        if (monitor != null)
        {
            monitor.ReportAction("SetLastAttackTimeAction", $"set LastAttackTime={LastAttackTime.Value:F2}", true);
            monitor.ReportBranch("Attack", "Attack committed");
        }
        return Status.Success;
    }
}


