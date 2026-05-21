using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "StopAgentAction", story: "Stop [Self] agent", category: "Action/AI", id: "dc6d52d522a4ca8a9a132aeef037fb3f")]
public partial class StopAgentAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    
    protected override Status OnUpdate()
    {
        //방어코드
        if (Self?.Value == null)
        {
            return Status.Failure;
        }
        
        //NavMeshAgent 컴포넌트 레퍼런스 get 실패 
        //or !agent.isOnNavMesh -> NavMesh -> 작동이 불가 ?X 
        // bake 되지않은 mesh 위에 있는가 ?, 고장 난 상태
        //  Self.Value == GameObject -> .GetComponent<NavMeshAgent>();
        NavMeshAgent agent = Self.Value.GetComponent<NavMeshAgent>();
        if(agent == null || !agent.isOnNavMesh)
        {
            return Status.Failure;
        }

       
        agent.isStopped = true;
        // 
        agent.ResetPath(); 
        return Status.Success;
    }
}

