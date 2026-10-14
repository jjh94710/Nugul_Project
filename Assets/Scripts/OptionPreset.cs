using System.Collections.Generic;
using System.Text;

// 어느 시점의 옵션 조합 하나. 세션 동안만 메모리에 남고 저장 파일은 만들지 않는다.
public class OptionPreset
{
    public readonly struct Pick
    {
        public Pick(OptionGroup group, int index)
        {
            Group = group;
            Index = index;
        }

        public OptionGroup Group { get; }

        public int Index { get; }
    }

    private readonly List<Pick> picks = new List<Pick>();

    public string Title { get; set; }

    public IReadOnlyList<Pick> Picks => picks;

    public void Add(OptionGroup group, int index)
    {
        if (group == null)
        {
            return;
        }

        picks.Add(new Pick(group, index));
    }

    // 이 조합에서 그 그룹이 몇 번이었는지. 열별로 글자를 만들 때 쓴다.
    public bool TryGetIndex(OptionGroup group, out int index)
    {
        foreach (Pick pick in picks)
        {
            if (pick.Group == group)
            {
                index = pick.Index;
                return true;
            }
        }

        index = -1;
        return false;
    }

    // 저장해 둔 번호를 그룹마다 그대로 다시 먹인다.
    public void Apply()
    {
        foreach (Pick pick in picks)
        {
            // 씬이 바뀌어 그룹이 사라졌을 수 있다.
            if (pick.Group != null)
            {
                pick.Group.Select(pick.Index);
            }
        }
    }

    // "원목, 베이지, 소파 B" 처럼 한 줄로 늘어놓는다.
    public string Describe()
    {
        StringBuilder builder = new StringBuilder();

        foreach (Pick pick in picks)
        {
            if (pick.Group == null)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(pick.Group.GetLabel(pick.Index));
        }

        return builder.Length > 0 ? builder.ToString() : "고른 옵션 없음";
    }
}
