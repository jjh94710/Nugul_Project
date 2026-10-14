using UnityEngine;

public class ConfigTabController : MonoBehaviour
{
    [SerializeField] private GameObject floorContent;
    [SerializeField] private GameObject wallContent;
    [SerializeField] private GameObject furnitureContent;
    [SerializeField] private GameObject lightContent;

    public void ShowFloor()
    {
        ShowOnly(floorContent);
    }

    public void ShowWall()
    {
        ShowOnly(wallContent);
    }

    public void ShowFurniture()
    {
        ShowOnly(furnitureContent);
    }

    public void ShowLight()
    {
        ShowOnly(lightContent);
    }

    private void ShowOnly(GameObject target)
    {
        floorContent.SetActive(target == floorContent);
        wallContent.SetActive(target == wallContent);
        furnitureContent.SetActive(target == furnitureContent);
        lightContent.SetActive(target == lightContent);
    }
}