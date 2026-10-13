using UnityEngine;

public class MaterialSwapper : OptionGroup
{
    [SerializeField] private Renderer[] targets;
    [SerializeField] private Material[] materials;

    public override int OptionCount => materials != null ? materials.Length : 0;

    // 버튼 OnClick에 이미 연결돼 있는 이름이라 바꾸지 않는다.
    public void Apply(int index)
    {
        Select(index);
    }

    protected override void ApplyOption(int index)
    {
        foreach (Renderer target in targets)
        {
            if (target != null)
            {
                target.material = materials[index];
            }
        }
    }

    // 씬에 이미 깔려 있는 재질이 곧 시작값이다.
    protected override int DetectInitialIndex()
    {
        if (targets != null && materials != null)
        {
            foreach (Renderer target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == target.sharedMaterial)
                    {
                        return i;
                    }
                }

                // 첫 번째 쓸 만한 렌더러만 본다. 여기서 못 찾으면 기본값으로 둔다.
                break;
            }
        }

        return base.DetectInitialIndex();
    }
}
