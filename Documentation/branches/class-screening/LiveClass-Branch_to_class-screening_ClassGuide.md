# LiveClass-Branch -> class-screening 수업 진행 가이드 (Behavior Graph 중심)

## 문서 목적
- 본 문서는 `LiveClass-Branch` 상태에서 `class-screening` 상태까지 AI를 확장하는 수업 진행용 상세 가이드입니다.
- 핵심은 `Behavior Graph`의 분기 안정성, `Animator` 파라미터 정렬, `Observer Abort` 보강, 디버깅 관측성 강화입니다.
- 비교 근거는 `git diff LiveClass-Branch class-screening`입니다.

## 수업 목표
- 기존 감지 중심 AI를 **전이 안정성 중심 AI**로 확장합니다.
- `Attack / Chase / Investigate / Patrol` 분기가 실전 상황에서 끊기지 않도록 보강합니다.
- `SenseEval`과 실제 분기 실행(`Branch`)이 일치하도록 로그 기반 검증 루프를 갖춥니다.

## 선행 읽기
- `Documentation/branches/class-screening/LiveClass-Branch_vs_class-screening_EnemyAI.md`
- `Documentation/Enemy_AI_Design.md`

## 전체 흐름 (수업 진행 순서)

### 1) Baseline 확인 (LiveClass-Branch 관점)
- `SenseTargetAction`이 감지 결과를 Blackboard에 쓰는 역할에 집중되어 있습니다.
- Graph 분기 실패 시 runtime fallback이 거의 없습니다.
- `Behavior Graph` key와 `Animator` key 불일치 가능성이 남아 있습니다.

### 2) class-screening 핵심 변화 요약
- `Behavior Graph` key 정렬:
  - `AttackTrigger -> Trigger`
  - `AnimatorSpeedParam -> Velocity Z`
- `ConditionalGuardModifier` observer 설정 재배치(파일별 `ObserverType: None / Lower Priority` 조정)
- `SenseTargetAction`에서 fallback 로직 추가:
  - fallback chase / attack / investigate / patrol
- `EnemyAIDebugVisualizer`, `EnemyAIConsoleMonitor` 도입으로 디버깅 루프 강화

### ObserverType 표기 기준
- `ObserverType: None` (`m_ObserverType: 0`)
- `ObserverType: Self` (`m_ObserverType: 1`)
- `ObserverType: Lower Priority` (`m_ObserverType: 2`)
- `ObserverType: Both` (`m_ObserverType: 3`)

### 3) 실습 검증
- `SenseEval` 로그와 `Branch` 로그를 페어로 확인
- `Investigate -> Patrol` 종료 보장 확인(timeout/도달)
- 장애물 LOS 차단 여부(`los-blocked`) 확인

---

## Behavior Graph 변경점 상세

### A. Main Graph (`BG_Enemy_Melee.asset`)
- 역할:
  - 상위 Selector에서 `Attack -> Chase -> Investigate -> Patrol` 서브그래프를 조합합니다.
- 변경 핵심:
  - 공격 Trigger 변수 정렬(`Trigger`)
  - `HasLastKnownPosition`, `LastKnownPosition` 전달 정합성 강화
  - `ConditionalGuardModifier` observer 설정 일부를 `ObserverType: Lower Priority`로 상향해 분기 재평가 반응성을 높임
  - 일부 guard는 `ObserverType: None`으로 유지하여 과도한 abort를 방지
- 수업 포인트:
  - "모든 guard를 aggressive observer로 바꾸면 항상 좋은가?"를 반례와 함께 설명합니다.
  - Branch thrashing(분기 흔들림)과 반응성 사이의 trade-off를 다룹니다.

### B. Attack SubGraph (`BG_Attack_SubGraph.asset`)
- 변경 핵심:
  - Trigger 키를 `Trigger`로 일치시켜 `SetAnimatorTriggerAction` 경로를 표준화
  - guard observer를 `ObserverType: Lower Priority`로 조정한 항목이 있어, 타겟 상태 변화 시 공격 시퀀스를 빠르게 중단/재평가할 수 있게 구성
- 코드 연동:
  - `SetLastAttackTimeAction`에서 공격 windup과 `EnemyRpgAnimatorDriver.PrepareAttackParameters()`를 동기화
- 수업 포인트:
  - Attack 시작 직전 parameter 세팅 시점이 어긋나면 animation 분기 실패가 생긴다는 점을 로그와 함께 재현합니다.

### C. Chase SubGraph (`BG_Chase_SubGraph.asset`)
- 변경 핵심:
  - `AnimatorSpeedParam`이 `Velocity Z`로 정렬
  - guard observer가 `ObserverType: Lower Priority`로 조정된 항목 존재
- 코드 연동:
  - `SetAgentSpeedAction` 보강 + `SenseTargetAction` fallback chase
- 수업 포인트:
  - Graph 전이 실패 시에도 fallback chase로 추적이 유지되는 구조를 설명합니다.

### D. Investigate SubGraph (`BG_Investigate_SubGraph.asset`)
- 구조 변화:
  - `BG_Investigate.asset`에서 `BG_Investigate_SubGraph.asset`로 정리
- 변경 핵심:
  - `LastKnownPosition`, `HasLastKnownPosition` 연결 안정화
  - guard observer 일부가 `None -> Lower Priority`로 상향
