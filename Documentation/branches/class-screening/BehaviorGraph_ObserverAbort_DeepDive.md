# Behavior Graph Observer Abort 심화 문서 (LiveClass-Branch -> class-screening)

## 문서 목적
- `Behavior Graph`에서 `Observer Abort` 관련 수정/추가 작업만 분리해 상세히 정리합니다.
- 수업에서 "왜 observer 설정을 바꿨는지", "어떤 증상이 줄었는지", "어디서 재확인해야 하는지"를 바로 설명할 수 있게 구성했습니다.

## 비교 기준
- `git diff LiveClass-Branch class-screening`
- 분석 파일:
  - `Assets/05.Behavior/BG_Enemy_Melee.asset`
  - `Assets/05.Behavior/BG_Attack_SubGraph.asset`
  - `Assets/05.Behavior/BG_Chase_SubGraph.asset`
  - `Assets/05.Behavior/BG_Investigate_SubGraph.asset`

## ObserverType 옵션 이름 매핑
- `ObserverType: None` (`m_ObserverType: 0`)
- `ObserverType: Self` (`m_ObserverType: 1`)
- `ObserverType: Lower Priority` (`m_ObserverType: 2`)
- `ObserverType: Both` (`m_ObserverType: 3`)

## 먼저 정리: Observer Abort를 왜 손봤는가
- LiveClass 단계에서는 일부 guard가 상태 변화를 늦게 반영해, 분기 전환 타이밍이 늦거나 끊기는 증상이 있었습니다.
- class-screening에서는 guard observer를 재배치해, 조건 변화 시 현재 branch를 더 빠르게 abort/re-evaluate 하도록 보강했습니다.
- 단, 모든 분기를 aggressive로 바꾸지 않고 일부 `ObserverType: None`을 유지해 branch thrashing을 억제했습니다.

## 파일별 변경 상세

### 1) `BG_Chase_SubGraph.asset`
- 주요 변화:
  - `ConditionalGuardModifier` 관련 `ObserverType`이 `None -> Lower Priority`로 조정된 항목이 확인됩니다.
  - diff 근거: `<ObserverType>k__BackingField` 변경(`0`에서 `2`) 및 `m_ObserverType: 2`(`Lower Priority`) 항목 추가.
- 효과:
  - Chase 중 target 조건 변화가 발생하면 guard 재평가가 빨라져 Attack/Investigate 전환 지연이 줄어듭니다.

### 2) `BG_Attack_SubGraph.asset`
- 주요 변화:
  - `ConditionalGuardModifier`의 observer가 `ObserverType: Lower Priority`로 재정렬되었습니다.
  - 일부 직렬화 구간에서도 `<ObserverType>k__BackingField: 2`(`Lower Priority`)가 반영됩니다.
- 효과:
  - 공격 직전/공격 중 target 상태가 바뀌면 분기 재평가가 빨라져 잘못된 공격 유지가 줄어듭니다.

### 3) `BG_Investigate_SubGraph.asset`
- 주요 변화:
  - diff 기준 guard observer가 `None -> Lower Priority`로 변경된 구간이 존재합니다.
- 효과:
  - 마지막 목격 위치 관련 조건 변화(`HasLastKnownPosition` clear 등)를 분기가 더 빠르게 감지해 Patrol 복귀 지연을 줄입니다.

### 4) `BG_Enemy_Melee.asset` (Main Selector)
- 주요 변화:
  - observer 등록(`m_RegisteredObservers`) 및 `ConditionalGuardModifier` 구성 재배치
  - `ObserverType: Lower Priority` guard와 `ObserverType: None` guard가 공존
- 효과:
  - 상위 분기 반응성은 높이되, 모든 노드에 동일 abort 강도를 걸지 않아 불필요한 흔들림을 줄입니다.

## ObserverType 운용 의도 (프로젝트 기준 해석)
- `ObserverType: Lower Priority` 적용 구간:
  - 상태 변화에 즉시 반응해야 하는 guard(전이 지연이 치명적인 구간)
- `ObserverType: None` 유지 구간:
  - 과도한 abort가 오히려 흔들림을 만드는 guard(안정성 우선 구간)

주의:
- Unity 내부 enum 명칭은 버전/에디터 표기와 함께 Inspector에서 최종 확인이 필요합니다.
- 본 문서는 프로젝트 diff에서 관측된 `None/Lower Priority` 운용 패턴과 runtime 증상 개선을 기준으로 설명합니다.

## Observer Abort와 Script fallback의 관계
- Observer 설정만으로 모든 끊김이 해결되지는 않습니다.
- class-screening에서는 `SenseTargetAction` fallback이 함께 들어가, Graph 전이 실패 순간에도 최소 행동을 유지합니다.
- 따라서 실제 안정성은:
  - `Graph observer tuning` + `Script fallback` + `logging/visualization`
  - 이 3개가 결합된 결과입니다.

## 수업 시연 시나리오

### 시나리오 A: Chase -> Attack 전환
- 기대:
  - 사거리 진입 시 빠르게 Attack guard 재평가
- 관찰 포인트:
  - `SenseEval suggested=Attack`
  - `Branch -> Attack`
  - Attack windup/commit 로그

### 시나리오 B: Attack/Chase 중 target 상실
- 기대:
  - Investigate 또는 Patrol로 빠른 전이
- 관찰 포인트:
  - `HasLastKnownPosition` 상태 변화
  - `Branch -> Investigate` 이후 complete 로그

### 시나리오 C: Investigate 종료
- 기대:
  - timeout 또는 도달 시 `HasLastKnownPosition` clear
  - Patrol 복귀
- 관찰 포인트:
  - `Investigate complete (...)`
  - `Branch -> Patrol`

## 튜닝 가이드 (수업 중 실습용)
- 반응성이 너무 둔함:
  - 전환 핵심 guard의 observer를 `Lower Priority`로 검토
- 분기가 너무 흔들림:
  - 일부 guard를 `None`으로 되돌려 안정화
- 항상 로그로 검증:
  - observer 변경 1건마다 Play Mode에서 최소 1시나리오 재현

## 회귀 테스트 체크리스트
- 공격 사거리 진입 시 Attack 전환 지연이 없는가
- target 상실 시 Chase 고착 없이 전환되는가
- Investigate 종료 후 Patrol 복귀가 보장되는가
- `SenseEval`과 `Branch` 로그 불일치 빈도가 줄었는가

## 참고
- 상세 비교 요약: `Documentation/branches/class-screening/LiveClass-Branch_vs_class-screening_EnemyAI.md`
- 수업 진행 가이드: `Documentation/branches/class-screening/LiveClass-Branch_to_class-screening_ClassGuide.md`
