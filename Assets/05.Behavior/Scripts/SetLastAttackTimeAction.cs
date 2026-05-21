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

    protected override Status OnUpdate()
    {
        LastAttackTime.Value = Time.time;
        return Status.Success;
    }
}


