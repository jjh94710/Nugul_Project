using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class CameraMovement : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 3f;
    [SerializeField]
    private float runSpeed = 6f;
    [SerializeField]
    private float gravity = -9.81f;

    [Tooltip("위아래 회전을 적용할 오브젝트. Cinemachine을 쓰면 vcam의 Tracking Target(PlayerCameraRoot)을 넣는다.")]
    [SerializeField]
    private Transform cameraTransform;
    [SerializeField]
    private float spinSpeed = 0.1f;
    [SerializeField]
    private float maxAngle = 80f;

    private CharacterController controller;
    private float verticalSpeed;
    private float pitch;
    private bool ready;
    private bool skipDelta;

    // Esc로 커서를 풀면 false. 화면을 클릭하면 다시 true가 되고 시점 조작이 돌아온다.
    private bool looking;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera != null)
            {
                cameraTransform = childCamera.transform;
            }
        }

        if (cameraTransform == null || cameraTransform == transform || !cameraTransform.IsChildOf(transform))
        {
            Debug.LogError(
                name + ": Camera Transform 칸이 비었거나 이 오브젝트의 자식이 아닙니다. " +
                "자식 중 시점 회전을 맡을 오브젝트를 넣어 주세요.",
                this);
            enabled = false;
            return;
        }

        if (cameraTransform.GetComponent("CinemachineBrain") != null)
        {
            Debug.LogError(
                name + ": Camera Transform에 CinemachineBrain이 붙은 카메라가 들어 있습니다. " +
                "Cinemachine이 매 프레임 회전을 덮어쓰므로 위아래 회전이 동작하지 않습니다. " +
                "대신 vcam의 Tracking Target(PlayerCameraRoot)을 넣어 주세요.",
                this);
            enabled = false;
            return;
        }

        transform.localRotation = Quaternion.Euler(0f, transform.localEulerAngles.y, 0f);

        float startPitch = cameraTransform.localEulerAngles.x;
        if (startPitch > 180f)
        {
            startPitch -= 360f;
        }

        pitch = Mathf.Clamp(startPitch, -maxAngle, maxAngle);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        ready = true;
    }

    private void OnEnable()
    {
        if (!ready)
        {
            // Awake가 설정 오류로 멈춘 상태. 커서를 잠그면 UI를 누를 수 없으니 다시 꺼 둔다.
            enabled = false;
            return;
        }

        SetLooking(true);
    }

    private void OnDisable()
    {
        SetLooking(false);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // 이 콜백은 컴포넌트가 꺼져 있어도 불린다. 시점 조작이 켜져 있을 때만 커서를 다시 잠근다.
        if (!isActiveAndEnabled)
        {
            return;
        }

        // Esc로 풀어 둔 커서는 창으로 돌아와도 그대로 둔다.
        LockCursor(hasFocus && looking);
    }

    private void SetLooking(bool value)
    {
        looking = value;
        LockCursor(value);
    }

    private void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;

        // 커서를 잠그는 순간 쌓여 있던 마우스 이동량이 한꺼번에 들어와 시점이 튄다.
        skipDelta = locked;
    }

    private void Update()
    {
        CursorToggle();

        if (looking)
        {
            Rotation();
        }

        Move();
    }

    // Esc는 커서를 풀고, 빈 화면 클릭은 다시 잠근다.
    private void CursorToggle()
    {
        Keyboard keyboard = Keyboard.current;

        if (looking)
        {
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetLooking(false);
            }

            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
        {
            return;
        }

        // 패널 버튼을 누른 클릭은 시점 복귀로 보지 않는다.
        EventSystem events = EventSystem.current;
        if (events != null && events.IsPointerOverGameObject())
        {
            return;
        }

        SetLooking(true);
    }

    public void Rotation()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (skipDelta)
        {
            skipDelta = false;
            return;
        }

        Vector2 delta = mouse.delta.ReadValue() * spinSpeed;

        transform.Rotate(Vector3.up, delta.x, Space.Self);

        pitch = Mathf.Clamp(pitch - delta.y, -maxAngle, maxAngle);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void Move()
    {
        Keyboard keyboard = Keyboard.current;

        Vector3 input = Vector3.zero;
        float speed = moveSpeed;

        // 커서를 풀어 둔 동안에는 이동도 멈추고 중력만 적용한다.
        if (keyboard != null && looking)
        {
            if (keyboard.wKey.isPressed)
            {
                input.z += 1f;
            }
            if (keyboard.sKey.isPressed)
            {
                input.z -= 1f;
            }
            if (keyboard.dKey.isPressed)
            {
                input.x += 1f;
            }
            if (keyboard.aKey.isPressed)
            {
                input.x -= 1f;
            }

            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            speed = keyboard.leftShiftKey.isPressed ? runSpeed : moveSpeed;
        }

        Vector3 move = transform.TransformDirection(input) * speed;

        if (controller.isGrounded && verticalSpeed < 0f)
        {
            verticalSpeed = -2f;
        }

        verticalSpeed += gravity * Time.deltaTime;
        move.y = verticalSpeed;

        controller.Move(move * Time.deltaTime);
    }
}
