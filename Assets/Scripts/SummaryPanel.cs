using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 선택 옵션 요약 패널.
// 표의 모든 줄을 같은 틀에서 찍어 낸다. 맨 윗줄이 지금 고른 옵션이고,
// 저장을 누를 때마다 그 아래로 조합이 한 줄씩 쌓인다. 쌓인 줄을 누르면 통째로 되돌아간다.
public class SummaryPanel : MonoBehaviour
{
    // 표의 열 하나. 열 하나에 그룹 여러 개를 묶으면 쉼표로 이어 붙인다.
    [System.Serializable]
    private class Cell
    {
        [Tooltip("이 열이 보여 줄 옵션 그룹들")]
        public OptionGroup[] groups;

        [Tooltip("그룹이 하나도 없는 열에 쓸 글자")]
        public string emptyLabel = "변경 불가";
    }

    [Tooltip("켜고 끌 오브젝트들. DimBackground 처럼 팝업 바깥에 있는 가림막도 같이 넣어야 한다. " +
             "비우면 이 오브젝트를 켜고 끈다.")]
    [SerializeField] private GameObject[] popupRoots;

    [Tooltip("표의 열들. 왼쪽부터 차례대로 넣는다. Saved Row Template의 Values 순서와 같아야 한다.")]
    [SerializeField] private Cell[] cells;

    [Header("표")]
    [Tooltip("모든 줄을 찍어 낼 틀. 첫 줄(현재)이 놓일 자리에 두고 꺼 둔다.")]
    [SerializeField] private SummaryRow rowTemplate;

    [Tooltip("줄 사이 세로 간격. 틀의 부모에 Layout Group이 붙어 있으면 그쪽 Spacing을 쓰고 이 값은 무시한다.")]
    [SerializeField] private float rowSpacing = 50f;

    [Tooltip("맨 윗줄 이름표.")]
    [SerializeField] private string currentRowTitle = "현재";

    [Tooltip("최대 몇 개까지 저장할지. 넘으면 가장 오래된 것부터 지운다.")]
    [SerializeField] private int maxPresets = 3;

    [Header("버튼")]
    [Tooltip("Info 패널의 요약 보기 버튼. 꺼져 있는 오브젝트 안에 있어도 된다.")]
    [SerializeField] private Button openButton;

    [Tooltip("돌아가기 버튼.")]
    [SerializeField] private Button closeButton;

    [Tooltip("지금 조합을 표에 한 줄 추가하는 버튼.")]
    [SerializeField] private Button saveButton;

    [Tooltip("옵션을 되돌리고 저장 줄을 모두 지우는 버튼.")]
    [SerializeField] private Button resetButton;

    [Header("플레이어")]
    [Tooltip("패널이 떠 있는 동안 시점 조작을 멈추고 커서를 푼다.")]
    [SerializeField] private bool releaseCursorWhileOpen = true;

    [Tooltip("비워 두면 씬에서 찾는다.")]
    [SerializeField] private CameraMovement playerController;

    private readonly List<OptionPreset> presets = new List<OptionPreset>();
    private readonly List<SummaryRow> rows = new List<SummaryRow>();

    public bool IsOpen
    {
        get
        {
            foreach (GameObject root in popupRoots)
            {
                if (root != null)
                {
                    return root.activeSelf;
                }
            }

            return false;
        }
    }

    private void Awake()
    {
        if (popupRoots == null || popupRoots.Length == 0)
        {
            popupRoots = new[] { gameObject };
        }

        if (playerController == null)
        {
            // 프리팹 에셋은 씬 오브젝트를 가리킬 수 없어서 칸이 비는 게 정상이다.
            playerController = FindAnyObjectByType<CameraMovement>(FindObjectsInactive.Include);
        }

        // 틀은 복제용이라 화면에 보이면 안 된다.
        if (rowTemplate != null)
        {
            rowTemplate.gameObject.SetActive(false);
        }

        // 프리팹이 켜진 채로 저장돼 있으면 시작하자마자 요약이 떠 있다.
        // 여기서는 커서를 건드리면 안 된다. 시작 화면이 멈춰 둔 컨트롤러를 도로 켜 버린다.
        SetOpen(false, false);

        if (cells == null || cells.Length == 0)
        {
            Debug.LogWarning(
                name + ": Cells가 비어 있어 요약 표를 채우지 못합니다. 열마다 옵션 그룹을 연결해 주세요.",
                this);
        }

        if (rowTemplate == null)
        {
            Debug.LogWarning(name + ": Row Template이 비어 있어 표를 그리지 못합니다.", this);
        }

        Bind(openButton, Show);
        Bind(closeButton, Hide);
        Bind(saveButton, SaveCurrent);
        Bind(resetButton, ResetOptions);
    }

    private void OnDestroy()
    {
        Unbind(openButton, Show);
        Unbind(closeButton, Hide);
        Unbind(saveButton, SaveCurrent);
        Unbind(resetButton, ResetOptions);
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
    }

