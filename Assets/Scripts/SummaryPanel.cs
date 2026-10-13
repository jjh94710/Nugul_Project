using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 선택 옵션 요약 패널.
// 지금 고른 옵션을 표에 채우고, 조합을 저장해 뒀다가 누르면 통째로 되돌린다.
public class SummaryPanel : MonoBehaviour
{
    // 표의 칸 하나. 칸 하나에 그룹 여러 개를 묶으면 쉼표로 이어 붙인다.
    [System.Serializable]
    private class Cell
    {
        [Tooltip("글자를 쓸 텍스트")]
        public TMP_Text text;

        [Tooltip("이 칸이 보여 줄 옵션 그룹들")]
        public OptionGroup[] groups;

        [Tooltip("그룹이 하나도 없는 칸에 쓸 글자")]
        public string emptyLabel = "변경 불가";
    }

    [Tooltip("켜고 끌 오브젝트들. DimBackground 처럼 팝업 바깥에 있는 가림막도 같이 넣어야 한다. " +
             "비우면 이 오브젝트를 켜고 끈다.")]
    [SerializeField] private GameObject[] popupRoots;

    [Tooltip("표의 칸들. 비워 두면 씬에 있는 옵션 그룹을 전부 한 줄로 보여 준다.")]
    [SerializeField] private Cell[] cells;

    [Header("저장 목록")]
    [Tooltip("저장된 조합 버튼이 쌓일 부모.")]
    [SerializeField] private RectTransform presetListContent;

    [Tooltip("조합 버튼으로 복제할 틀. 비우면 코드로 간단한 버튼을 만든다.")]
    [SerializeField] private Button presetRowTemplate;

    [Tooltip("최대 몇 개까지 저장할지. 넘으면 가장 오래된 것부터 지운다.")]
    [SerializeField] private int maxPresets = 8;

    [Header("버튼")]
    [Tooltip("Info 패널의 요약 보기 버튼. 꺼져 있는 오브젝트 안에 있어도 된다.")]
    [SerializeField] private Button openButton;

    [Tooltip("돌아가기 버튼.")]
    [SerializeField] private Button closeButton;

    [Tooltip("지금 조합을 목록에 추가하는 버튼.")]
    [SerializeField] private Button saveButton;

    [Tooltip("옵션을 기본값으로 되돌리는 버튼.")]
    [SerializeField] private Button resetButton;

    [Header("플레이어")]
    [Tooltip("패널이 떠 있는 동안 시점 조작을 멈추고 커서를 푼다.")]
    [SerializeField] private bool releaseCursorWhileOpen = true;

    [Tooltip("비워 두면 씬에서 찾는다.")]
    [SerializeField] private CameraMovement playerController;

    private readonly List<OptionPreset> presets = new List<OptionPreset>();
    private readonly List<GameObject> rows = new List<GameObject>();

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
        if (presetRowTemplate != null)
        {
            presetRowTemplate.gameObject.SetActive(false);
        }

        // 프리팹이 켜진 채로 저장돼 있으면 시작하자마자 요약이 떠 있다.
        // 여기서는 커서를 건드리면 안 된다. 시작 화면이 멈춰 둔 컨트롤러를 도로 켜 버린다.
        SetOpen(false, false);

        // 칸을 안 채우면 프리팹에 적어 둔 예시 글자가 그대로 남아서, 선택이 반영 안 되는 것처럼 보인다.
        if (cells == null || cells.Length == 0)
        {
            Debug.LogWarning(
                name + ": Cells가 비어 있어 요약 표를 채우지 못합니다. " +
                "칸마다 텍스트와 옵션 그룹을 연결해 주세요.",
                this);
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

    // 지금 조합을 목록에 추가한다.
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

        preset.Title = $"조합 {presets.Count + 1}";
        presets.Add(preset);

        // 오래된 것부터 밀어낸다. 제목은 순서대로 다시 매긴다.
        while (presets.Count > Mathf.Max(1, maxPresets))
        {
            presets.RemoveAt(0);
        }

        for (int i = 0; i < presets.Count; i++)
        {
            presets[i].Title = $"조합 {i + 1}";
        }

        RebuildRows();
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
        RebuildRows();
    }

    // 모든 옵션을 기본값으로 되돌린다. 초기화 버튼에 연결한다.
    public void ResetOptions()
    {
        foreach (OptionGroup group in CollectGroups())
        {
            group.ResetToDefault();
        }

        Refresh();
    }

    public void Refresh()
    {
        if (cells == null || cells.Length == 0)
        {
            return;
        }

        foreach (Cell cell in cells)
        {
            if (cell == null || cell.text == null)
            {
                continue;
            }

            cell.text.text = BuildCellText(cell);
        }
    }

    private static string BuildCellText(Cell cell)
    {
        if (cell.groups == null || cell.groups.Length == 0)
        {
            return cell.emptyLabel;
        }

        StringBuilder builder = new StringBuilder();

        foreach (OptionGroup group in cell.groups)
        {
            if (group == null)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(group.CurrentLabel);
        }

        return builder.Length > 0 ? builder.ToString() : cell.emptyLabel;
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

    // 칸에 걸어 둔 그룹들을 중복 없이 모은다. 칸이 비어 있으면 씬 전체에서 찾는다.
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

    private void RebuildRows()
    {
        foreach (GameObject row in rows)
        {
            if (row != null)
            {
                Destroy(row);
            }
        }

        rows.Clear();

        if (presetListContent == null)
        {
            Debug.LogWarning(name + ": Preset List Content 가 비어 있어 저장 목록을 그리지 못합니다.", this);
            return;
        }

        for (int i = 0; i < presets.Count; i++)
        {
            int captured = i;
            Button row = CreateRow();
            if (row == null)
            {
                return;
            }

            SetRowLabel(row, $"{presets[i].Title}  ·  {presets[i].Describe()}");
            row.onClick.AddListener(() => ApplyPreset(captured));
            rows.Add(row.gameObject);
        }
    }

    private Button CreateRow()
    {
        if (presetRowTemplate != null)
        {
            Button row = Instantiate(presetRowTemplate, presetListContent);
            row.gameObject.SetActive(true);
            row.onClick.RemoveAllListeners();
            return row;
        }

        // 틀을 안 넣었으면 최소한의 버튼을 만들어 둔다. 모양은 나중에 틀로 바꾸면 된다.
        GameObject go = new GameObject("PresetRow", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(presetListContent, false);
        rect.sizeDelta = new Vector2(0f, 44f);

        Image background = go.GetComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.12f);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 0f);
        labelRect.offsetMax = new Vector2(-12f, 0f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.fontSize = 22f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;

        return go.GetComponent<Button>();
    }

    private static void SetRowLabel(Button row, string value)
    {
        TMP_Text label = row.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = value;
        }
    }
}
