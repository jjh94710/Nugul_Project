using TMPro;
using UnityEngine;

public class ZoneInfoPanel : MonoBehaviour
{
    [SerializeField]
    private TMP_Text zoneNameText;
    [SerializeField]
    private TMP_Text areaText;
    [SerializeField]
    private TMP_Text descriptionText;

    public void Show(string zoneName, float area, string description)
    {
        zoneNameText.text = zoneName;
        areaText.text = $"전용면적 {area}㎡";
        descriptionText.text = description;

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void TestShow()
    {
        Show("거실", 32.4f, "남향으로 자연광이 들어오는 거실입니다.");
    }


}
