using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Set Last Attack Time", story: "Set LastAttackTime to current Time", category: "Action/AI",
    id: "1b6ef3f0cf3db4f7ed878fff88e901ea")]
public partial class SetLastAttackTimeAction : Action
{
    [SerializeReference] public BlackboardVariable<float> LastAttackTime;

    protected override Status OnUpdate()
    {
        LastAttackTime.Value = Time.time;
        return Status.Success;
    }
}