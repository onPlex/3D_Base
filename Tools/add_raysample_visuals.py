#!/usr/bin/env python3
"""RaySample.unity에 Crosshair UI와 ItemInteractionController 시각화 필드를 추가합니다."""

from pathlib import Path

SCENE_PATH = Path(r"e:\Unity\3D_Base\Assets\01.Scenes\RaySample.unity")

CROSSHAIR_GO = 900001270
CROSSHAIR_RT = 900001271
CROSSHAIR_IMG = 900001272

IMAGE_GUID = "fe87c0e1cc204ed48ad3b37840f39efc"

CROSSHAIR_YAML = f"""--- !u!1 &{CROSSHAIR_GO}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {CROSSHAIR_RT}}}
  - component: {{fileID: {CROSSHAIR_IMG}}}
  m_Layer: 5
  m_Name: Crosshair
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &{CROSSHAIR_RT}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {CROSSHAIR_GO}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 900001201}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0.5, y: 0.5}}
  m_AnchorMax: {{x: 0.5, y: 0.5}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: 12, y: 12}}
  m_Pivot: {{x: 0.5, y: 0.5}}
--- !u!114 &{CROSSHAIR_IMG}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {CROSSHAIR_GO}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {IMAGE_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {{fileID: 0}}
  m_Color: {{r: 1, g: 1, b: 1, a: 0.6}}
  m_RaycastTarget: 0
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {{fileID: 0}}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
"""

CONTROLLER_FIELDS = """  cameraTransform: {fileID: 0}
  interactionDistance: 8
  hitLayerMask:
    serializedVersion: 2
    m_Bits: 4294967295
  queryTriggerInteraction: 1
  inventory: {fileID: 900001102}
  interactionUI: {fileID: 900001205}
  showRayLine: 1
  rayLineWidth: 0.03
  showItemHighlight: 1
  highlightPickupColor: {r: 0.2, g: 1, b: 0.3, a: 1}
  highlightBlockedColor: {r: 1, g: 0.25, b: 0.2, a: 1}
  crosshair: {fileID: 900001272}
  crosshairDefaultColor: {r: 1, g: 1, b: 1, a: 0.6}
  crosshairPickupColor: {r: 0.2, g: 1, b: 0.3, a: 0.9}
  crosshairBlockedColor: {r: 1, g: 0.25, b: 0.2, a: 0.9}
  drawDebugRay: 1
  debugHitColor: {r: 0, g: 1, b: 0, a: 1}
  debugMissColor: {r: 1, g: 0.92156863, b: 0.015686275, a: 1}
  debugRayDuration: 0.1"""


def main() -> None:
    text = SCENE_PATH.read_text(encoding="utf-8")

    if "m_Name: Crosshair" in text:
        print("Crosshair already exists; updating controller fields only.")
    else:
        insert_marker = "--- !u!1660057539 &9223372036854775807"
        text = text.replace(insert_marker, CROSSHAIR_YAML + "\n" + insert_marker)

        old_children = """  m_Children:
  - {fileID: 900001211}
  - {fileID: 900001251}
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 0}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0, y: 0}
--- !u!223 &900001202"""

        new_children = f"""  m_Children:
  - {{fileID: 900001211}}
  - {{fileID: 900001251}}
  - {{fileID: {CROSSHAIR_RT}}}
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0, y: 0}}
  m_AnchorMax: {{x: 0, y: 0}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: 0, y: 0}}
  m_Pivot: {{x: 0, y: 0}}
--- !u!223 &900001202"""

        if old_children not in text:
            raise ValueError("UI Canvas RectTransform children block not found")

        text = text.replace(old_children, new_children)
        print("Added Crosshair UI to Canvas.")

    old_controller = """  cameraTransform: {fileID: 0}
  interactionDistance: 8
  hitLayerMask:
    serializedVersion: 2
    m_Bits: 4294967295
  queryTriggerInteraction: 1
  inventory: {fileID: 900001102}
  interactionUI: {fileID: 900001205}
  drawDebugRay: 1
  debugHitColor: {r: 0, g: 1, b: 0, a: 1}
  debugMissColor: {r: 1, g: 0.92156863, b: 0.015686275, a: 1}
  debugRayDuration: 0.1"""

    if old_controller in text:
        text = text.replace(old_controller, CONTROLLER_FIELDS)
        print("Updated ItemInteractionController serialized fields.")
    elif "showRayLine:" in text:
        print("Controller fields already include visualization settings.")
    else:
        raise ValueError("ItemInteractionController field block not found")

    SCENE_PATH.write_text(text, encoding="utf-8")
    print("RaySample.unity visual feedback injection complete.")


if __name__ == "__main__":
    main()
