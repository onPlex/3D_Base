# class-screening vs LiveClass-Branch (Behavior/Enemy AI 중심 비교)

## 비교 기준
- 기준 명령: `git diff LiveClass-Branch class-screening`
- 본 문서는 대규모 리소스 변화 전체를 나열하지 않고, `Behavior Graph`와 `Enemy AI` 동작에 직접 영향이 있는 변경을 우선 정리합니다.

## 한눈에 보는 결론
- `class-screening`은 `Enemy AI`를 단순 감지 기반에서 **전이 안정성 + 디버깅 가시성 강화형** 구조로 확장했습니다.
- 핵심은 `SenseTargetAction`에서 감지 결과만 기록하던 방식에서, 전이 실패 상황을 대비한 `fallback chase/attack/investigate/patrol` 로직을 추가한 점입니다.
- `Behavior Graph` 측면에서는 `AttackTrigger -> Trigger`, `AnimatorSpeedParam -> Velocity Z` 정렬과 `ConditionalGuardModifier` observer 설정 보정이 주요 차이입니다.
- `Enemy_Melee.prefab`은 `BehaviorGraphAgent` 연동과 함께 `EnemyRpgAnimatorDriver`, `EnemyAIDebugVisualizer`, `EnemyAIConsoleMonitor`, `EnemyAnimationEventReceiver`가 붙는 구성으로 강화되었습니다.

## Behavior Graph 차이

### 1) Blackboard / Parameter 정렬
- `BB_EnemyShared.asset`, `BG_Enemy_Melee.asset`, `BG_Attack_SubGraph.asset`에서 공격 Trigger 키가 `AttackTrigger` 기반 표현에서 실제 Animator 연동 키인 `Trigger`로 정렬되었습니다.
- `BG_Chase_SubGraph.asset`, `BG_Patrol_SubGraph.asset`, `BG_Investigate_SubGraph.asset`를 포함한 속도 파라미터 경로에서 `AnimatorSpeedParam` 값이 `Velocity Z`로 맞춰졌습니다.
- 결과적으로 RPG 계열 Animator Controller 파라미터 스키마와 Behavior 출력 키의 불일치가 줄어들었습니다.

### 2) 분기 Guard/Observer 보정
- `BG_Enemy_Melee.asset` 및 하위 SubGraph에서 `ConditionalGuardModifier` 배치와 observer 설정(`ObserverType` 0/2)이 재정렬되었습니다.
- `HasLastKnownPosition`, `LastKnownPosition` 연동이 SubGraph 경계에서 일관되도록 정리되어, `Investigate -> Patrol` 전환 안정성 개선 방향으로 맞춰졌습니다.

### 3) 그래프 자산 구조 변경
- `BG_Investigate.asset`는 `BG_Investigate_SubGraph.asset` 형태로 정리되었습니다.
- `BB_UnitShared.asset`는 제거되었고, Enemy 관련 공용 Blackboard는 `BB_EnemyShared.asset` 중심으로 수렴했습니다.

## Enemy AI 코드 차이

### 1) `SenseTargetAction`의 역할 확장
파일: `Assets/05.Behavior/Scripts/SenseTargetAction.cs`

- `LiveClass-Branch`: 감지(거리/FOV/LOS) 계산 후 Blackboard 업데이트 중심.
- `class-screening`: 아래 기능이 추가되었습니다.
  - `Player` tag 자동 target 탐색(초기 바인딩 누락 보완)
  - `planar distance` 기준 판단 강화
  - `SenseBreakdown` 로그를 위한 `inRadius/inFov/blocked` 세분화
  - `fallback chase` (`NavMeshAgent.SetDestination`)
  - `fallback attack` (`Trigger` 발행 + cooldown 보조)
  - `fallback investigate` (마지막 위치 재추적)
  - `investigate timeout / reached-last-known` 시 `HasLastKnownPosition` clear
  - Branch 전이 실패 대비 `DriveFallbackPatrol` 추가

즉, Graph 분기만 신뢰하던 구조에서, 런타임 안전장치를 갖춘 하이브리드 구조로 이동했습니다.

### 2) Patrol 선택 노드 안정화
파일: `Assets/05.Behavior/Scripts/SelectNextPatrolPositionAction.cs`

- `Self` fallback 복구(`GameObject` 기반)
- Patrol 실행 시점의 즉시 시야 재검사(`IsTargetVisibleNow`)로, target이 보이는 상황에서 Patrol 고정되는 문제를 완화
- `waitAtPatrolPoint` 기반 대기 상태(`Status.Running`) 지원
- `EnemyAIConsoleMonitor` 및 `EnemyAIDebugVisualizer` 연동

