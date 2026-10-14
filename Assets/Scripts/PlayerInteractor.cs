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

    [Tooltip("조준 판정의 굵기(m). 전구처럼 작은 물체를 겨냥하기 쉬워진다. 0이면 가느다란 레이만 쓴다.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float aimRadius = 0.12f;

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
            // 인스펙터에 꽂아 두는 게 원칙이다. 자동 탐색은 프리팹으로 재사용할 때를 위한 안전망이고,
            // 어느 카메라가 잡혔는지 모른 채 넘어가지 않도록 반드시 알린다.
            viewCamera = ResolveCamera();

            if (viewCamera != null)
            {
                Debug.LogWarning(
                    name + ": View Camera 칸이 비어 " + viewCamera.name + "을(를) 자동으로 골랐습니다. " +
                    "인스펙터에 직접 넣어 주세요.",
                    this);
            }
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

        // 먼저 정확한 레이. 제대로 겨냥했으면 여기서 잡힌다.
        Interactable found = Cast(ray, 0f, out RaycastHit hit, out bool hitSomething);

        // 못 잡았으면 굵은 레이로 한 번 더 본다. 전구처럼 작은 물체를 겨냥하기 쉬워진다.
        if (found == null && aimRadius > 0.001f)
        {
            Interactable wide = Cast(ray, aimRadius, out RaycastHit wideHit, out bool wideHitSomething);

            if (wide != null)
            {
                found = wide;
                hit = wideHit;
                hitSomething = wideHitSomething;
            }
        }

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

    // 구역 볼륨 같은 트리거는 무시한다. 플레이어를 감싸고 있어서 레이를 바로 막는다.
    private Interactable Cast(Ray ray, float radius, out RaycastHit hit, out bool hitSomething)
    {
        if (radius <= 0.001f)
        {
            hitSomething = Physics.Raycast(ray, out hit, maxDistance, layers, QueryTriggerInteraction.Ignore);
        }
        else
        {
            // 플레이어 콜라이더 안에서 구가 시작되면 거리 0으로 걸린다. 조금 앞에서 쏜다.
            float offset = radius * 2f;
            Vector3 origin = ray.origin + ray.direction * offset;

            hitSomething = Physics.SphereCast(
                origin, radius, ray.direction, out hit, maxDistance - offset, layers, QueryTriggerInteraction.Ignore);
        }

        return hitSomething ? hit.collider.GetComponentInParent<Interactable>() : null;
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