    private static void Unbind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(action);
        }
    }

    // 요약 보기 버튼에 연결한다.
    public void Show()
    {
        SetOpen(true);
        Refresh();
    }

    // 돌아가기 버튼에 연결한다.
    public void Hide()
    {
        SetOpen(false);
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    // 지금 조합을 표에 한 줄 추가한다.
    public void SaveCurrent()
    {
        List<OptionGroup> groups = CollectGroups();
        if (groups.Count == 0)
        {
            Debug.LogWarning(name + ": 저장할 옵션 그룹을 찾지 못했습니다.", this);
            return;
        }

        OptionPreset preset = new OptionPreset();
        foreach (OptionGroup group in groups)
        {
            preset.Add(group, group.CurrentIndex);
        }

        presets.Add(preset);

        // 오래된 것부터 밀어낸다.
        while (presets.Count > Mathf.Max(1, maxPresets))
        {
            presets.RemoveAt(0);
        }

        Refresh();
    }

    // 저장된 조합을 통째로 적용한다.
    public void ApplyPreset(int index)
    {
        if (index < 0 || index >= presets.Count)
        {
            return;
        }

        presets[index].Apply();
        Refresh();
    }

    public void ClearPresets()
    {
        presets.Clear();
        Refresh();
    }

    // 모든 옵션을 기본값으로 되돌리고 저장해 둔 조합도 지운다. 초기화 버튼에 연결한다.
    public void ResetOptions()
    {
        foreach (OptionGroup group in CollectGroups())
        {
            group.ResetToDefault();
        }

        presets.Clear();
        Refresh();
    }

    public void Refresh()
    {
        foreach (SummaryRow row in rows)
        {
            if (row != null)
            {
                Destroy(row.gameObject);
            }
        }

        rows.Clear();

        if (rowTemplate == null)
        {
            return;
        }

        RectTransform templateRect = rowTemplate.transform as RectTransform;
        if (templateRect == null)
        {
            Debug.LogWarning(name + ": Row Template이 RectTransform이 아닙니다.", this);
            return;
        }

        Transform parent = templateRect.parent;
        Vector2 origin = templateRect.anchoredPosition;

        // 부모가 Layout Group이면 줄 위치는 그쪽이 잡는다. 여기서 건드리면 서로 싸운다.
        bool layoutDriven = parent != null && parent.GetComponent<LayoutGroup>() != null;

        // 0번은 현재 줄, 그 아래로 저장된 조합이 쌓인다.
        for (int slot = 0; slot <= presets.Count; slot++)
        {
            OptionPreset preset = slot == 0 ? null : presets[slot - 1];
            string title = slot == 0 ? currentRowTitle : $"저장 {slot}";

            SummaryRow row = Instantiate(rowTemplate, parent);
            row.gameObject.SetActive(true);

            if (!layoutDriven)
            {
                RectTransform rect = row.transform as RectTransform;
                rect.anchoredPosition = new Vector2(origin.x, origin.y - rowSpacing * slot);
            }

            row.Set(title, BuildTexts(preset));
            BindRow(row, slot);

            rows.Add(row);
        }
    }

    private void BindRow(SummaryRow row, int slot)
    {
        if (row.Button == null)
        {
            return;
        }

        // 틀 바깥의 버튼을 꽂아 두면 복제해도 그 버튼 하나를 모든 줄이 공유한다.
        // 그대로 두면 아래에서 그 버튼의 기존 연결까지 지워 버리니 건드리지 않는다.
        if (!row.Button.transform.IsChildOf(row.transform))
        {
            Debug.LogWarning(
                name + ": Row Template의 Button 칸에 줄 바깥 버튼(" + row.Button.name + ")이 들어 있습니다. " +
                "줄 자신에 붙인 Button을 넣어 주세요.",
                this);
            return;
        }

        row.Button.onClick.RemoveAllListeners();

        // 현재 줄은 되돌릴 대상이 아니다.
        if (slot == 0)
        {
            row.Button.interactable = false;
            return;
        }

        int index = slot - 1;
        row.Button.onClick.AddListener(() => ApplyPreset(index));
    }

    // preset이 null이면 지금 고른 옵션을 글자로 만든다.
    private string[] BuildTexts(OptionPreset preset)
    {
        if (cells == null)
        {
            return new string[0];
        }

        string[] texts = new string[cells.Length];

        for (int i = 0; i < cells.Length; i++)
        {
            Cell cell = cells[i];
            StringBuilder builder = new StringBuilder();

            if (cell != null && cell.groups != null)
            {
                foreach (OptionGroup group in cell.groups)
                {
                    if (group == null)
                    {
                        continue;
                    }

                    if (preset == null)
                    {
                        Append(builder, group.CurrentLabel);
                    }
                    else if (preset.TryGetIndex(group, out int index))
                    {
                        Append(builder, group.GetLabel(index));
                    }
                }
            }

            texts[i] = builder.Length > 0 ? builder.ToString() : (cell != null ? cell.emptyLabel : string.Empty);
        }

        return texts;
    }

    private static void Append(StringBuilder builder, string value)
    {
        if (builder.Length > 0)
        {
            builder.Append(", ");
        }

        builder.Append(value);
    }

    private void SetOpen(bool open, bool affectPlayer = true)
    {
        foreach (GameObject root in popupRoots)
        {
            if (root != null)
            {
                root.SetActive(open);
            }
        }

        if (!affectPlayer || !releaseCursorWhileOpen || playerController == null)
        {
            return;
        }

        // 컨트롤러가 꺼지면 커서가 풀리고, 켜지면 다시 잠긴다.
        playerController.enabled = !open;
    }

    // 열에 걸어 둔 그룹들을 중복 없이 모은다. 열이 비어 있으면 씬 전체에서 찾는다.
    private List<OptionGroup> CollectGroups()
    {
        List<OptionGroup> groups = new List<OptionGroup>();

        if (cells != null)
        {
            foreach (Cell cell in cells)
            {
                if (cell == null || cell.groups == null)
                {
                    continue;
                }

                foreach (OptionGroup group in cell.groups)
                {
                    if (group != null && !groups.Contains(group))
                    {
                        groups.Add(group);
                    }
                }
            }
        }

        if (groups.Count == 0)
        {
            groups.AddRange(FindObjectsByType<OptionGroup>(FindObjectsInactive.Include));
        }

        return groups;
    }
}
