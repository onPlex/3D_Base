using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

// [Serializable, GeneratePropertyBag]
// 커스텀 노드가 Unity 에디터에서 직렬화(저장)되고, Inspector 및 Graph UI에 정상적으로 노출되도록 하는 필수 속성입니다.
[Serializable, GeneratePropertyBag]
// [NodeDescription]
// 이 노드가 Behavior Graph 에디터에서 어떻게 보일지 정의합니다.
// - name: 노드 생성 메뉴에서 검색되는 이름입니다.
// - story: 그래프 상에서 노드가 문장 형태로 읽히도록 만드는 UI 포맷입니다. 
//          여기에 적힌 단어들은 아래 선언한 BlackboardVariable 변수명과 매칭되어 슬롯으로 표시됩니다.
// - category: 노드 생성 메뉴에서의 폴더 경로입니다.
// - id: 노드의 고유 식별자입니다. 코드를 복사해서 새 노드를 만들 때 이 ID가 겹치지 않도록 주의해야 합니다.
[NodeDescription(name: "Sync Config From Self", story: "[Self] updates config", category: "Action", id: "55c1e220b86f25b7e3d8ac1f1c562f93")]
// Unity Behavior 시스템이 추가적인 코드를 백그라운드에서 자동 생성하므로 반드시 'partial' 클래스로 선언해야 합니다.
// 행동(Action)을 수행하는 리프(Leaf) 노드이므로 Action 클래스를 상속받습니다.
public partial class SyncConfigFromSelfAction : Action
{
    // [SerializeReference] 및 BlackboardVariable<T>

    // Behavior Tree의 Blackboard에 있는 변수들과 이 스크립트를 연결해주는 역할을 합니다.

    // [SerializeReference]를 붙여야 그래프 에디터에서 해당 변수를 드롭다운으로 선택하거나 값을 넣을 수 있습니다.

    [SerializeReference] public BlackboardVariable<GameObject> Self;

    [SerializeReference] public BlackboardVariable<float> AttackRange;

    [SerializeReference] public BlackboardVariable<float> DetectRadius;

    [SerializeReference] public BlackboardVariable<float> ChaseSpeed;

    [SerializeReference] public BlackboardVariable<float> PatrolSpeed;

    [SerializeReference] public BlackboardVariable<float> AttackCooldown;
    // OnUpdate()
    // Behavior Tree가 실행되면서 이 노드에 도달했을 때(Tick) 호출되는 핵심 로직 함수입니다.
    // 실행 결과에 따라 Status(Success, Failure, Running 등)를 반환해야 합니다.
    protected override Status OnUpdate()
    {
        // 1. 유효성 검사 (안전 장치)
        // Self.Value를 통해 참조된 게임 오브젝트가 실제로 존재하는지 확인합니다. 없다면 노드 실행을 실패(Failure) 처리합니다.
        if (Self?.Value == null)
            return Status.Failure;

        // 2. 컴포넌트 가져오기
        // 대상 게임 오브젝트에 붙어있는 EnemyBehaviorBridge 컴포넌트를 찾습니다.
        // (이 컴포넌트는 적의 기본 설정 데이터를 들고 있는 중간 다리 역할을 합니다.)
        EnemyBehaviorBridge bridge = Self.Value.GetComponent<EnemyBehaviorBridge>();
        
        // 브릿지 컴포넌트가 없거나, 내부에 설정 데이터(Config)가 연결되어 있지 않다면 실패 처리합니다.
        if (bridge == null || !bridge.HasConfig)
            return Status.Failure;

        // 3. 데이터 추출
        // 브릿지를 통해 실제 기획 데이터가 들어있는 ScriptableObject 등을 가져옵니다.
        EnemyConfigSO config = bridge.Config;

        // 4. Blackboard로 데이터 동기화(전달)
        // 가져온 고정 데이터(config)의 값들을 BlackboardVariable의 '.Value'에 덮어씌웁니다.
        // 이렇게 하면 Behavior Tree 내부의 다른 노드들이 이 갱신된 값들을 꺼내어 쓸 수 있게 됩니다.
        AttackRange.Value = config.attackRange;
        DetectRadius.Value = config.detectRadius;
        ChaseSpeed.Value = config.chaseSpeed;
        PatrolSpeed.Value = config.patrolSpeed;
        AttackCooldown.Value = config.attackCooldown;

        // 5. 실행 완료
        // 데이터를 갱신하는 단일 작업이 무사히 끝났으므로 Success(성공)를 반환하여 트리가 다음 노드로 넘어가게 합니다.
        return Status.Success;
    }
}