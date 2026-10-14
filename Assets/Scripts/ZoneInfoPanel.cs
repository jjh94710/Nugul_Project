using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ZoneInfoPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text zoneNameText;
    [SerializeField] private TMP_Text areaText;
    [SerializeField] private TMP_Text descriptionText;

    [Tooltip("어느 구역에도 없을 때 패널을 숨긴다. 끄면 마지막으로 머물던 구역을 계속 보여 준다.")]
    [SerializeField] private bool hideWhenOutside = true;

    // 지금 플레이어가 겹쳐 있는 구역들. 마지막 항목이 현재 위치다.
    private readonly List<ZoneData> inside = new List<ZoneData>();

    public ZoneData Current => inside.Count > 0 ? inside[inside.Count - 1] : null;

    private void Awake()
    {
        // 캔버스 루트에 붙이면 패널을 숨길 때 캔버스 전체가 사라진다.
        if (GetComponent<Canvas>() != null)
        {
            Debug.LogWarning(
                name + ": 캔버스 루트에 ZoneInfoPanel이 붙어 있습니다. " +
                "패널 오브젝트로 옮기지 않으면 숨길 때 캔버스 전체가 꺼집니다.",
                this);
        }

        // 프리팹이 켜진 상태로 저장돼 있어서, 첫 구역에 들어가기 전에는 빈 패널이 보인다.
        if (hideWhenOutside)
        {
            gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        // 구역 경계를 넘을 때 Exit가 Enter보다 먼저 오면 패널이 한 프레임 깜빡인다.
        // 그래서 숨기는 일은 그 프레임의 트리거 콜백이 모두 끝난 뒤에 한다.
        if (hideWhenOutside && inside.Count == 0)
        {
            gameObject.SetActive(false);
        }
    }

    // 구역에 들어왔다. 구역이 겹쳐 있으면 가장 나중에 들어온 구역을 보여 준다.
    public void Show(ZoneData data)
    {
        if (data == null)
        {
            Debug.LogError(name + ": ZoneData가 없습니다. ZoneTrigger의 Zone Data 칸을 확인하세요.", this);
            return;
        }

        inside.Remove(data);
        inside.Add(data);

        Refresh();
    }

    // 구역에서 나왔다. 아직 다른 구역 안이면 그 구역 텍스트로 돌아간다.
    public void Hide(ZoneData data)
    {
        if (data == null || !inside.Remove(data))
        {
            return;
        }

        Refresh();
    }

    public void Hide()
    {
        inside.Clear();
        Refresh();
    }

    private void Refresh()
    {
        ZoneData data = Current;

        // 남은 구역이 없으면 LateUpdate가 숨긴다.
        if (data == null)
        {
            return;
        }

        SetText(zoneNameText, data.ZoneName);
        SetText(areaText, $"전용면적 {data.Area}m²");
        SetText(descriptionText, data.Description);

        gameObject.SetActive(true);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}
