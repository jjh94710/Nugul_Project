using UnityEngine;

[CreateAssetMenu(fileName = "ZoneData", menuName = "ModelHouse/ZoneData")]
public class ZoneData : ScriptableObject
{
    [SerializeField] private string zoneName;
    [SerializeField] private float area;

    [TextArea]
    [SerializeField] private string description;

    public string ZoneName => zoneName;

    public float Area => area;

    public string Description => description;
}
