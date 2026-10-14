using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LightSwitchInteractable : Interactable
{
    [Tooltip("켜고 끌 조명들. 비워 두면 자식에서 찾는다.")]
    [SerializeField] private Light[] lights;

    [SerializeField] private bool startOn = true;

    [SerializeField] private string turnOnPrompt = "조명 켜기";
    [SerializeField] private string turnOffPrompt = "조명 끄기";

    private bool on;

    public override string Prompt => on ? turnOffPrompt : turnOnPrompt;

    public bool IsOn => on;

    private void Awake()
    {
        if (lights == null || lights.Length == 0)
        {
            lights = GetComponentsInChildren<Light>(true);
        }

        if (lights.Length == 0)
        {
            Debug.LogError(name + ": 켤 조명이 없습니다. Lights 칸에 Light를 넣어 주세요.", this);
            enabled = false;
            return;
        }

        WarnIfBaked();

        on = startOn;
        Apply();
    }

    public override void Interact()
    {
        on = !on;
        Apply();
    }

    private void Apply()
    {
        foreach (Light target in lights)
        {
            if (target != null)
            {
                target.enabled = on;
            }
        }
    }

    private void WarnIfBaked()
    {
#if UNITY_EDITOR
        foreach (Light target in lights)
        {
            // 베이크된 조명은 라이트맵에 구워져 있어서 런타임에 꺼도 화면이 그대로다.
            if (target != null && target.lightmapBakeType == LightmapBakeType.Baked)
            {
                Debug.LogWarning(
                    target.name + ": Mode가 Baked라 런타임에 꺼도 보이지 않습니다. " +
                    "Realtime이나 Mixed로 바꿔 주세요.",
                    target);
            }
        }
#endif
    }
}
