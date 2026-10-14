using UnityEngine;

public class FurnitureSlot : OptionGroup
{
    [SerializeField] private GameObject[] variants;

    public override int OptionCount => variants != null ? variants.Length : 0;

    // 버튼 OnClick에 이미 연결돼 있는 이름이라 바꾸지 않는다.
    public void Activate(int index)
    {
        Select(index);
    }

    protected override void ApplyOption(int index)
    {
        for (int i = 0; i < variants.Length; i++)
        {
            if (variants[i] != null)
            {
                variants[i].SetActive(i == index);
            }
        }
    }

    // 씬에 켜 둔 variant가 곧 시작값이다.
    protected override int DetectInitialIndex()
    {
        for (int i = 0; i < OptionCount; i++)
        {
            if (variants[i] != null && variants[i].activeSelf)
            {
                return i;
            }
        }

        return base.DetectInitialIndex();
    }
}
