using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ZoneTrigger : MonoBehaviour
{
    [SerializeField] private ZoneData zoneData;
    [SerializeField] private ZoneInfoPanel zoneInfoPanel;

    private void Awake()
    {
        if (zoneData == null || zoneInfoPanel == null)
        {
            Debug.LogError(name + ": Zone Data 또는 Zone Info Panel 칸이 비었습니다.", this);
            enabled = false;
            return;
        }

        Collider zoneCollider = GetComponent<Collider>();
        if (!zoneCollider.isTrigger)
        {
            Debug.LogWarning(name + ": Collider의 Is Trigger가 꺼져 있어 켭니다. 씬에도 체크해 두세요.", this);
            zoneCollider.isTrigger = true;
        }
    }

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
            zoneInfoPanel.Hide(zoneData);
        }
    }
}
