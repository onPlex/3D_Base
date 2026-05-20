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
// 스토리(story)에 여러 변수들을 나열하여, 노드 UI에서 각 변수들을 한눈에 보고 연결할 수 있게 만듭니다.
[NodeDescription(name: "Sense Target",
    story: "Self, Target, CanSeeTarget, DistanceToTarget, LastKnownPosition, HasLastKnownPosition, TargetPosition",
    category: "Action/AI", id: "24cb70622f80a7812633f21cbf35c323")]
public partial class SenseTargetAction : Action
{
    // [SerializeReference] 및 BlackboardVariable<T>
    // Behavior Tree의 Blackboard와 통신할 변수들입니다.
    // 이 노드는 'Self'와 'Target'이라는 입력(Input)을 받아서, 
    // 거리, 시야 확보 여부, 마지막 위치 등의 결과(Output)를 Blackboard에 기록하는 역할을 합니다.
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<bool> CanSeeTarget;
    [SerializeReference] public BlackboardVariable<float> DistanceToTarget;
    [SerializeReference] public BlackboardVariable<Vector3> LastKnownPosition;
    [SerializeReference] public BlackboardVariable<bool> HasLastKnownPosition;
    [SerializeReference] public BlackboardVariable<Vector3> TargetPosition;

    // OnUpdate()
    // Behavior Tree가 이 노드를 실행할 때마다 호출되는 함수입니다.
    protected override Status OnUpdate()
    {
        // 1. 유효성 검사 (안전 장치)
        // 나와 타겟 중 하나라도 존재하지 않는다면 감지 로직을 수행할 수 없습니다.
        if (Self?.Value == null || Target?.Value == null)
        {
            // 타겟이 없으므로 안 보인다고 처리하고 거리를 임의의 큰 값(9999f)으로 설정합니다.
            CanSeeTarget.Value = false;
            DistanceToTarget.Value = 9999f;
            // Status.Failure를 반환하면 트리의 흐름이 끊길 수 있으므로, 
            // "감지 시도 자체는 정상적으로 완료되었으나 안 보인다"는 의미로 Success를 반환합니다.
            return Status.Success;
        }

        Transform self = Self.Value.transform;
        Transform target = Target.Value.transform;

        // 2. 적 설정 데이터 가져오기
        // EnemyBehaviorBridge를 통해 기획 데이터(Config)를 가져옵니다.
        EnemyBehaviorBridge bridge = Self.Value.GetComponent<EnemyBehaviorBridge>();
        EnemyConfigSO config = bridge != null ? bridge.Config : null;

        // 삼항 연산자(?:)를 사용하여 Config가 없을 경우 기본값을 할당합니다. (에러 방지)
        float detectRadius = config != null ? config.detectRadius : 10f; // 기본 감지 반경 10
        float viewAngle = config != null ? config.viewAngle : 120f;      // 기본 시야각 120도
        LayerMask obstacleLayer = config != null ? config.obstacleLayer : ~0; // 기본적으로 모든 레이어를 장애물로 취급(~0)

        // 3. 타겟과의 거리 및 방향 계산
        Vector3 toTarget = target.position - self.position;
        float distance = toTarget.magnitude;
        
        // 계산된 거리와 타겟의 현재 위치를 Blackboard에 즉시 업데이트합니다.
        DistanceToTarget.Value = distance;
        TargetPosition.Value = target.position;

        bool visible = false; // 최종적으로 타겟이 보이는지 여부를 저장할 변수

        // 4. 시야 감지 로직 (거리 -> 각도 -> 장애물 순서로 검사)
        // 4-1. 거리 검사: 감지 반경(detectRadius) 안에 들어왔는가?
        if (distance <= detectRadius)
        {
            // 4-2. 시야각 검사: 타겟이 내 앞을 기준으로 시야각(viewAngle) 안에 있는가?
            Vector3 flatDirection = toTarget;
            flatDirection.y = 0f; // 높낮이 차이 때문에 각도 계산이 왜곡되는 것을 막기 위해 Y축을 0으로 평탄화
            
            // 내 정면(self.forward)과 타겟을 향하는 방향 사이의 각도를 구합니다.
            float angle = Vector3.Angle(self.forward, flatDirection.normalized);
            
            // 시야각(viewAngle)의 절반(0.5f)보다 작아야 내 시야 영역 안에 있는 것입니다.
            // (예: 시야각이 120도라면 좌측 60도, 우측 60도 이내여야 함)
            if (angle <= viewAngle * 0.5f)
            {
                // 4-3. 장애물 검사 (Line of Sight - 시야 확보): 사이에 벽이 없는가?
                // 발끝(바닥)에서 쏘면 바닥에 걸릴 수 있으므로, 눈높이(위로 1.5f)에서 타겟의 가슴 높이(위로 1.0f)로 레이(광선)를 쏩니다.
                Vector3 origin = self.position + Vector3.up * 1.5f;
                Vector3 dir = (target.position + Vector3.up * 1.0f - origin).normalized;
                
                // Physics.Raycast로 광선을 쏴서 장애물(obstacleLayer)에 부딪혔는지 확인합니다.
                bool blocked = Physics.Raycast(origin, dir, distance, obstacleLayer);
                
                // 막히지 않았다면(!blocked) 최종적으로 눈에 보인다는 의미입니다.
                visible = !blocked;
            }
        }

        // 5. 시야 검사 결과를 Blackboard에 동기화
        CanSeeTarget.Value = visible;
        
        // 만약 타겟이 보인다면, '마지막으로 목격한 위치'를 현재 위치로 갱신해줍니다.
        // 타겟이 시야에서 사라지더라도 AI가 이 위치로 수색하러 갈 수 있도록 하기 위함입니다.
        if (visible)
        {
            LastKnownPosition.Value = target.position;
            HasLastKnownPosition.Value = true;
        }

        // 감지 및 정보 업데이트가 완료되었으므로 성공(Success)을 반환합니다.
        return Status.Success;
    }
}