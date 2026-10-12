using UnityEngine;
using UnityEngine.InputSystem;

// 화면 중앙에서 앞으로 레이를 쏘아 Interactable을 찾고, 키를 누르면 실행한다.
public class PlayerInteractor : MonoBehaviour
{
    [Tooltip("비워 두면 자동으로 찾는다. 이 씬은 카메라가 플레이어의 형제라 자동 탐색이 까다롭다.")]
    [SerializeField] private Camera viewCamera;

    [Tooltip("비워 두면 씬에서 InteractionPrompt를 찾는다.")]
    [SerializeField] private InteractionPrompt prompt;

    [SerializeField] private float maxDistance = 3f;

    [Tooltip("상호작용 대상이 있는 레이어만 켜 두면 벽 뒤를 덜 훑는다.")]
    [SerializeField] private LayerMask layers = ~0;

    [Tooltip("켜면 씬 뷰에 조준선을 그리고, 무엇에 맞았는지 콘솔에 남긴다.")]
    [SerializeField] private bool debugRay;

    private Interactable current;
    private string lastHitName;

    private void Awake()
    {
        if (viewCamera == null)
        {
            viewCamera = ResolveCamera();
        }

        if (viewCamera == null)
        {
            Debug.LogError(name + ": 카메라를 찾지 못했습니다. View Camera 칸을 채워 주세요.", this);
            enabled = false;
            return;
        }

        if (prompt == null)
        {
            prompt = FindAnyObjectByType<InteractionPrompt>(FindObjectsInactive.Include);
        }

        if (prompt == null)
        {
            Debug.LogWarning(
                name + ": 씬에 InteractionPrompt가 없습니다. 상호작용은 되지만 안내 문구가 뜨지 않아 " +
                "조준이 됐는지 알 수 없습니다. [Tools > 상호작용 설정 > 안내 문구 UI 만들기]를 실행하세요.",
                this);
        }
    }

    // 자식 → 형제(부모의 하위) → Camera.main 순으로 찾는다.
    private Camera ResolveCamera()
    {
        Camera found = GetComponentInChildren<Camera>(true);
        if (found != null)
        {
            return found;
        }

        // 이 프로젝트는 CONTROLLER_FPS 아래에 MainCamera와 PlayerCapsule이 나란히 있다.
        if (transform.parent != null)
        {
            found = transform.parent.GetComponentInChildren<Camera>(true);
            if (found != null)
            {
                return found;
            }
        }

        return Camera.main;
    }

    private void OnDisable()
    {
        Clear();
    }

    private void Update()
    {
        // 커서가 풀려 있으면 시작 화면이나 메뉴 상태다. 조준도 상호작용도 쉰다.
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Clear();
            return;
        }

        Interactable found = Probe();

        if (found != current)
        {
            current = found;
            ShowPrompt(current == null ? null : current.Prompt);
        }

        if (current == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
        {
            current.Interact();

            // 문구가 열기 ↔ 닫기로 바뀐다.
            ShowPrompt(current.Prompt);
        }
    }

    private Interactable Probe()
    {
        Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // 구역 볼륨 같은 트리거는 무시한다. 플레이어를 감싸고 있어서 레이를 바로 막는다.
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, maxDistance, layers, QueryTriggerInteraction.Ignore);

        Interactable found = hitSomething ? hit.collider.GetComponentInParent<Interactable>() : null;

        if (debugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * maxDistance, found != null ? Color.green : Color.red);

            string hitName = hitSomething ? hit.collider.name : "(아무것도 없음)";
            if (hitName != lastHitName)
            {
                lastHitName = hitName;
                Debug.Log(
                    $"조준: {hitName}" +
                    (hitSomething ? $" / 거리 {hit.distance:0.00}m / 상호작용 {(found != null ? found.name : "없음")}" : string.Empty));
            }
        }

        return found;
    }

    private void Clear()
    {
        if (current == null)
        {
            return;
        }

        current = null;
        ShowPrompt(null);
    }

    private void ShowPrompt(string text)
    {
        if (prompt == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(text))
        {
            prompt.Hide();
        }
        else
        {
            prompt.Show(text);
        }
    }
}
