using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Select Next Patrol Position", 
    story: "Select next patrol position from [Self] into [CurrentPatrolPosition] and [CurrentPatrolIndex]", 
    category: "Action/AI", id: "302a39f14f9db86004a43c8111ae0c8c")]
public partial class SelectNextPatrolPositionAction : Action
{
    private const string PlayerTag = "Player";

    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<Vector3> CurrentPatrolPosition;
    [SerializeReference] public BlackboardVariable<int> CurrentPatrolIndex;

    // 순찰 지점 도착 후 다음 지점을 고르기 전 대기 상태를 관리합니다.
    private bool isWaitingAtPatrolPoint;
    private float nextPatrolSelectionTime;

    // 실행 중인 노드의 소유 GameObject를 사용해 개체별 Self를 정확히 복구한다.
    private GameObject ResolveFallbackSelf()
    {
        return GameObject;
    }

    // 순찰 노드가 실행되는 순간의 시야 상태를 한 번 더 점검해,
    // 타겟을 본 상태에서 Patrol 분기로 고정되는 현상을 차단합니다.
    private bool IsTargetVisibleNow(GameObject selfObject, EnemyBehaviorBridge bridge)
    {
        if (selfObject == null)
            return false;

        GameObject targetObject = null;
        try
        {
            targetObject = GameObject.FindGameObjectWithTag(PlayerTag);
        }
        catch (UnityException)
        {
            return false;
        }

        if (targetObject == null)
            return false;

        EnemyConfigSO config = bridge != null ? bridge.Config : null;
        float detectRadius = config != null ? config.detectRadius : 10f;
        float viewAngle = config != null ? config.viewAngle : 120f;
        LayerMask obstacleLayer = config != null ? config.obstacleLayer : ~0;

        Vector3 origin = selfObject.transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = targetObject.transform.position + Vector3.up * 1.0f;
        Vector3 toTarget = targetPos - origin;
        float distance = toTarget.magnitude;
        if (distance > detectRadius)
            return false;

        Vector3 flatDirection = toTarget;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude <= 0.0001f)
            return true;

        float angle = Vector3.Angle(selfObject.transform.forward, flatDirection.normalized);
        if (angle > viewAngle * 0.5f)
            return false;

        bool blocked = Physics.Raycast(origin, toTarget.normalized, distance, obstacleLayer);
        return !blocked;
    }

    protected override Status OnUpdate()
    {
        if (Self != null && Self.Value == null)
        {
            Self.Value = ResolveFallbackSelf();
        }

        if (Self?.Value == null)
        {
            Debug.LogWarning("[EnemyAI][Unknown] SelectNextPatrolPositionAction FAILED | Self is null (fallback failed)");
            return Status.Failure;
        }

        EnemyAIConsoleMonitor monitor = Self.Value.GetComponent<EnemyAIConsoleMonitor>();
        EnemyBehaviorBridge bridge = Self.Value.GetComponent<EnemyBehaviorBridge>();
        if (bridge == null || !bridge.HasPatrolPoints)
        {
            if (monitor != null)
                monitor.ReportFailure("SelectNextPatrolPositionAction", "Bridge missing or patrolPoints empty");
            else
                Debug.LogWarning("[EnemyAI][Unknown] SelectNextPatrolPositionAction FAILED | Bridge missing or patrolPoints empty");
            return Status.Failure;
        }
        EnemyAIDebugVisualizer debugVisualizer = Self.Value.GetComponent<EnemyAIDebugVisualizer>();

        if (IsTargetVisibleNow(Self.Value, bridge))
        {
            if (monitor != null)
            {
                monitor.ReportAction("SelectNextPatrolPositionAction", "target visible -> skip patrol", false);
                monitor.ReportBranch("Chase", "Patrol suppressed: target visible");
            }
            if (debugVisualizer != null)
                debugVisualizer.ReportSenseHint("Chase", "Patrol suppressed: target visible");
            return Status.Failure;
        }

        // Config에 정의된 대기 시간을 사용하고, 비정상 값은 0으로 보정합니다.
        float waitSeconds = 0f;
        if (bridge.HasConfig)
            waitSeconds = Mathf.Max(0f, bridge.Config.waitAtPatrolPoint);

        // 첫 진입 이후에는 지정 시간 동안 대기한 뒤 다음 순찰 지점을 선택합니다.
        if (isWaitingAtPatrolPoint)
        {
            if (Time.time < nextPatrolSelectionTime)
                return Status.Running;

            isWaitingAtPatrolPoint = false;
        }

        CurrentPatrolPosition.Value = bridge.GetPatrolPosition(CurrentPatrolIndex.Value);
        CurrentPatrolIndex.Value = CurrentPatrolIndex.Value + 1;
        nextPatrolSelectionTime = Time.time + waitSeconds;
        isWaitingAtPatrolPoint = waitSeconds > 0f;

        if (debugVisualizer != null)
            debugVisualizer.ReportPatrol(CurrentPatrolIndex.Value, CurrentPatrolPosition.Value);
        if (monitor != null)
        {
            monitor.ReportAction("SelectNextPatrolPositionAction", $"nextIndex={CurrentPatrolIndex.Value} point={CurrentPatrolPosition.Value}");
            monitor.ReportBranch("Patrol", "Select next patrol point");
        }

        return Status.Success;
    }
}

