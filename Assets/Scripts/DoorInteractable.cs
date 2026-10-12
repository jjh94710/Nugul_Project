using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorInteractable : Interactable
{
    [Tooltip("경첩 위치. 오브젝트 피벗 기준의 로컬 좌표다. 피벗이 이미 경첩이면 0으로 둔다.")]
    [SerializeField] private Vector3 hingeLocalPoint = Vector3.zero;

    [Tooltip("회전축. 월드 기준이라 문이 기울어 있어도 위쪽(0,1,0)이면 된다.")]
    [SerializeField] private Vector3 hingeAxis = Vector3.up;

    [Tooltip("열릴 각도. 반대로 열리면 부호를 뒤집는다.")]
    [SerializeField] private float openAngle = 90f;

    [SerializeField] private float openSeconds = 0.6f;
    [SerializeField] private bool startOpen;

    [SerializeField] private string openPrompt = "문 열기";
    [SerializeField] private string closePrompt = "문 닫기";

    private Vector3 closedPosition;
    private Quaternion closedRotation;
    private Vector3 pivot;
    private Vector3 axis;
    private bool open;
    private float angle;
    private float targetAngle;

    public override string Prompt => open ? closePrompt : openPrompt;

    public bool IsOpen => open;

    private void Awake()
    {
        // 닫힌 상태를 기준으로 삼는다. 이후 회전은 전부 여기서 다시 계산한다.
        closedPosition = transform.position;
        closedRotation = transform.rotation;
        pivot = transform.TransformPoint(hingeLocalPoint);

        axis = hingeAxis.sqrMagnitude > 0.0001f ? hingeAxis.normalized : Vector3.up;

        if (startOpen)
        {
            open = true;
            angle = openAngle;
            targetAngle = openAngle;
            Apply(angle);
        }
    }

    public override void Interact()
    {
        open = !open;
        targetAngle = open ? openAngle : 0f;
    }

    private void Update()
    {
        if (Mathf.Approximately(angle, targetAngle))
        {
            return;
        }

        float speed = openSeconds > 0.001f ? Mathf.Abs(openAngle) / openSeconds : Mathf.Infinity;
        angle = Mathf.MoveTowards(angle, targetAngle, speed * Time.deltaTime);

        Apply(angle);
    }

    // 피벗이 경첩이 아니어도 되도록, 회전과 위치를 함께 옮긴다.
    private void Apply(float value)
    {
        Quaternion rotation = Quaternion.AngleAxis(value, axis);

        transform.SetPositionAndRotation(
            pivot + rotation * (closedPosition - pivot),
            rotation * closedRotation);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 hinge = Application.isPlaying ? pivot : transform.TransformPoint(hingeLocalPoint);
        Vector3 up = (hingeAxis.sqrMagnitude > 0.0001f ? hingeAxis.normalized : Vector3.up) * 0.5f;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(hinge, 0.04f);
        Gizmos.DrawLine(hinge - up, hinge + up);
    }
}
