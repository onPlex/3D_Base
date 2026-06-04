using UnityEngine;

/// <summary>
/// Connects a scene item object with an ItemData ScriptableObject.
/// The collider is required so the camera RayCast can hit this object.
/// </summary>
[RequireComponent(typeof(Collider))]
public class InteractableItem : MonoBehaviour
{
    [Tooltip("이 씬 오브젝트가 참조하는 ItemData ScriptableObject")]
    public ItemData itemData;
}
