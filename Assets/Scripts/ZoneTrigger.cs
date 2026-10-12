using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ZoneTrigger : MonoBehaviour
{
    [SerializeField] private ZoneData zoneData;

    [Tooltip("비워 두면 씬에서 ZoneInfoPanel을 찾아 쓴다.")]
    [SerializeField] private ZoneInfoPanel zoneInfoPanel;

    [Tooltip("이 태그를 가진 오브젝트가 들어올 때만 구역 진입으로 센다.")]
    [SerializeField] private string playerTag = "Player";

    private bool playerInside;

    private void Awake()
    {
        if (zoneData == null)
        {
            Debug.LogError(name + ": Zone Data 칸이 비었습니다.", this);
            enabled = false;
            return;
        }

        if (zoneInfoPanel == null)
        {
            // 프리팹으로 올린 패널은 씬마다 참조가 달라서 비어 있는 게 흔하다.
            zoneInfoPanel = FindAnyObjectByType<ZoneInfoPanel>(FindObjectsInactive.Include);
        }

        if (zoneInfoPanel == null)
        {
            Debug.LogError(
                name + ": 씬에서 ZoneInfoPanel을 찾지 못했습니다. " +
                "Info_Canvas 프리팹을 씬에 올려 주세요.",
                this);
            enabled = false;
            return;
        }

        if (!HasTriggerCollider())
        {
            // 예전처럼 isTrigger를 강제로 켜면, 벽이나 바닥에 붙은 경우 플레이어가 통과해 버린다.
            Debug.LogError(
                name + ": Is Trigger가 켜진 Collider가 없어 구역 진입을 감지할 수 없습니다. " +
                "구역용 BoxCollider를 따로 두고 Is Trigger를 켜 주세요.",
                this);
            enabled = false;
        }
    }

    private void OnDisable()
    {
        // 구역 오브젝트가 꺼질 때 패널에 남은 기록까지 지운다.
        if (playerInside && zoneInfoPanel != null)
        {
            playerInside = false;
            zoneInfoPanel.Hide(zoneData);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInside = true;
        zoneInfoPanel.Show(zoneData);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInside = false;
        zoneInfoPanel.Hide(zoneData);
    }

    private bool HasTriggerCollider()
    {
        foreach (Collider zoneCollider in GetComponents<Collider>())
        {
            if (zoneCollider.isTrigger)
            {
                return true;
            }
        }

        return false;
    }
}
