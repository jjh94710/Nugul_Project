using UnityEngine;

// 바꿔 끼울 수 있는 옵션 묶음 하나. 가구 슬롯이든 재질이든 요약 패널은 이 타입으로만 본다.
// 버튼이 부르는 Activate / Apply 같은 이름은 각 자식 클래스가 그대로 들고 있고,
// 실제 적용과 "지금 몇 번이 골라져 있는지" 기록은 여기서 한다.
public abstract class OptionGroup : MonoBehaviour
{
    [Tooltip("요약 패널에 쓸 이름. 비우면 오브젝트 이름을 쓴다.")]
    [SerializeField] private string groupName;

    [Tooltip("옵션별 이름표. 요약에 이 글자가 그대로 나온다. 비우면 '1번' 같은 번호로 보여 준다.")]
    [SerializeField] private string[] optionLabels;

    [Tooltip("초기화를 눌렀을 때 돌아갈 옵션 번호.")]
    [SerializeField] private int defaultIndex;

    private int currentIndex = -1;

    public string GroupName => string.IsNullOrWhiteSpace(groupName) ? name : groupName;

    public abstract int OptionCount { get; }

    public int CurrentIndex => currentIndex;

    public int DefaultIndex => Mathf.Clamp(defaultIndex, 0, Mathf.Max(0, OptionCount - 1));

    public string CurrentLabel => GetLabel(currentIndex);

    public string GetLabel(int index)
    {
        if (index < 0 || index >= OptionCount)
        {
            return "-";
        }

        if (optionLabels != null && index < optionLabels.Length && !string.IsNullOrWhiteSpace(optionLabels[index]))
        {
            return optionLabels[index];
        }

        return $"{index + 1}번";
    }

    protected virtual void Awake()
    {
        // 요약이 처음부터 맞으려면 기록과 실제 화면이 같아야 한다.
        // 다만 여기서 ApplyOption까지 부르면 씬에 세팅해 둔 초기 상태를 덮어쓰게 되니,
        // 이미 적용돼 있는 상태를 읽어서 기록만 남긴다.
        currentIndex = Mathf.Clamp(DetectInitialIndex(), 0, Mathf.Max(0, OptionCount - 1));
    }

    // 버튼이든 저장된 조합이든 옵션을 바꾸는 길은 전부 여기로 모인다.
    public void Select(int index)
    {
        if (OptionCount == 0)
        {
            Debug.LogWarning(name + ": 옵션이 하나도 없습니다.", this);
            return;
        }

        if (index < 0 || index >= OptionCount)
        {
            Debug.LogWarning($"{name}: 옵션 {index} 는 범위를 벗어납니다 (0~{OptionCount - 1}).", this);
            return;
        }

        currentIndex = index;
        ApplyOption(index);
    }

    public void ResetToDefault()
    {
        Select(DefaultIndex);
    }

    protected abstract void ApplyOption(int index);

    // 씬에 저장된 상태에서 시작 번호를 읽어낼 수 있으면 자식이 덮어쓴다.
    protected virtual int DetectInitialIndex()
    {
        return DefaultIndex;
    }
}