### 3) Action 노드들의 방어/관측성 강화
- `SetAgentSpeedAction.cs`: `Self` fallback, `NavMesh` 유효성 체크 강화, monitor/visualizer 보고
- `StopAgentAction.cs`: 정지 + path reset 시 monitor/visualizer 보고
- `SetLastAttackTimeAction.cs`: 공격 windup(0.4~0.8s) 대기 후 시간 기록, `EnemyRpgAnimatorDriver.PrepareAttackParameters()` 연동
- `SyncConfigFromSelfAction.cs`: 동작 로직 변화는 작고, 주석/메타 정리 중심

### 4) Bridge 확장
파일: `Assets/02.Scripts/Enemy/EnemyBehaviorBridge.cs`

- 수동 patrol point가 비어 있으면 씬의 `PatrolPoint` 루트를 탐색해 자동 수집하는 `Awake` 흐름이 추가되었습니다.
- 레벨 배치 단계에서 참조 누락이 있어도 기본 순찰 루프를 유지하기 쉬워졌습니다.

## 신규 Enemy 컴포넌트 및 Prefab 연결

### 신규 코드
- `Assets/02.Scripts/Enemy/EnemyRpgAnimatorDriver.cs`
  - `Moving`, `Velocity X`, `Velocity Z`, `Weapon`, `Action`, `TriggerNumber`를 `NavMeshAgent`/공격 상태와 동기화
- `Assets/02.Scripts/Enemy/EnemyAIDebugVisualizer.cs`
  - `SenseLoop` 판정과 동기화되는 `Gizmo` 기반 시각화(거리/FOV/LOS/branch 힌트)
- `Assets/02.Scripts/Enemy/EnemyAIConsoleMonitor.cs`
  - `SenseEval`, `SenseBreakdown`, `Branch`, `Heartbeat` 로그 분리
- `Assets/02.Scripts/Enemy/EnemyAnimationEventReceiver.cs`
  - `OnFootstep`, `FootL`, `FootR`, `Hit` 수신 함수 제공(경고 제거 및 확장 포인트)

### Prefab/Animator
- `Assets/03.Prefab/Enemy/Enemy_Melee.prefab`
  - `BehaviorGraphAgent` + 위 신규 컴포넌트들이 조합된 구성으로 확장
- `Assets/03.Prefab/Enemy/Enemy_RPG_Melee.controller`
  - RPG Animator 파라미터 체계와 Enemy AI 출력을 맞추기 위한 전용 Controller

## 비-AI 에셋 리소스 변경 요약 (간략)
- 변경 파일 수 기준으로 `Assets/Synty`, `Assets/Remesh Games`, `Assets/ExplosiveLLC` 영역의 리소스 증감이 큽니다.
- 이 구간은 주로 모델/애니메이션/머티리얼/메타데이터 레벨의 대용량 변화이며, 본 문서의 동작 분석 범위에서는 **Behavior/Enemy AI 로직 영향 파일**을 우선 근거로 사용했습니다.

## 검증 포인트 (권장)
- Play Mode에서 `SenseEval`의 `suggested`와 실제 `Branch` 로그가 일치하는지 확인
- target 상실 후 `Investigate -> Patrol` 복귀가 timeout 또는 도달 조건으로 종료되는지 확인
- 장애물(`obstacleLayer`)이 있을 때 `SenseBreakdown reason=los-blocked`가 재현되는지 확인
- Animator에서 `Velocity Z`/`Trigger` 기반 이동/공격 전이가 정상 동작하는지 확인

## 참고 파일
- `Assets/05.Behavior/BG_Enemy_Melee.asset`
- `Assets/05.Behavior/BG_Attack_SubGraph.asset`
- `Assets/05.Behavior/BG_Chase_SubGraph.asset`
- `Assets/05.Behavior/BG_Investigate_SubGraph.asset`
- `Assets/05.Behavior/BG_Patrol_SubGraph.asset`
- `Assets/05.Behavior/BB_EnemyShared.asset`
- `Assets/05.Behavior/Scripts/SenseTargetAction.cs`
- `Assets/05.Behavior/Scripts/SelectNextPatrolPositionAction.cs`
- `Assets/02.Scripts/Enemy/EnemyBehaviorBridge.cs`
- `Assets/02.Scripts/Enemy/EnemyRpgAnimatorDriver.cs`
- `Assets/02.Scripts/Enemy/EnemyAIDebugVisualizer.cs`
- `Assets/02.Scripts/Enemy/EnemyAIConsoleMonitor.cs`
- `Assets/02.Scripts/Enemy/EnemyAnimationEventReceiver.cs`
- `Assets/03.Prefab/Enemy/Enemy_Melee.prefab`
- `Assets/03.Prefab/Enemy/Enemy_RPG_Melee.controller`
