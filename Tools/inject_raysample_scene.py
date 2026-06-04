#!/usr/bin/env python3
"""RaySample.unity에 Item Interaction 씬 오브젝트 YAML을 주입합니다."""

from pathlib import Path

SCENE_PATH = Path(r"e:\Unity\3D_Base\Assets\01.Scenes\RaySample.unity")
MARKER = "ItemInteractionSystem"

ITEM_DATA = {
    "HealingPotion": ("a1b2c3d4e5f6789012345678abcdef01", "Lit", (9.5, 0.5, -1.0), (1, 1, 1), "Capsule"),
    "OldSword": ("a1b2c3d4e5f6789012345678abcdef02", "White", (8.5, 0.5, 0.5), (0.2, 0.2, 1.0), "Cube"),
    "DungeonKey": ("a1b2c3d4e5f6789012345678abcdef03", "Gold", (6.5, 0.3, -0.5), (0.3, 0.3, 0.6), "Cube"),
    "QuestScroll": ("a1b2c3d4e5f6789012345678abcdef04", "Black", (10.0, 0.5, -3.0), (0.5, 0.5, 0.5), "Sphere"),
    "SealedRelic": ("a1b2c3d4e5f6789012345678abcdef05", "Gold", (5.5, 0.5, 0.0), (0.7, 0.7, 0.7), "Capsule"),
}

MATERIALS = {
    "Lit": "26a884ed9e4fff54fa799fb4d75f5e47",
    "White": "26a40101d25caa34b847a6e300ec0296",
    "Black": "245940ef39f0223429bb917230c3512d",
    "Gold": "ae7b56c0d1c613049ac20e0d77ebb873",
}

MESH = {"Cube": 10202, "Sphere": 10207, "Capsule": 10208}

SCRIPTS = {
    "InteractableItem": "ee92ec8692b8e25458563eb3208eeeaf",
    "Inventory": "df1b517ffaa6a35408d72bcd4136b7ef",
    "ItemInteractionController": "a103c713112946245a919f3b39dd5786",
    "ItemInteractionUI": "a3475db329a019346bc5076419fb93e4",
    "InventoryUI": "f7433f7547bd76c4f9e6b573863ac36e",
    "CanvasScaler": "0cd44c1031e13a943bb63640046fad76",
    "GraphicRaycaster": "dc42784cf147c0c48a680349fa168899",
    "Image": "fe87c0e1cc204ed48ad3b37840f39efc",
    "Text": "5f7201a12d95ffc409449d95f23cf332",
    "EventSystem": "76c392e42b5098c458856cdf6ecaaaa1",
}


def script_block(file_id: int, guid: str, go_id: int, fields: str) -> str:
    return f"""--- !u!114 &{file_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
{fields}"""


def transform_block(file_id: int, go_id: int, parent: int, position, scale=(1, 1, 1), children=None) -> str:
    px, py, pz = position
    sx, sy, sz = scale
    child_lines = ""
    if children:
        child_lines = "  m_Children:\n" + "\n".join(f"  - {{fileID: {child}}}" for child in children)
    else:
        child_lines = "  m_Children: []"
    return f"""--- !u!4 &{file_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: {px}, y: {py}, z: {pz}}}
  m_LocalScale: {{x: {sx}, y: {sy}, z: {sz}}}
  m_ConstrainProportionsScale: 0
{child_lines}
  m_Father: {{fileID: {parent}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}"""


def rect_transform_block(file_id: int, go_id: int, parent: int, anchor, pivot, size, children=None) -> str:
    ax, ay = anchor
    px, py = pivot
    sx, sy = size
    child_lines = ""
    if children:
        child_lines = "  m_Children:\n" + "\n".join(f"  - {{fileID: {child}}}" for child in children)
    else:
        child_lines = "  m_Children: []"
    return f"""--- !u!224 &{file_id}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
{child_lines}
  m_Father: {{fileID: {parent}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: {ax}, y: {ay}}}
  m_AnchorMax: {{x: {ax}, y: {ay}}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: {sx}, y: {sy}}}
  m_Pivot: {{x: {px}, y: {py}}}"""


def gameobject_block(file_id: int, name: str, components: list[int], layer=0, active=True) -> str:
    comp_lines = "\n".join(f"  - component: {{fileID: {c}}}" for c in components)
    active_value = 1 if active else 0
    return f"""--- !u!1 &{file_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{comp_lines}
  m_Layer: {layer}
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: {active_value}"""


