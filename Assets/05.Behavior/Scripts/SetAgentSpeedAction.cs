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

    protected override Status OnUpdate()
    {
        if (Self?.Value == null)
            return Status.Failure;
        NavMeshAgent agent = Self.Value.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.isOnNavMesh)
            return Status.Failure;
        agent.isStopped = false;
        agent.speed = Speed.Value;
        return Status.Success;
    }
}

