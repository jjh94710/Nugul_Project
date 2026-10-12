using UnityEngine;

public class MaterialSwapper : MonoBehaviour
{
    [SerializeField] private Renderer[] targets;
    [SerializeField] private Material[] materials;

    public void Apply(int index)
    {
        if (index < 0 || index >= materials.Length)
            return;

        foreach (Renderer target in targets)
        {
            if (target != null)
            {
                target.material = materials[index];
            }
        }
    }
}