def build_item(name: str, base_id: int, parent: int, data_guid: str, mat_key: str, pos, scale, shape: str) -> str:
    go = base_id
    tr = base_id + 1
    mf = base_id + 2
    mr = base_id + 3
    col = base_id + 4
    interact = base_id + 5
    mat_guid = MATERIALS[mat_key]
    mesh_id = MESH[shape]

    if shape == "Cube":
        collider_yaml = f"""--- !u!65 &{col}
BoxCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 3
  m_Size: {{x: 1, y: 1, z: 1}}
  m_Center: {{x: 0, y: 0, z: 0}}"""
    elif shape == "Sphere":
        collider_yaml = f"""--- !u!135 &{col}
SphereCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 3
  m_Radius: 0.5
  m_Center: {{x: 0, y: 0, z: 0}}"""
    else:
        collider_yaml = f"""--- !u!136 &{col}
CapsuleCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 2
  m_Radius: 0.5
  m_Height: 2
  m_Direction: 1
  m_Center: {{x: 0, y: 0, z: 0}}"""

    return "\n".join([
        gameobject_block(go, f"Item_{name}", [tr, mf, mr, col, interact]),
        transform_block(tr, go, parent, pos, scale),
        f"""--- !u!33 &{mf}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Mesh: {{fileID: {mesh_id}, guid: 0000000000000000e000000000000000, type: 0}}""",
        f"""--- !u!23 &{mr}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 2
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_ForceMeshLod: -1
  m_MeshLodSelectionBias: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 2100000, guid: {mat_guid}, type: 2}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_GlobalIlluminationMeshLod: 0
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_MaskInteraction: 0
  m_AdditionalVertexStreams: {{fileID: 0}}""",
        collider_yaml,
        script_block(interact, SCRIPTS["InteractableItem"], go, f"  itemData: {{fileID: 11400000, guid: {data_guid}, type: 2}}"),
    ])


def text_component(file_id: int, go_id: int, text: str, font_size: int, alignment: int) -> str:
    return script_block(
        file_id,
        SCRIPTS["Text"],
        go_id,
        f"  m_Material: {{fileID: 0}}\n  m_Color: {{r: 1, g: 1, b: 1, a: 1}}\n  m_RaycastTarget: 0\n  m_Maskable: 1\n  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n  m_FontData:\n    m_Font: {{fileID: 10102, guid: 0000000000000000e000000000000000, type: 0}}\n    m_FontSize: {font_size}\n    m_FontStyle: 0\n    m_BestFit: 0\n    m_MinSize: 1\n    m_MaxSize: 40\n    m_Alignment: {alignment}\n    m_AlignByGeometry: 0\n    m_RichText: 0\n    m_HorizontalOverflow: 0\n    m_VerticalOverflow: 0\n    m_LineSpacing: 1\n  m_Text: {text}",
    )


def image_component(file_id: int, go_id: int, alpha: float) -> str:
    return script_block(
        file_id,
        SCRIPTS["Image"],
        go_id,
        f"  m_Material: {{fileID: 0}}\n  m_Color: {{r: 0, g: 0, b: 0, a: {alpha}}}\n  m_RaycastTarget: 1\n  m_Maskable: 1\n  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n  m_Sprite: {{fileID: 0}}\n  m_Type: 0\n  m_PreserveAspect: 0\n  m_FillCenter: 1\n  m_FillMethod: 4\n  m_FillAmount: 1\n  m_FillClockwise: 1\n  m_FillOrigin: 0\n  m_UseSpriteMesh: 0\n  m_PixelsPerUnitMultiplier: 1",
    )


