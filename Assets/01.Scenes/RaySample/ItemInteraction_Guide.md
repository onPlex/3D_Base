# RaySample 아이템 상호작용 시스템 — 구현 설명 문서

> **씬**: `Assets/01.Scenes/RaySample.unity`  
> **목적**: ScriptableObject + RayCast 기반 아이템 상호작용 참고 답안  
> **문서 버전**: 마우스 포인터 UX + UI 깜빡임 보완 + Play Monitor (2026)

---

## 목차

1. [시스템 개요](#1-시스템-개요)
2. [구현 기능 요약](#2-구현-기능-요약)
3. [스크립트 구성](#3-스크립트-구성)
4. [상호작용 감지 파이프라인](#4-상호작용-감지-파이프라인)
5. [ItemData ScriptableObject](#5-itemdata-scriptableobject)
6. [Inventory 저장 방식](#6-inventory-저장-방식)
7. [씬 Hierarchy 구조](#7-씬-hierarchy-구조)
8. [UI 구성](#8-ui-구성)
9. [입력·커서 (우클릭 시점 회전)](#9-입력커서-우클릭-시점-회전)
10. [시각화 기능](#10-시각화-기능)
11. [Inspector 주요 설정값](#11-inspector-주요-설정값)
12. [Play 모드 테스트 체크리스트](#12-play-모드-테스트-체크리스트)
13. [에디터 Setup 도구](#13-에디터-setup-도구)
14. [선택 확장 (과제 PDF)](#14-선택-확장-과제-pdf)
15. [알려진 이슈 및 해결](#15-알려진-이슈-및-해결)
16. [제출 설명 문서에 포함할 내용](#16-제출-설명-문서에-포함할-내용)

---

## 1. 시스템 개요

플레이어가 **마우스 포인터**로 월드의 아이템을 가리키면 정보가 UI에 표시됩니다.  
**플레이어와 아이템 사이 거리가 3m 이내**일 때만 하이라이트·E 키 획득이 가능합니다.

```
ItemData (ScriptableObject)
    ↓ 참조
InteractableItem + Collider (씬 오브젝트)
    ↓ 마우스 ScreenPointToRay
ItemInteractionController  ←→  InteractionTargetState
    ↓ UI 갱신              ↓ E키 획득 (근접·가능 시만)
ItemInteractionUI          Inventory → InventoryUI

ItemInteractionCursorInput (별도)
    ↓ 우클릭 Hold
StarterAssetsInputs → Cinemachine 시점 회전
```

### 핵심 흐름 (5단계)

| 단계 | 내용 |
|------|------|
| 1 | `ItemData` ScriptableObject 생성 (이름, 설명, 타입, canPickup) |
| 2 | 씬 오브젝트 + Collider + `InteractableItem` 연결 |
| 3 | 마우스 위치 `ScreenPointToRay` → `InteractableItem` 감지 |
| 4 | 거리·획득 가능 여부에 따라 UI / Reticle / 하이라이트 갱신 |
| 5 | 근접 + canPickup일 때 E키 → Inventory 추가 → 씬 오브젝트 비활성화 |

### 조작 요약

| 입력 | 동작 |
|------|------|
| **마우스 이동** | Pointer Reticle 이동 + RayCast 대상 탐색 |
| **우클릭 Hold** | 커서 숨김 + 카메라 시점 회전 (상호작용 일시 중단 UI만) |
| **E키** | `InRangeCanPickup` 상태일 때만 획득 |
| **WASD** | Starter Assets 3인칭 이동 |

---

## 2. 구현 기능 요약

과제 기본(SO + RayCast + Inventory + UI)에 더해 아래 UX가 구현되어 있습니다.

| 기능 | 설명 | 담당 스크립트 |
|------|------|----------------|
| **마우스 포인터 RayCast** | 화면 중앙이 아닌 `Mouse.position`에서 Ray 발사 | `ItemInteractionController` |
| **근접 거리 제한** | Ray로 보이는 아이템도 3m 밖이면 획득·하이라이트 불가 | `ItemInteractionController` |
| **상태 enum** | None / OutOfRange / InRangeCanPickup / InRangeBlocked | `InteractionTargetState` |
| **Pointer Reticle** | 마우스를 따라가는 12×12 UI Image, 상태별 색상 | `ItemInteractionController` |
| **우클릭 시점 회전** | 평소 자유 커서, RMB Hold 시 Lock + Look | `ItemInteractionCursorInput` |
| **LineRenderer 광선** | 카메라 → Hit 지점 3D 선 (Miss/원거리/근접 색 구분) | `ItemInteractionController` |
| **MaterialPropertyBlock 하이라이트** | 근접(InRange)일 때만 아이템 색 틴트 | `ItemInteractionController` |
| **UI Ray 분리** | InteractionPanel은 `raycastTarget=false`, Inventory만 차단 | Setup + `ItemInteractionController` |
| **타겟 안정화 (hysteresis)** | N프레임 확인 후 표시, Miss 시 release delay | `ItemInteractionController` |
| **Play 모니터 Log** | 상태 전환·획득·UI 차단 등 Console 로그 | `ItemInteractionPlayMonitor` |
| **UI 포인터 차단** | `blockingUiRoots`(InventoryPanel) 위에서만 Ray 중단 | `ItemInteractionController` |
| **Player Collider 스킵** | 자기 몸 CharacterController가 Ray를 가리지 않도록 제외 | `ItemInteractionController` |
| **Scene Gizmo** | 플레이어 기준 `interactionRange` WireSphere | `ItemInteractionController` |

### 이전 방식과의 차이

| 항목 | 이전 (중앙 Crosshair) | 현재 |
|------|----------------------|------|
| Ray 기준 | `camera.position + forward` | `ScreenPointToRay(Mouse.position)` |
| 거리 | Ray 최대 8m만 | `raycastMaxDistance` 30m + `interactionRange` 3m (플레이어↔아이템) |
| 조준 UI | 화면 중앙 고정 Crosshair | 마우스 추적 Pointer Reticle |
| 시점 | 항상 마우스 Look (커서 Lock) | **우클릭 Hold**일 때만 Look |

---

## 3. 스크립트 구성

위치: `Assets/02.Scripts/ForClass/`

| 스크립트 | 역할 |
|----------|------|
| `ItemType.cs` | enum: Potion, Weapon, Key, QuestItem |
| `ItemData.cs` | ScriptableObject — itemName, description, itemType, canPickup |
| `InteractableItem.cs` | 씬 오브젝트 ↔ ItemData 참조 (`RequireComponent(typeof(Collider))`) |
| `Inventory.cs` | `List<ItemData>` 저장, `AddItem()`, `OnInventoryChanged` 이벤트 |
| `ItemInteractionUI.cs` | 상태별 이름/설명/prompt Text 갱신 |
| `InventoryUI.cs` | 획득 목록 Text 갱신 |
| `InteractionTargetState.cs` | 상호작용 상태 enum |
| `ItemInteractionController.cs` | 마우스 RayCast, 거리 판정, 시각화, E키 획득 |
| `ItemInteractionCursorInput.cs` | 우클릭 Hold 시점 / 평소 자유 커서 |
| `ItemInteractionLogLevel.cs` | Play Monitor 로그 레벨 enum |
| `ItemInteractionPlayMonitor.cs` | Play 모드 상황별 Console 로그 |

에디터 도구: `Assets/Editor/RaySampleItemInteractionSetupTool.cs`  
메뉴: **Tools → ForClass → Setup RaySample Item Interaction**

---

## 4. 상호작용 감지 파이프라인

매 `Update()` 프레임마다 아래 순서로 처리됩니다.

```
1. 우클릭 Hold? → Yes → Suspend (CameraLook), UI/Ray 숨김
2. InventoryPanel(blockingUiRoots) 위? → Yes → Suspend (UiBlock)
   ※ InteractionPanel은 raycastTarget=false → 차단 안 됨
3. Mouse.position → ScreenPointToRay
4. RaycastAll → Player 스킵 → InteractableItem 선택
5. 거리 판정 → rawState (OutOfRange / InRange*)
6. targetStableFrames / targetReleaseDelayFrames → stableTarget·stableState
7. stable 기준 UI / Reticle / LineRenderer / 하이라이트 (동일 상태면 UI 스킵)
8. E키 & stable InRangeCanPickup → Inventory.AddItem
```

### UI 깜빡임 방지

| UI | raycastTarget | World Ray |
|----|---------------|-----------|
| InteractionPanel·Text | **false** | 막지 않음 (패널 표시 중에도 Ray 유지) |
| InventoryPanel | **true** | `blockingUiRoots`로만 Suspend |

과거: InteractionPanel이 Ray를 가로막아 **표시 → Hide → 표시** 루프가 발생했습니다.

### 타겟 안정화

| 필드 | 기본 | 역할 |
|------|------|------|
| `targetStableFrames` | 2 | raw 타겟·상태 N프레임 유지 시 stable 반영 |
| `targetReleaseDelayFrames` | 3 | Miss 후 N프레임 유예 |

### RayCast 파라미터

| 항목 | 기본값 | 설명 |
|------|--------|------|
| `interactionCamera` | `Camera.main` | ScreenPointToRay에 사용 (비어 있으면 Awake에서 자동) |
| `playerTransform` | `Tag: Player` | 근접 거리 계산 기준 (비어 있으면 Awake에서 자동) |
| `raycastMaxDistance` | **30m** | Ray가 닿을 수 있는 최대 거리 (포인터로 ‘무엇을 가리키는지’) |
| `interactionRange` | **3m** | 플레이어 위치 ↔ 아이템 `transform.position` 3D 거리 |
| `hitLayerMask` | All (~0) | Physics 레이어 마스크 |
| `skipPlayerColliders` | true | Player Tag / 자식 / CharacterController Hit 무시 |
| `queryTriggerInteraction` | Ignore | Trigger Collider 제외 |
| `blockingUiRoots` | InventoryPanel | 이 Rect 하위만 UI Ray 차단 |

### 상호작용 상태 (`InteractionTargetState`)

| 상태 | 조건 | InteractionPanel | E 획득 | 하이라이트 | Reticle |
|------|------|------------------|--------|------------|---------|
| `None` | Miss, UI 위 포인터, 우클릭 Hold, 비아이템 Collider 차단 | 숨김 | 불가 | 없음 | 흰색 반투명 |
| `OutOfRange` | Hit + 거리 > 3m | 이름/설명 + "가까이 가서…" | 불가 | 없음 | 주황 |
| `InRangeCanPickup` | Hit + 거리 ≤ 3m + canPickup | [E] 획득하기 | **가능** | 녹색 | 녹색 |
| `InRangeBlocked` | Hit + 거리 ≤ 3m + !canPickup | 획득할 수 없습니다 | 불가 | 빨강 | 빨강 |

### 핵심 코드 (참고)

```csharp
Vector2 pointer = Mouse.current.position.ReadValue();
Ray ray = interactionCamera.ScreenPointToRay(pointer);
RaycastHit[] hits = Physics.RaycastAll(ray, raycastMaxDistance, hitLayerMask, queryTriggerInteraction);
// ... InteractableItem 필터, 거리 판정 ...

if (currentTargetState == InteractionTargetState.InRangeCanPickup)
    inventory.AddItem(currentItem.itemData);
```

---

## 5. ItemData ScriptableObject

위치: `Assets/01.Scenes/RaySample/ItemData/`

| 에셋 | 타입 | canPickup | 씬 오브젝트 |
|------|------|-----------|-------------|
| ItemData_HealingPotion | Potion | true | Item_HealingPotion (Capsule) |
| ItemData_OldSword | Weapon | true | Item_OldSword (Cube, 가로) |
| ItemData_DungeonKey | Key | true | Item_DungeonKey (Cube, 작게) |
| ItemData_QuestScroll | QuestItem | true | Item_QuestScroll (Sphere) |
| ItemData_SealedRelic | QuestItem | **false** | Item_SealedRelic (Capsule, 획득 불가 테스트) |

### 생성 방법 (Unity Editor)

1. Project 창 우클릭 → **Create → ForClass → Item Data**
2. Inspector: itemName, description, itemType, canPickup
3. 씬 오브젝트 `InteractableItem.itemData`에 드래그 연결

---

## 6. Inventory 저장 방식

```csharp
public class Inventory : MonoBehaviour
{
    private List<ItemData> items = new List<ItemData>();
    public event Action OnInventoryChanged;

    public void AddItem(ItemData item)
    {
        items.Add(item);
        OnInventoryChanged?.Invoke();
    }
}
```

- 획득 시 **ItemData 참조**를 List에 추가 (값 복사 아님)
- `OnInventoryChanged`로 `InventoryUI` 목록 Text 갱신
- 씬 오브젝트는 `SetActive(false)` (Destroy 아님)

---

## 7. 씬 Hierarchy 구조

```
RaySample
├── PlayerArmature          (Tag: Player, Starter Assets TPS)
├── MainCamera              (ScreenPointToRay용 Camera)
├── PlayerFollowCamera      (Cinemachine)
├── InteractableItems
│   ├── Item_HealingPotion
│   ├── Item_OldSword
│   ├── Item_DungeonKey
│   ├── Item_QuestScroll
│   └── Item_SealedRelic
├── ItemInteractionSystem
│   ├── Inventory
│   ├── ItemInteractionController
│   ├── ItemInteractionCursorInput
│   └── ItemInteractionPlayMonitor
├── UI                      (Canvas Screen Space Overlay)
│   ├── PointerReticle      (마우스 추적, raycastTarget=false)
│   ├── InteractionPanel
│   └── InventoryPanel
└── UI_EventSystem          (InputSystemUIInputModule prefab)
```

---

## 8. UI 구성

### Canvas 패널

| 패널 | 위치 | 표시 내용 |
|------|------|-----------|
| InteractionPanel | 화면 중앙 하단 | 아이템 이름, 설명, 상태별 prompt |
| InventoryPanel | 화면 좌상단 | 획득한 아이템 목록 |
| PointerReticle | **마우스 위치** (런타임) | 조준점, 상태별 색상 |

### `ItemInteractionUI` prompt 규칙 (Rich Text)

| `InteractionTargetState` | prompt 텍스트 | 색 |
|--------------------------|---------------|-------------|
| `OutOfRange` | 가까이 가서 상호작용하세요 (`tooFarPromptMessage`) | 회색 |
| `InRangeCanPickup` | **[ E ]** 획득하기 (키캡 강조 + 액션) | 골드 키캡 + 녹색 |
| `InRangeBlocked` | 획득할 수 없습니다 | 주황 |

- 이름 줄: `<b>{이름}</b>  [{타입}]` — 타입 배지(파랑) 동반 (예: `던전 열쇠  [열쇠]`)
- 키캡 라벨은 `interactionKeyLabel`(기본 `E`)로 변경 가능
- `UpdateDisplay(InteractableItem, InteractionTargetState)` — Controller가 호출
- `None`이면 `Hide()`로 InteractionPanel 비활성화

> 모든 UI 한글은 **실제 UTF-8 문자**로 저장합니다. 씬 YAML에 `\uXXXX` 이스케이프를 평문/홑따옴표 스칼라로 넣으면 Unity가 디코딩하지 못해 그대로 출력되므로 금지합니다.

### `InventoryUI` 표시 규칙 (Rich Text)

| 상태 | 표시 |
|------|------|
| 헤더 | `<b>인벤토리</b> (N)` — 개수 골드 강조 |
| 빈 상태 | `획득한 아이템 없음` + 작은 안내 `아이템에 다가가 [E]로 획득하세요` |
| 항목 | `• [타입] 이름` — 타입 태그 파랑 |
| 최근 획득 | 방금 얻은 항목에 `NEW` 배지 (`newBadgeDurationSeconds` 초) |

---

## 9. 입력·커서 (우클릭 시점 회전)

프로젝트는 **Input System Package 전용** (`activeInputHandler: 1`).

### `ItemInteractionCursorInput` 동작

| 상태 | `cursorInputForLook` | `Cursor.lockState` | `Cursor.visible` |
|------|----------------------|--------------------|------------------|
| 평소 (상호작용) | `false` | `None` | `true` |
| **우클릭 Hold** | `true` | `Locked` | `false` |

- `StarterAssetsInputs`를 `Player` Tag 오브젝트에서 자동 탐색
- `cursorLocked = false` 고정 → 포커스 복귀 시 Starter Assets가 항상 Lock 하지 않음
- RMB 해제 시 `look = Vector2.zero`로 잔여 시점 입력 제거

### Input System 사용처

| 기능 | API |
|------|-----|
| E키 획득 | `Keyboard.current.eKey.wasPressedThisFrame` |
| 마우스 위치 | `Mouse.current.position.ReadValue()` |
| 우클릭 Hold | `Mouse.current.rightButton.isPressed` |
| UI Ray 차단 | `blockingUiRoots` + `GraphicRaycaster.Raycast` (Inventory만) |

**주의**: `StandaloneInputModule`은 이 프로젝트에서 사용 불가 → `UI_EventSystem` prefab 필수.

---

## 10. 시각화 기능

Play 모드 Game 뷰에서 RayCast·상호작용 결과를 확인할 수 있습니다.

### (1) LineRenderer 광선

| 상태 | 색상 |
|------|------|
| Miss (`None`) | 노랑 (`debugMissColor`) |
| 원거리 (`OutOfRange`) | 주황 (`reticleOutOfRangeColor`) |
| 근접·획득 가능 | 녹색 (`debugHitColor`) |
| 근접·획득 불가 | 빨강 (`highlightBlockedColor`) |

- `showRayLine = true`: 런타임 `RayLineRenderer` 자식 자동 생성
- 카메라 Ray **origin** → Hit **point** (마우스 Ray 방향)
- UI 위 포인터 / 우클릭 Hold 중에는 LineRenderer를 숨겨 월드 상호작용 중단 상태를 명확히 표시

### (2) 아이템 하이라이트 (MaterialPropertyBlock)

- **`InRangeCanPickup` / `InRangeBlocked`일 때만** 적용
- `OutOfRange` / `None`일 때는 하이라이트 해제 (`SetPropertyBlock(null)`)
- 공유 Material 수정 없이 per-renderer 색만 덮어씀

### (3) Pointer Reticle

- `RectTransformUtility.ScreenPointToLocalPointInRectangle`으로 Canvas 로컬 좌표 갱신
- **우클릭 Hold 중 숨김** (시점 회전 전용)
- `raycastTarget = false` (UI 클릭 방해 없음)

### (4) Scene 뷰 Gizmo

`ItemInteractionSystem` 선택 시:

- 플레이어 위치 기준 **`interactionRange`(3m) WireSphere** (청록)
- 현재 **근접 타겟**(`currentItem`) 위치 WireCube (녹색/빨강)

### (5) Debug.DrawLine

- `drawDebugRay = true` (기본): Scene/Game Gizmos 활성화 시 보조 선
- LineRenderer와 동시 사용 가능

---

## 11. Inspector 주요 설정값

### ItemInteractionSystem → ItemInteractionController

| 필드 | 권장값 |
|------|--------|
| Interaction Camera | Main Camera |
| Player Transform | PlayerArmature |
| Raycast Max Distance | 30 |
| Interaction Range | 3 |
| Skip Player Colliders | ✓ |
| Target Stable Frames | 2 |
| Target Release Delay Frames | 3 |
| Blocking Ui Roots | UI/InventoryPanel RectTransform |
| Pointer Reticle | UI/PointerReticle Image |
| Play Monitor | ItemInteractionPlayMonitor |
| Show Ray Line | ✓ |
| Show Item Highlight | ✓ |

### ItemInteractionSystem → ItemInteractionPlayMonitor

| 필드 | 권장값 |
|------|--------|
| Enable Play Monitor | ✓ |
| Log Level | Events (디버깅 시 Verbose) |
| Verbose Log Interval Seconds | 0.5 |
| Log To Console | ✓ |

#### Console 로그 예시 (`[ItemInteraction]` 접두사)

| 로그 | 의미 |
|------|------|
| `TargetChanged \| item=... \| raw=... → stable=...` | 안정 타겟·상태 변경 |
| `Suspended \| reason=UiBlock` | Inventory 위 포인터 |
| `Suspended \| reason=CameraLook` | 우클릭 Hold |
| `UiBlock \| hit=InventoryPanel` | 차단 UI Hit 이름 |
| `UiVisibility \| panel=InteractionPanel \| visible=true` | 패널 표시/숨김 |
| `Pickup \| item=... \| success=true` | E키 획득 성공 |
| `InventoryAdd \| item=... \| total=N` | 인벤토리 추가 |
| `RayVerbose \| ...` | Verbose 레벨, 0.5초 스로틀 |

### ItemInteractionSystem → ItemInteractionCursorInput

| 필드 | 권장값 |
|------|--------|
| Starter Assets Inputs | PlayerArmature의 `StarterAssetsInputs` |

### PlayerArmature → StarterAssetsInputs

| 필드 | 권장값 |
|------|--------|
| Cursor Locked | **false** (Setup 도구가 자동 설정) |

---

## 12. Play 모드 테스트 체크리스트

### 기본 (과제)

- [ ] ItemData 4개 이상, 씬 오브젝트에 각각 연결
- [ ] 아이템 가리키면 이름/설명 UI 표시
- [ ] 대상 없을 때 InteractionPanel 숨김
- [ ] SealedRelic 근접 시 "획득할 수 없습니다", E키 무효
- [ ] E키 획득 → InventoryPanel 갱신 + 씬 오브젝트 비활성화
- [ ] Console에 Missing Script / Input 예외 없음

### 마우스 포인터 UX

- [ ] Play 시작 시 **커서 표시**, Reticle이 마우스를 따름
- [ ] 멀리 가리키면 주황 Reticle + "가까이 가서 상호작용하세요", E 무효
- [ ] 3m 이내 접근 후 녹색 Reticle + [E] 획득
- [ ] 우클릭 Hold → 커서 숨김·카메라 회전, Reticle 숨김
- [ ] 우클릭 해제 → 포인터·Reticle·상호작용 복귀
- [ ] InventoryPanel 위 포인터 시 상호작용 Ray/UI 갱신 없음
- [ ] LineRenderer 색: 노랑(Miss) / 주황(원거리) / 녹·빨(근접)
- [ ] WASD 이동 정상
- [ ] InteractionPanel **깜빡임 없음** (아이템 위 마우스 유지)
- [ ] InventoryPanel 위에서 Suspend (`[ItemInteraction] UiBlock` 로그)
- [ ] 모든 UI 한글이 정상 출력 (`\uXXXX` 미표시)
- [ ] 인벤토리 헤더 개수·타입 태그·NEW 배지 연출 확인
- [ ] 프롬프트 `[ E ]` 키캡 + 이름 옆 타입 배지 표시
- [ ] Play Monitor `Events` — 상태 변경 시에만 로그

---

## 13. 에디터 Setup 도구

**메뉴**: Tools → ForClass → Setup RaySample Item Interaction

자동 처리 항목:

- `ItemData` 5종 생성/갱신 (`Assets/01.Scenes/RaySample/ItemData/`)
- `InteractableItems` 5개 프리미티브 + Collider + `InteractableItem`
- `ItemInteractionSystem` (Inventory, Controller, CursorInput, **PlayMonitor**)
- UI Canvas (InteractionPanel, InventoryPanel, PointerReticle)
- InteractionPanel/Text `raycastTarget=false`, Inventory `true`
- `blockingUiRoots`, `targetStableFrames`, `inventoryPanelRect` 연결
- Controller: `raycastMaxDistance=30`, `interactionRange=3`
- `UI_EventSystem` prefab, Player `cursorLocked=false`
- 씬 저장

씬이 손상되었거나 참조가 깨졌을 때 **전체 재구성**용으로 사용합니다.

---

## 14. 선택 확장 (과제 PDF)

| 확장 | 방법 |
|------|------|
| Debug.DrawRay | `drawDebugRay = true` (기본 활성화) |
| 타입별 UI 색상 | `ItemInteractionUI`에서 `itemType`별 Text color |
| 획득 불가 아이템 | `canPickup = false` + `InRangeBlocked` UI |
| Interactable 전용 Layer | Layer 추가 후 `hitLayerMask` 제한 |
| 열쇠로 문 열기 | Key 타입 획득 후 Door 상호작용 스크립트 추가 |

---

## 15. 알려진 이슈 및 해결

| 증상 | 원인 | 해결 |
|------|------|------|
| "The referenced script (Unknown)" | CanvasScaler/GraphicRaycaster/Text GUID 오기입 | Setup 도구 재실행 |
| StandaloneInputModule 예외 | Input System 전용 프로젝트 | `UI_EventSystem` prefab 사용 |
| 아이템이 전혀 감지되지 않음 | Collider 없음 / ItemData 미연결 | `InteractableItem` + Collider 확인 |
| 멀리서만 보이고 E가 안 됨 | **정상** — `interactionRange` 3m 밖 | 플레이어가 가까이 이동 |
| **InteractionPanel 깜빡임** | Panel `raycastTarget=1` + 전역 UI 차단 | Setup 재실행 또는 Panel Raycast 끄기 |
| 가리켜도 UI가 안 뜸 | Inventory 위 / RMB Hold | Inventory 밖으로 이동, RMB 해제 |
| Console 로그 폭주 | Log Level = Verbose | Events로 변경 |
| 시점이 안 돌아감 | `StarterAssetsInputs` 미연결 | CursorInput에 Player Inputs 연결 |
| 커서가 항상 Lock | `cursorLocked = true` | Setup 도구 또는 Inspector에서 false |
| MaterialPropertyBlock Awake 예외 | 필드 초기화에서 `new MaterialPropertyBlock()` | Awake에서 생성 (현재 코드 반영됨) |
| **UI에 `\uXXXX`가 그대로 출력** | 씬 YAML이 한글을 이스케이프(평문/홑따옴표)로 저장 → Unity 미디코딩 | 실제 UTF-8 한글로 저장 (Setup 재실행 또는 m_Text 직접 수정) |
| Rich Text 태그가 글자로 보임 | 해당 Text의 `Rich Text` 꺼짐 | 이름/프롬프트/인벤토리 Text의 `m_RichText: 1` |

---

## 16. 제출 설명 문서에 포함할 내용

1. **ItemData 목록** — 5개 에셋, 타입, canPickup (표 §5 참고)
2. **RayCast** — `ScreenPointToRay(Mouse.position)`, `raycastMaxDistance` 30m, `interactionRange` 3m
3. **상호작용 상태** — `InteractionTargetState` 4종과 UI/획득 규칙
4. **입력 UX** — 마우스 포인터 상호작용 + 우클릭 Hold 시점 회전
5. **Inventory** — `List<ItemData>` 참조, `OnInventoryChanged` 이벤트
6. **시각화** — LineRenderer, MaterialPropertyBlock, Pointer Reticle, Gizmo
7. **UI 안정화** — InteractionPanel Raycast 통과, Inventory만 차단, hysteresis
8. **Play Monitor** — `ItemInteractionLogLevel`, 주요 이벤트 로그
9. **어려웠던 점** — UI·World Ray 피드백 루프, Input System + EventSystem, TPS 커서/시점 분리 등

---

*관련 파일: `Assets/02.Scripts/ForClass/*.cs`, `Assets/Editor/RaySampleItemInteractionSetupTool.cs`*
