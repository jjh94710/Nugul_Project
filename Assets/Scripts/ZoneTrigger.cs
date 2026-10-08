using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    [SerializeField] private ZoneData zoneData;
    [SerializeField] private ZoneInfoPanel zoneInfoPanel;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            zoneInfoPanel.Show(zoneData);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            zoneInfoPanel.Hide();
        }
    }
}