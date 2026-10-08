using TMPro;
using UnityEngine;

public class ZoneInfoPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text zoneNameText;
    [SerializeField] private TMP_Text areaText;
    [SerializeField] private TMP_Text descriptionText;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Show(ZoneData data)
    {
        zoneNameText.text = data.zoneName;
        areaText.text = $"전용면적 {data.area}평";
        descriptionText.text = data.description;

        gameObject.SetActive(true);

    }

    public void Hide()
    {
        gameObject.SetActive(false);

      
    }

    
}