def main() -> None:
    scene_text = SCENE_PATH.read_text(encoding="utf-8")
    if MARKER in scene_text:
        print("Scene already contains ItemInteractionSystem; skipping.")
        return

    parts: list[str] = []
    items_root_go = 900001001
    items_root_tr = 900001002
    item_transform_ids: list[int] = []

    next_id = 900001010
    for item_name, (data_guid, mat, pos, scale, shape) in ITEM_DATA.items():
        item_transform_ids.append(next_id + 1)
        next_id += 10

    parts.append(gameobject_block(items_root_go, "InteractableItems", [items_root_tr]))
    parts.append(transform_block(items_root_tr, items_root_go, 0, (0, 0, 0), children=item_transform_ids))

    next_id = 900001010
    for item_name, (data_guid, mat, pos, scale, shape) in ITEM_DATA.items():
        parts.append(build_item(item_name, next_id, items_root_tr, data_guid, mat, pos, scale, shape))
        next_id += 10

    system_go = 900001100
    system_tr = 900001101
    inventory = 900001102
    controller = 900001103
    parts.append(gameobject_block(system_go, MARKER, [system_tr, inventory, controller]))
    parts.append(transform_block(system_tr, system_go, 0, (0, 0, 0)))
    parts.append(script_block(inventory, SCRIPTS["Inventory"], system_go, "  items: []"))
    parts.append(script_block(
        controller,
        SCRIPTS["ItemInteractionController"],
        system_go,
        "\n".join([
            "  cameraTransform: {fileID: 0}",
            "  interactionDistance: 3",
            "  hitLayerMask:",
            "    serializedVersion: 2",
            "    m_Bits: 4294967295",
            f"  inventory: {{fileID: {inventory}}}",
            "  interactionUI: {fileID: 900001205}",
            "  drawDebugRay: 1",
            "  debugHitColor: {r: 0, g: 1, b: 0, a: 1}",
            "  debugMissColor: {r: 1, g: 0.92156863, b: 0.015686275, a: 1}",
            "  debugRayDuration: 0.1",
        ]),
    ))

    ui_go = 900001200
    ui_rt = 900001201
    canvas = 900001202
    scaler = 900001203
    raycaster = 900001204
    interaction_ui = 900001205
    inventory_ui = 900001206
    panel_go = 900001210
    panel_rt = 900001211
    panel_img = 900001212
    name_go = 900001220
    name_rt = 900001221
    name_txt = 900001222
    desc_go = 900001230
    desc_rt = 900001231
    desc_txt = 900001232
    prompt_go = 900001240
    prompt_rt = 900001241
    prompt_txt = 900001242
    inv_panel_go = 900001250
    inv_panel_rt = 900001251
    inv_panel_img = 900001252
    inv_list_go = 900001260
    inv_list_rt = 900001261
    inv_list_txt = 900001262

    parts.append(gameobject_block(ui_go, "UI", [ui_rt, canvas, scaler, raycaster, interaction_ui, inventory_ui], layer=5))
    parts.append(rect_transform_block(ui_rt, ui_go, 0, (0, 0), (0, 0), (0, 0), [panel_rt, inv_panel_rt]))
    parts.append(f"""--- !u!223 &{canvas}
Canvas:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ui_go}}}
  m_Enabled: 1
  serializedVersion: 3
  m_RenderMode: 0
  m_Camera: {{fileID: 0}}
  m_PlaneDistance: 100
  m_PixelPerfect: 0
  m_ReceivesEvents: 1
  m_OverrideSorting: 0
  m_OverridePixelPerfect: 0
  m_SortingBucketNormalizedSize: 0
  m_VertexColorAlwaysGammaSpace: 0
  m_AdditionalShaderChannelsFlag: 0
  m_UpdateRectTransformForStandalone: 0
  m_SortingLayerID: 0
  m_SortingOrder: 0
  m_TargetDisplay: 0""")
    parts.append(script_block(scaler, SCRIPTS["CanvasScaler"], ui_go, "  m_UiScaleMode: 0\n  m_ReferencePixelsPerUnit: 100\n  m_ScaleFactor: 1\n  m_ReferenceResolution: {x: 800, y: 600}\n  m_ScreenMatchMode: 0\n  m_MatchWidthOrHeight: 0\n  m_PhysicalUnit: 3\n  m_FallbackScreenDPI: 96\n  m_DefaultSpriteDPI: 96\n  m_DynamicPixelsPerUnit: 1\n  m_PresetInfoIsWorld: 0"))
    parts.append(script_block(raycaster, SCRIPTS["GraphicRaycaster"], ui_go, "  m_IgnoreReversedGraphics: 1\n  m_BlockingObjects: 0\n  m_BlockingMask:\n    serializedVersion: 2\n    m_Bits: 4294967295"))
    parts.append(script_block(
        interaction_ui,
        SCRIPTS["ItemInteractionUI"],
        ui_go,
        "\n".join([
            f"  interactionPanel: {{fileID: {panel_go}}}",
            f"  itemNameText: {{fileID: {name_txt}}}",
            f"  itemDescriptionText: {{fileID: {desc_txt}}}",
            f"  promptText: {{fileID: {prompt_txt}}}",
            "  pickupPromptMessage: '[E] \\uD68D\\uB4DD\\uD558\\uAE30'",
            "  cannotPickupMessage: '\\uD68D\\uB4DD\\uD560 \\uC218 \\uC5C6\\uC2B5\\uB2C8\\uB2E4'",
        ]),
    ))
    parts.append(script_block(
        inventory_ui,
        SCRIPTS["InventoryUI"],
        ui_go,
        "\n".join([
            f"  inventory: {{fileID: {inventory}}}",
            f"  inventoryListText: {{fileID: {inv_list_txt}}}",
            "  emptyMessage: '\\uD68D\\uB4DD\\uD55C \\uC544\\uC774\\uD15C \\uC5C6\\uC74C'",
            "  headerMessage: '\\uC778\\uBCA4\\uD1A0\\uB9AC'",
        ]),
    ))

    parts.append(gameobject_block(panel_go, "InteractionPanel", [panel_rt, panel_img], layer=5, active=False))
    parts.append(rect_transform_block(panel_rt, panel_go, ui_rt, (0.5, 0.15), (0.5, 0.5), (420, 160), [name_rt, desc_rt, prompt_rt]))
    parts.append(image_component(panel_img, panel_go, 0.65))

    parts.append(gameobject_block(name_go, "ItemNameText", [name_rt, name_txt], layer=5))
    parts.append(rect_transform_block(name_rt, name_go, panel_rt, (0.5, 0.78), (0.5, 0.5), (380, 36)))
    parts.append(text_component(name_txt, name_go, "\\uC544\\uC774\\uD15C \\uC774\\uB984", 22, 4))

    parts.append(gameobject_block(desc_go, "ItemDescriptionText", [desc_rt, desc_txt], layer=5))
    parts.append(rect_transform_block(desc_rt, desc_go, panel_rt, (0.5, 0.48), (0.5, 0.5), (380, 56)))
    parts.append(text_component(desc_txt, desc_go, "\\uC544\\uC774\\uD15C \\uC124\\uBA85", 16, 1))

    parts.append(gameobject_block(prompt_go, "PromptText", [prompt_rt, prompt_txt], layer=5))
    parts.append(rect_transform_block(prompt_rt, prompt_go, panel_rt, (0.5, 0.18), (0.5, 0.5), (380, 28)))
    parts.append(text_component(prompt_txt, prompt_go, "[E] \\uD68D\\uB4DD\\uD558\\uAE30", 18, 4))

    parts.append(gameobject_block(inv_panel_go, "InventoryPanel", [inv_panel_rt, inv_panel_img], layer=5))
    parts.append(rect_transform_block(inv_panel_rt, inv_panel_go, ui_rt, (0.02, 0.98), (0.0, 1.0), (260, 220), [inv_list_rt]))
    parts.append(image_component(inv_panel_img, inv_panel_go, 0.55))
    parts.append(gameobject_block(inv_list_go, "InventoryListText", [inv_list_rt, inv_list_txt], layer=5))
    parts.append(rect_transform_block(inv_list_rt, inv_list_go, inv_panel_rt, (0.5, 0.5), (0.5, 0.5), (230, 190)))
    parts.append(text_component(inv_list_txt, inv_list_go, "\\uC778\\uBCA4\\uD1A0\\uB9AC\\n\\uD68D\\uB4DD\\uD55C \\uC544\\uC774\\uD15C \\uC5C6\\uC74C", 16, 0))

    # UI_EventSystem prefab (InputSystemUIInputModule)
    parts.append("""--- !u!1001 &900001300
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
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {fileID: 100100000, guid: f0271df749728104eac22c3d897fd8ce, type: 3}""")

    injection = "\n".join(parts)
    scene_text = scene_text.replace(
        "--- !u!1660057539 &9223372036854775807\nSceneRoots:",
        injection + "\n--- !u!1660057539 &9223372036854775807\nSceneRoots:",
    )
    scene_text = scene_text.replace(
        "  m_Roots:\n  - {fileID: 1160234427}",
        "  m_Roots:\n  - {fileID: 900001002}\n  - {fileID: 900001101}\n  - {fileID: 900001201}\n  - {fileID: 900001300}\n  - {fileID: 1160234427}",
    )

    SCENE_PATH.write_text(scene_text, encoding="utf-8")
    print("Injected RaySample item interaction scene objects.")


if __name__ == "__main__":
    main()
