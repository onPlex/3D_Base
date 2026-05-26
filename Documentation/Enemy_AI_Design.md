# Enemy AI Design (Behavior Graph + Animator)

## 개요
- 현재 `Enemy_Melee` AI는 `Behavior Graph` 기반의 분기형 상태 흐름으로 구성되어 있습니다.
- 핵심 루프는 `Sense -> Selector(Attack / Chase / Investigate / Patrol)` 구조입니다.
- 이동은 `NavMeshAgent`, 시각 판정은 `SenseTargetAction`, 애니메이션은 `Animator`(`StarterAssetsThirdPerson.controller`)로 동작합니다.

## 런타임 구성 요소
- `EnemyBehaviorBridge`
  - `EnemyConfigSO`를 참조하고, 순찰 포인트(`patrolPoints`)를 제공합니다.
  - 수동 포인트가 비어 있으면 씬의 `PatrolPoint` 루트에서 자동 수집합니다.
- `BehaviorGraphAgent`
  - 그래프 에셋: `BG_Enemy_Melee.asset`
  - `Self`, `Target`, `SelfAnimator`, `PatrolPoints` 같은 Blackboard override를 바인딩합니다.
- `EnemyAIDebugVisualizer`
  - 실행 branch, 감지 결과, 경로, 마지막 위치를 오버레이/Gizmo로 시각화합니다.
- `EnemyAIConsoleMonitor`
  - `SenseEval`, `Branch`, `Action`, `Heartbeat`를 콘솔 로그로 기록합니다.

## Behavior Graph 설계
- Main Graph (`BG_Enemy_Melee`)
  - `SyncConfigFromSelfAction`로 Config 값을 Blackboard에 동기화
  - `SenseTargetAction`로 `CanSeeTarget`, `DistanceToTarget`, `LastKnownPosition`, `HasLastKnownPosition` 갱신
  - `RunSubgraph` Selector 순서:
    1. `BG_Attack_SubGraph`
    2. `BG_Chase_SubGraph`
    3. `BG_Investigate_SubGraph`
    4. `BG_Patrol_SubGraph`

- Attack SubGraph
  - 조건 충족 시 `StopAgent -> LookAt -> SetAnimatorTrigger(Attack) -> SetLastAttackTime`
  - 현재 Attack clip은 비어 있어도 `AttackPlaceholder` state로 흐름이 유지되도록 구성

- Chase SubGraph
  - 감지 성공 시 `SetAgentSpeed(ChaseSpeed) -> NavigateToTarget`

- Investigate SubGraph
  - 타겟 시야 상실 + 마지막 위치 존재 시 `LastKnownPosition`으로 이동
  - 완료 후 `HasLastKnownPosition`을 false로 리셋

- Patrol SubGraph
  - `SelectNextPatrolPositionAction`으로 다음 포인트 선택
  - `SetAgentSpeed(PatrolSpeed) -> NavigateToLocation`

## Animator 설계
- Controller: `StarterAssetsThirdPerson.controller`
- 주요 파라미터
  - `Speed` (BlendTree 이동 블렌딩)
  - `MotionSpeed` (클립 재생 속도)
  - `Attack` (Trigger)
- Attack state
  - `AttackPlaceholder`(Motion 없음) state를 통해 clip 미삽입 상태에서도 전이/복귀 흐름 보장

## 명세 대조 결과 (enemy_melee_behavior_graph_dev_spec 기준)
- 구조/역할 차이
  - `EnemyBehaviorBridge`가 명세의 런타임 파사드(감지/이동/애니메이션 헬퍼 집약)보다는 축소된 형태입니다.
  - 현재 감지 핵심 로직은 `SenseTargetAction`에 직접 포함되어 있습니다.
  - 근거: `Assets/02.Scripts/Enemy/EnemyBehaviorBridge.cs`, `Assets/05.Behavior/Scripts/SenseTargetAction.cs`
- Blackboard 동기화 차이
  - 명세에서 제시한 `AttackLockTime`, `WaitAtPatrolPoint`, `ViewAngle` 동기화가 현재 `SyncConfigFromSelfAction`에는 없습니다.
  - 현재 동기화 대상은 `AttackRange`, `DetectRadius`, `ChaseSpeed`, `PatrolSpeed`, `AttackCooldown` 중심입니다.
  - 근거: `Assets/05.Behavior/Scripts/SyncConfigFromSelfAction.cs`
- Attack 타이밍 차이
  - 명세는 `AttackLockTime` 기반 `Wait` 흐름을 제시하지만, 현재는 `SetLastAttackTimeAction` 내부 랜덤 windup(`0.4~0.8s`)으로 공격 타이밍을 제어합니다.
  - 근거: `Assets/05.Behavior/Scripts/SetLastAttackTimeAction.cs`, `Assets/05.Behavior/BG_Attack_SubGraph.asset`
- Observer 정책 점검 필요
  - `Chase/Investigate` 분기에서 `m_ObserverType`이 동일 값(`2`)로 사용되어, 명세 권장(특히 Chase의 self-abort 보장)과 완전히 동일한 의도로 동작하는지 Play Mode 확인이 필요합니다.
  - 근거: `Assets/05.Behavior/BG_Chase_SubGraph.asset`, `Assets/05.Behavior/BG_Investigate_SubGraph.asset`
- Edge Case 미구현
  - 명세의 `LastKnownPosition` NavMesh 샘플링(`NavMesh.SamplePosition`) 보정은 현재 구현에 없습니다.
  - 근거: `Assets/05.Behavior/Scripts/SenseTargetAction.cs`
- Animator Attack clip 상태
  - `AttackPlaceholder`는 현재까지 Motion이 비어 있는 상태였고, 이번 작업에서 `Zombie Attack.fbx` 연결 대상입니다.
  - 근거: `Assets/01.Scenes/Addressable/StarterAssetsThirdPerson.controller`

## 이번 수정 사항
- `BG_Enemy_Melee.asset`
  - Attack 분기 조건이 `SenseTargetAction`에서 갱신되는 동일 변수 세트를 참조하도록 정렬
  - Chase 분기의 거리 조건을 감지 기반 흐름에서 안정적으로 통과되도록 보정
- `StarterAssetsThirdPerson.controller`
  - `MotionSpeed` 기본값을 `1`로 조정하여 이동 BlendTree 재생이 0으로 멈추는 상황을 방지

## 디버깅 체크리스트
- `EnemyAIConsoleMonitor`에서 다음 순서가 출력되는지 확인
  - `SenseEval ... suggested=Chase`
  - 이어서 Chase 관련 `SetAgentSpeedAction` 호출
- `EnemyAIDebugVisualizer`에서
  - `Branch(executed)`가 `Patrol`에서 `Chase`로 전환되는지 확인
  - `CanSeeTarget`, `DistanceToTarget` 값이 기대대로 갱신되는지 확인
- Attack clip을 추가한 뒤
  - `Attack` Trigger 진입/복귀가 정상인지 확인

