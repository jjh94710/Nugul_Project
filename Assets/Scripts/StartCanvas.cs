using UnityEngine;
using UnityEngine.UI;

public class StartCanvas : MonoBehaviour
{
    [SerializeField] private Button startButton;

    [Tooltip("체험 시작을 누르면 켜질 플레이어 컨트롤러. 비워 두면 씬에서 찾는다.")]
    [SerializeField] private CameraMovement playerController;

    [Tooltip("체험 시작을 누르면 켤 패널들. 보통 Info_Canvas를 넣는다. 시작 화면이 떠 있는 동안에는 꺼 둔다.")]
    [SerializeField] private GameObject[] showOnStart;

    private void Awake()
    {
        if (startButton == null)
        {
            startButton = GetComponentInChildren<Button>(true);
        }

        if (startButton == null)
        {
            Debug.LogError(name + ": Start Button을 찾지 못했습니다.", this);
        }

        if (playerController == null)
        {
            // 프리팹 에셋은 씬 오브젝트를 가리킬 수 없어서 칸이 비는 게 정상이다.
            playerController = FindAnyObjectByType<CameraMovement>(FindObjectsInactive.Include);
        }

        if (playerController == null)
        {
            Debug.LogError(name + ": 씬에서 CameraMovement를 찾지 못했습니다.", this);
        }

        StopPlayer();

        // 시작 화면 뒤에 설정 패널이 비쳐 보이면 안 된다.
        SetPanels(false);
    }

    private void OnEnable()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(Hide);
        }

        // 시작 화면이 떠 있는 동안에는 마우스로 버튼을 누를 수 있어야 한다.
        StopPlayer();
    }

    private void OnDisable()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(Hide);
        }
    }

    public void Hide()
    {
        SetPanels(true);

        if (playerController != null)
        {
            // 컨트롤러가 켜질 때 커서가 다시 잠긴다.
            playerController.enabled = true;
        }

        gameObject.SetActive(false);
    }

    private void SetPanels(bool visible)
    {
        if (showOnStart == null)
        {
            return;
        }

        foreach (GameObject panel in showOnStart)
        {
            if (panel == null)
            {
                continue;
            }

            // 프로젝트 창의 프리팹을 그대로 끌어다 놓으면 씬이 아니라 에셋을 켜고 끄게 된다.
            // 조용히 넘어가면 패널이 안 뜨는 이유를 찾기 어려우니 여기서 잡는다.
            if (!panel.scene.IsValid())
            {
                Debug.LogError(
                    name + ": Show On Start에 프리팹 에셋이 들어 있습니다 (" + panel.name + "). " +
                    "프로젝트 창이 아니라 하이어라키에 있는 오브젝트를 넣어 주세요.",
                    this);
                continue;
            }

            panel.SetActive(visible);
        }
    }

    private void StopPlayer()
    {
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // 컨트롤러가 한 번도 켜지지 않았으면 OnDisable도 불리지 않으니 여기서 직접 풀어 준다.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
