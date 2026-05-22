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
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<Vector3> CurrentPatrolPosition;
    [SerializeReference] public BlackboardVariable<int> CurrentPatrolIndex;
    protected override Status OnUpdate()
    {
        if (Self?.Value == null)
            return Status.Failure;
        EnemyBehaviorBridge bridge = Self.Value.GetComponent<EnemyBehaviorBridge>();
        if (bridge == null || !bridge.HasPatrolPoints)
            return Status.Failure;
        CurrentPatrolPosition.Value = bridge.GetPatrolPosition(CurrentPatrolIndex.Value);
        CurrentPatrolIndex.Value = CurrentPatrolIndex.Value + 1;
        return Status.Success;
    }
}

