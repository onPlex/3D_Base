# Systemic Overload Documentation

## 문서 목적
이 폴더는 `Systemic Overload`의 MVP 구현과 운영을 위한 단일 참조 소스입니다.  
설계 철학(`Decoupling`, `Scalability`, `Performance`)을 실제 Unity 3D 개발 흐름에 맞춰 문서화했습니다.

## 권장 읽기 순서
1. [Technical Design Specification](Technical_Design_Specification_Systemic_Overload.md)
2. [Architecture Component and Interface](Architecture_Component_and_Interface.md)
3. [Gameplay Systems Movement AI Combat](Gameplay_Systems_Movement_AI_Combat.md)
4. [Data and Resource Management](Data_and_Resource_Management.md)
5. [VFX and Feedback Pipeline](VFX_and_Feedback_Pipeline.md)
6. [MVP Roadmap Phase and Release Management](MVP_Roadmap_Phase_and_Release_Management.md)

## 문서 목록
- [Technical Design Specification](Technical_Design_Specification_Systemic_Overload.md)
  - 프로젝트 비전, 시스템 목표, 기술 스택 기준 문서
- [Architecture Component and Interface](Architecture_Component_and_Interface.md)
  - Composition 기반 설계, Interface Messaging 규칙, 의존성 경계
- [Gameplay Systems Movement AI Combat](Gameplay_Systems_Movement_AI_Combat.md)
  - 이동, 전투, AI의 런타임 동작/구현 기준
- [Data and Resource Management](Data_and_Resource_Management.md)
  - `ScriptableObject`, `Addressables`, 메모리 관리 정책
- [VFX and Feedback Pipeline](VFX_and_Feedback_Pipeline.md)
  - `Shuriken`, `VFX Graph`, `Cinemachine Impulse` 적용 정책
- [MVP Roadmap Phase and Release Management](MVP_Roadmap_Phase_and_Release_Management.md)
  - MVP Phase 운영, 브랜치 전략, 릴리즈/태그 기준, Phase별 `Animation` 산출물 및 `Animation Pipeline` 공통 규칙
- [Phase1 vs Phase2 Change Log](Phase1_vs_Phase2_Change_Log.md) — Phase 2 도입 시 Phase 1 입력/애니/전투 연동 변경 요약
- [Phase2 Implementation Guide](Phase2_Implementation_Guide_and_Source.md) — Phase 2 구현 참고(코드는 `Assets`에 반영됨)
- [Unity 3D Course — Notion & Git](Unity3D_Course_Notion_and_Git.md) — 수업용 Notion 페이지 링크 및 `phase/*` 브랜치 정리
- [LiveClass-Branch vs class-screening (Enemy AI)](branches/class-screening/LiveClass-Branch_vs_class-screening_EnemyAI.md) — `Behavior Graph`/`Enemy AI` 구현 차이 및 에셋 변화 요약
- [LiveClass-Branch to class-screening Class Guide](branches/class-screening/LiveClass-Branch_to_class-screening_ClassGuide.md) — 수업 진행용 상세 가이드(`Behavior Graph` 변경 중심)
- [Behavior Graph Observer Abort Deep Dive](branches/class-screening/BehaviorGraph_ObserverAbort_DeepDive.md) — `Observer Abort` 수정/추가 작업 심화 정리

## 현재 패키지 기준
`Packages/manifest.json` 기준 핵심 패키지:
- `Input System`
- `AI Navigation`
- `Addressables`
- `Cinemachine`
- `VFX Graph`
- `URP`

## Phase 운영 원칙
- 개발 단위는 `feature/<phase>-<topic>` 브랜치에서 시작합니다.
- 각 기능은 해당 `phase/*` 브랜치로 PR 후 통합합니다.
- Phase 완료 후 `develop`으로 병합하고 마일스톤 태그를 생성합니다.
- 최종 MVP는 `release/mvp-1.0.0`에서 QA 후 `main`으로 병합합니다.
- 각 Phase는 `Assets/01.Scenes/PhaseValidation/` 아래 전용 Validation Scene을 유지해야 합니다.

## 문서 유지보수 규칙
- 새로운 시스템 도입 시, 관련 문서를 먼저 업데이트한 뒤 구현을 진행합니다.
- 런타임 성능 관련 변경(`Object Pool`, `Addressables`, `VFX`)은 체크리스트를 반드시 갱신합니다.
- Phase 종료 시 `Done/Out of Scope/Risk`를 문서에 기록합니다.
