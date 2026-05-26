using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Set Agent Speed", story: "Set [Self] agent speed to [Speed]",
    category: "Action/AI", id: "0588045e47d7a2f2e4a6d0a62799d5b9")]
public partial class SetAgentSpeedAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<float> Speed;

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
            Debug.LogWarning("[EnemyAI][Unknown] SetAgentSpeedAction FAILED | Self is null (fallback failed)");
            return Status.Failure;
        }

        EnemyAIConsoleMonitor monitor = Self.Value.GetComponent<EnemyAIConsoleMonitor>();
        NavMeshAgent agent = Self.Value.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.isOnNavMesh)
        {
            if (monitor != null)
                monitor.ReportFailure("SetAgentSpeedAction", "NavMeshAgent missing or not on NavMesh");
            return Status.Failure;
        }
        agent.isStopped = false;
        agent.speed = Speed.Value;
        EnemyAIDebugVisualizer debugVisualizer = Self.Value.GetComponent<EnemyAIDebugVisualizer>();
        if (debugVisualizer != null)
            debugVisualizer.ReportAgentState(false, agent.speed, agent.hasPath, agent.destination);
        if (monitor != null)
            monitor.ReportAction("SetAgentSpeedAction", $"speed={agent.speed:F2}");
        return Status.Success;
    }
}

