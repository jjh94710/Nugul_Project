using UnityEngine;
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
    }

    private void OnEnable()
    {
        SetCursorLock(true);
    }

    private void OnDisable()
    {
        SetCursorLock(false);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorLock(hasFocus);
    }

    private void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void Update()
    {
        Rotation();
        Move();
    }

    public void Rotation()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
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
        if (keyboard == null)
        {
            return;
        }

        Vector3 input = Vector3.zero;

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

        float speed = keyboard.leftShiftKey.isPressed ? runSpeed : moveSpeed;
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