- 코드 연동:
  - `SenseTargetAction`에서 `investigateStartedTime`, timeout, reached-last-known clear 처리
- 수업 포인트:
  - "왜 Investigate가 무한 지속되었는가?"를 상태 변수 관점에서 해부합니다.

### E. Patrol SubGraph (`BG_Patrol_SubGraph.asset`)
- 변경 핵심:
  - `AnimatorSpeedParam`의 `Velocity Z` 정렬
  - Patrol 실행 중 target visibility 재검증을 script 쪽에서 수행하도록 보강
- 코드 연동:
  - `SelectNextPatrolPositionAction`의 `IsTargetVisibleNow` + waitAtPatrolPoint
  - `SenseTargetAction`의 `DriveFallbackPatrol`
- 수업 포인트:
  - Patrol은 "기본 상태"이면서 동시에 "회복 상태"라는 점을 강조합니다.

---

## Script 변경점과 Graph 연결 해설

### `SenseTargetAction.cs`
- LiveClass baseline에서 class-screening으로 넘어오며 가장 큰 변경이 집중되었습니다.
- 추가된 핵심:
  - `Player` tag 자동 탐색
  - `planar distance` 사용
  - `inRadius/inFov/blocked` 세분화
  - fallback chase/attack/investigate/patrol
  - investigate 종료 보장(timeout/도달)
- 수업 실습:
  - fallback 제거 버전과 현재 버전을 번갈아 테스트해 전이 안정성 차이를 체감하게 합니다.

### `SelectNextPatrolPositionAction.cs`
- target visible 시 Patrol 분기를 실패 처리해 Chase 재진입을 유도합니다.
- waitAtPatrolPoint를 통해 Patrol 템포를 조절합니다.

### `SetAgentSpeedAction.cs`, `StopAgentAction.cs`, `SetLastAttackTimeAction.cs`
- 공통 보강:
  - `Self` fallback
  - 실패 케이스 명시 로그
  - debug visualizer 연동
- 결과:
  - "왜 멈췄는지 모르는 상태"를 로그로 설명 가능한 상태로 바꿉니다.

### `EnemyBehaviorBridge.cs`
- `PatrolPoint` 루트 자동 스캔으로 level 배치 실수를 흡수합니다.
- 수업에서 "데이터 세팅 누락이 runtime failure로 번지지 않게 하는 패턴" 예시로 사용 가능합니다.

---

## 수업 운영안 (권장)

### Phase 1: 문제 재현
- 시나리오:
  - target이 시야에 들어온 뒤 공격 사거리 진입
  - target 상실 후 마지막 위치 수색
- 관찰:
  - `SenseEval`과 실제 `Branch` 로그가 어긋나는 시점 찾기

### Phase 2: Graph 정렬
- `AttackTrigger`, `AnimatorSpeedParam`, guard observer 변경 포인트를 순서대로 적용
- 변경 직후마다 Play Mode 1회 검증

### Phase 3: Script fallback 보강
- `SenseTargetAction` 보강 후 재검증
- Patrol/Investigate 복귀 안정성 확인

### Phase 4: 관측성 검증
- `EnemyAIConsoleMonitor`의 `SenseBreakdown` reason을 기반으로 문제 분류:
  - `out-of-radius`
  - `out-of-fov`
  - `los-blocked`
  - `attack-window`, `chase-window`

---

## 자주 발생하는 이슈와 수업 중 설명 포인트
- `obstacleLayer` 미설정:
  - 벽이 있어도 `LOS blocked`가 안 뜨는 문제를 유발합니다.
- `Animator` key 불일치:
  - 이동/공격이 "논리상 실행되는데 animation이 안 보이는" 상태를 만듭니다.
- observer 과설정:
  - 반응성은 좋아지지만 branch 흔들림이 늘 수 있습니다.

---

## 실습 체크리스트
- `SenseEval suggested=Chase` 이후 `Branch -> Chase`로 이어지는가
- 공격 진입 시 `SetLastAttackTimeAction`의 windup 후 commit 로그가 찍히는가
- target 상실 후 `Investigate complete (timeout|reached-last-known)` 로그 후 Patrol 복귀하는가
- Scene Gizmo에서 감지 상태 색상/거리 라벨이 실제 상태와 일치하는가

## 관련 파일
- `Assets/05.Behavior/BG_Enemy_Melee.asset`
- `Assets/05.Behavior/BG_Attack_SubGraph.asset`
- `Assets/05.Behavior/BG_Chase_SubGraph.asset`
- `Assets/05.Behavior/BG_Investigate_SubGraph.asset`
- `Assets/05.Behavior/BG_Patrol_SubGraph.asset`
- `Assets/05.Behavior/BB_EnemyShared.asset`
- `Assets/05.Behavior/Scripts/SenseTargetAction.cs`
- `Assets/05.Behavior/Scripts/SelectNextPatrolPositionAction.cs`
- `Assets/05.Behavior/Scripts/SetAgentSpeedAction.cs`
- `Assets/05.Behavior/Scripts/SetLastAttackTimeAction.cs`
- `Assets/05.Behavior/Scripts/StopAgentAction.cs`
- `Assets/02.Scripts/Enemy/EnemyBehaviorBridge.cs`
- `Assets/02.Scripts/Enemy/EnemyAIConsoleMonitor.cs`
- `Assets/02.Scripts/Enemy/EnemyAIDebugVisualizer.cs`
