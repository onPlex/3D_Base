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
    protected override Status OnUpdate()
    {
        if (Self?.Value == null)
            return Status.Failure;
        UnityEngine.AI.NavMeshAgent agent = Self.Value.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent == null || !agent.isOnNavMesh)
            return Status.Failure;
        agent.isStopped = true;
        agent.ResetPath();
        return Status.Success;
    }
}

