using TMPro;
using UnityEngine;

// 조준한 오브젝트의 안내 문구를 띄우는 작은 UI.
public class InteractionPrompt : MonoBehaviour
{
    [Tooltip("비워 두면 자신이나 자식에서 찾는다.")]
    [SerializeField] private TMP_Text label;

    [Tooltip("문구 앞에 붙는 키 안내. 비우면 문구만 나온다.")]
    [SerializeField] private string keyHint = "E";

    private bool warned;

    private void Awake()
    {
        // 캔버스 루트에 붙이면 숨길 때 캔버스 전체가 꺼진다.
        if (GetComponent<Canvas>() != null)
        {
            Debug.LogWarning(
                name + ": 캔버스 루트에 InteractionPrompt가 붙어 있습니다. " +
                "문구용 텍스트 오브젝트로 옮겨 주세요.",
                this);
        }

        if (label == null)
        {
            label = GetComponentInChildren<TMP_Text>(true);
        }

        if (label == null)
        {
            Debug.LogError(name + ": 문구를 넣을 TMP_Text를 찾지 못했습니다.", this);
            enabled = false;
            return;
        }

        Hide();
    }

    public void Show(string text)
    {
        if (label == null)
        {
            return;
        }

        label.text = string.IsNullOrEmpty(keyHint) ? text : $"[{keyHint}] {text}";
        gameObject.SetActive(true);

        // 부모가 꺼져 있으면 켜도 화면에 안 나온다. 시작 화면 캔버스 밑에 두면 이렇게 된다.
        if (!warned && !gameObject.activeInHierarchy)
        {
            warned = true;
            Debug.LogWarning(
                name + ": 부모 오브젝트가 꺼져 있어 안내 문구가 보이지 않습니다. " +
                "항상 켜져 있는 캔버스(Info_Canvas) 밑으로 옮겨 주세요.",
                this);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
