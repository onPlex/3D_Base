using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Stop Agent", story: "Stop [Self] agent", category: "Action/AI", id: "c19338d6bc8d753e625b1775360c3d6d")]
public partial class StopAgentAction : Action
{

    [SerializeReference] public BlackboardVariable<GameObject> Self;

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

        if (Self?.Value == null)
        {
            Debug.LogWarning("[EnemyAI][Unknown] StopAgentAction FAILED | Self is null (fallback failed)");
            return Status.Failure;
        }
        EnemyAIConsoleMonitor monitor = Self.Value.GetComponent<EnemyAIConsoleMonitor>();
        UnityEngine.AI.NavMeshAgent agent = Self.Value.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent == null || !agent.isOnNavMesh)
        {
            if (monitor != null)
                monitor.ReportFailure("StopAgentAction", "NavMeshAgent missing or not on NavMesh");
            return Status.Failure;
        }
        agent.isStopped = true;
        agent.ResetPath();
        EnemyAIDebugVisualizer debugVisualizer = Self.Value.GetComponent<EnemyAIDebugVisualizer>();
        if (debugVisualizer != null)
        {
            debugVisualizer.ReportBranch("Attack", "StopAgent");
            debugVisualizer.ReportAgentState(true, agent.speed, agent.hasPath, agent.destination);
        }
        if (monitor != null)
        {
            monitor.ReportAction("StopAgentAction", "agent stopped");
            monitor.ReportBranch("Attack", "StopAgentAction");
        }
        return Status.Success;
    }
}

