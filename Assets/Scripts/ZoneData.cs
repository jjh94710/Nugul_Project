using UnityEngine;

[CreateAssetMenu(fileName = "ZoneData", menuName = "ModelHouse/ZoneData")]
public class ZoneData : ScriptableObject
{
    public string zoneName;
    public float area;

    [TextArea]
    public string description;
}