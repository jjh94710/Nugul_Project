using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 요약 표의 한 줄. 맨 앞 이름표와 열 개수만큼의 값 텍스트를 들고 있다.
// 저장된 조합 줄은 이 틀을 복제해서 만든다.
public class SummaryRow : MonoBehaviour
{
    [Tooltip("줄 맨 앞 이름표. '저장 1' 같은 글자가 들어간다.")]
    [SerializeField] private TMP_Text label;

    [Tooltip("열 순서대로 넣는다. SummaryPanel의 Cells 순서와 같아야 한다.")]
    [SerializeField] private TMP_Text[] values;

    [Tooltip("이 줄을 눌러 조합을 되돌릴 버튼. 없어도 된다.")]
    [SerializeField] private Button button;

    public Button Button => button;

    public void Set(string title, IReadOnlyList<string> columnTexts)
    {
        if (label != null)
        {
            label.text = title;
        }

        if (values == null || columnTexts == null)
        {
            return;
        }

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == null)
            {
                continue;
            }

            values[i].text = i < columnTexts.Count ? columnTexts[i] : string.Empty;
        }
    }
}
