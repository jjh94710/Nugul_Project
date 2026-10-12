using UnityEngine;

public class FurnitureSlot : MonoBehaviour
{
    [SerializeField] private GameObject[] variants;

    public void Activate(int index)
    {
        for (int i = 0; i < variants.Length; i++)
        {
            variants[i].SetActive(i == index);
        }
    }
}