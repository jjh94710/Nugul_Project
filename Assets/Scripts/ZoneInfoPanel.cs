using System.Collections;
using TMPro;
using UnityEngine;

public class ZoneInfoPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text zoneNameText;
    [SerializeField] private TMP_Text areaText;
    [SerializeField] private TMP_Text descriptionText;

    [Tooltip("패널을 띄워 둘 시간(초). 0 이하면 자동으로 닫지 않는다.")]
    [SerializeField] private float displaySeconds = 4f;

    private ZoneData current;
    private Coroutine hideRoutine;

    public ZoneData Current => current;

    public void Show(ZoneData data)
    {
        if (data == null)
        {
            Debug.LogError(name + ": ZoneData가 없습니다. ZoneTrigger의 Zone Data 칸을 확인하세요.", this);
            zoneNameText.text = "구역 정보 없음";
            areaText.text = string.Empty;
            descriptionText.text = string.Empty;
            gameObject.SetActive(true);
            return;
        }

        current = data;

        zoneNameText.text = data.zoneName;
        areaText.text = $"전용면적 {data.area}m²";
        descriptionText.text = data.description;

        gameObject.SetActive(true);

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        if (displaySeconds > 0f)
        {
            hideRoutine = StartCoroutine(HideAfterDelay());
        }
    }

    public void Hide(ZoneData data)
    {
        if (data != current)
        {
            return;
        }

        Hide();
    }

    public void Hide()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        current = null;
        gameObject.SetActive(false);
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displaySeconds);

        hideRoutine = null;
        Hide();
    }
}
