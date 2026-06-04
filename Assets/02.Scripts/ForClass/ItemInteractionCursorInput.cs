using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

/// <summary>
/// RaySample 상호작용용 커서 모드를 관리합니다.
/// 평소: 커서 표시 + 마우스 포인터로 상호작용.
/// 우클릭 Hold: 커서 잠금 + Starter Assets 시점 회전.
/// </summary>
public class ItemInteractionCursorInput : MonoBehaviour
{
    [SerializeField] private StarterAssetsInputs starterAssetsInputs;

    private void Awake()
    {
        if (starterAssetsInputs == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                starterAssetsInputs = playerObject.GetComponent<StarterAssetsInputs>();
            }
        }
    }

    private void OnEnable()
    {
        ApplyInteractionCursorMode();
    }

    private void OnDisable()
    {
        ApplyInteractionCursorMode();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            ApplyInteractionCursorMode();
        }
    }

    private void Update()
    {
        if (starterAssetsInputs == null)
        {
            return;
        }

        bool isCameraLookHeld = Mouse.current != null && Mouse.current.rightButton.isPressed;

        if (isCameraLookHeld)
        {
            starterAssetsInputs.cursorInputForLook = true;
            starterAssetsInputs.cursorLocked = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            starterAssetsInputs.cursorInputForLook = false;
            starterAssetsInputs.look = Vector2.zero;
            ApplyInteractionCursorMode();
        }
    }

    /// <summary>
    /// 상호작용 모드: 커서 해제·표시. StarterAssetsInputs.cursorLocked를 false로 두어 포커스 시 자동 Lock을 막습니다.
    /// </summary>
    private void ApplyInteractionCursorMode()
    {
        if (starterAssetsInputs != null)
        {
            starterAssetsInputs.cursorLocked = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
