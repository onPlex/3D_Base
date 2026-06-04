#!/usr/bin/env python3
"""RaySample.unity의 잘못된 GUID와 EventSystem 블록을 수정합니다."""

import re
from pathlib import Path

SCENE_PATH = Path(r"e:\Unity\3D_Base\Assets\01.Scenes\RaySample.unity")

CANVAS_SCALER_BLOCK = 900001203
GRAPHIC_RAYCASTER_BLOCK = 900001204

WRONG_CANVAS_SCALER = "dc42784cf147c0c48a860349bf072ccc"
CORRECT_CANVAS_SCALER = "0cd44c1031e13a943bb63640046fad76"

WRONG_GRAPHIC_RAYCASTER = "0cd44c1031e13a943bb63640046fad76"
CORRECT_GRAPHIC_RAYCASTER = "dc42784cf147c0c48a680349fa168899"

WRONG_TEXT = "5f7201a12d95ffc409444d83181a376b"
CORRECT_TEXT = "5f7201a12d95ffc409449d95f23cf332"

EVENT_SYSTEM_PREFAB = """--- !u!1001 &900001300
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {fileID: 0}
    m_Modifications:
    - target: {fileID: 1992104595683069851, guid: f0271df749728104eac22c3d897fd8ce, type: 3}
      propertyPath: m_Name
      value: UI_EventSystem
      objectReference: {fileID: 0}
    - target: {fileID: 8063073397250431797, guid: f0271df749728104eac22c3d897fd8ce, type: 3}
      propertyPath: m_LocalPosition.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 8063073397250431797, guid: f0271df749728104eac22c3d897fd8ce, type: 3}
      propertyPath: m_LocalPosition.y
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 8063073397250431797, guid: f0271df749728104eac22c3d897fd8ce, type: 3}
      propertyPath: m_LocalPosition.z
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 8063073397250431797, guid: f0271df749728104eac22c3d897fd8ce, type: 3}
      propertyPath: m_LocalRotation.w
      value: 1
      objectReference: {fileID: 0}
    - target: {fileID: 8063073397250431797, guid: f0271df749728104eac22c3d897fd8ce, type: 3}
      propertyPath: m_LocalRotation.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 8063073397250431797, guid: f0271df749728104eac22c3d897fd8ce, type: 3}
      propertyPath: m_LocalRotation.y
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 8063073397250431797, guid: f0271df749728104eac22c3d897fd8ce, type: 3}
      propertyPath: m_LocalRotation.z
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 8063073397250431797, guid: f0271df749728104eac22c3d897fd8ce, type: 3}
      propertyPath: m_LocalEulerAnglesHint.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 8063073397250431797, guid: f0271df749728104eac22c3d897fd8ce, type: 3}
      propertyPath: m_LocalEulerAnglesHint.y
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 8063073397250431797, guid: f0271df749728104eac22c3d897fd8ce, type: 3}
      propertyPath: m_LocalEulerAnglesHint.z
      value: 0
      objectReference: {fileID: 0}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {fileID: 100100000, guid: f0271df749728104eac22c3d897fd8ce, type: 3}
"""


def extract_block(text: str, block_id: int) -> tuple[str, int, int]:
    """YAML 블록의 시작/끝 인덱스를 반환합니다."""
    marker = f"--- !u!114 &{block_id}"
    start = text.find(marker)
    if start == -1:
        raise ValueError(f"Block {block_id} not found")

    next_block = text.find("\n--- !u!", start + 1)
    if next_block == -1:
        end = len(text)
    else:
        end = next_block + 1  # keep leading newline of next block

    return text[start:end], start, end


def fix_guid_in_block(block: str, wrong: str, correct: str) -> str:
    if wrong not in block:
        raise ValueError(f"Expected GUID {wrong} not found in block")
    return block.replace(wrong, correct, 1)


def main() -> None:
    text = SCENE_PATH.read_text(encoding="utf-8")

    # Step 1: Fix CanvasScaler GUID in block 900001203
    cs_block, cs_start, cs_end = extract_block(text, CANVAS_SCALER_BLOCK)
    cs_block = fix_guid_in_block(cs_block, WRONG_CANVAS_SCALER, CORRECT_CANVAS_SCALER)
    text = text[:cs_start] + cs_block + text[cs_end:]

    # Step 2: Fix GraphicRaycaster GUID in block 900001204
    gr_block, gr_start, gr_end = extract_block(text, GRAPHIC_RAYCASTER_BLOCK)
    gr_block = fix_guid_in_block(gr_block, WRONG_GRAPHIC_RAYCASTER, CORRECT_GRAPHIC_RAYCASTER)
    text = text[:gr_start] + gr_block + text[gr_end:]

    # Step 3: Fix all Text GUIDs
    text_count = text.count(WRONG_TEXT)
    text = text.replace(WRONG_TEXT, CORRECT_TEXT)
    print(f"Fixed {text_count} Text GUID(s)")

    # Step 4: Replace manual EventSystem block with PrefabInstance
    event_start = text.find("--- !u!1 &900001300\nGameObject:")
    event_end = text.find("\n--- !u!1660057539 &9223372036854775807")
    if event_start == -1:
        raise ValueError("EventSystem block not found")
    text = text[:event_start] + EVENT_SYSTEM_PREFAB + text[event_end:]

    # Step 5: Update SceneRoots reference
    text = text.replace("- {fileID: 900001301}", "- {fileID: 900001300}")

    SCENE_PATH.write_text(text, encoding="utf-8")
    print("RaySample.unity fixed successfully.")


if __name__ == "__main__":
    main()
