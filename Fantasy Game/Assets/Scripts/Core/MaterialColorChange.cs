using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialColorChange : MonoBehaviour
{
    public Color[] materialColors;
    Color[] originalColors;
    Renderer[] materialRenderers;

    void Awake()
    {
        List<Renderer> materialRenderersList = new List<Renderer>();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer.material.HasProperty("_Color"))
            {
                materialRenderersList.Add(renderer);
            }
        }
        materialRenderers = materialRenderersList.ToArray();

        originalColors = new Color[materialRenderers.Length];
        for (int i = 0; i < materialRenderers.Length; i++)
        {
            originalColors[i] = materialRenderers[i].material.color;
        }
    }

    public void ResetColors()
    {
        materialColors = originalColors;
        Apply();
    }

    public void Apply()
    {
        for (int i = 0; i < materialColors.Length; i++)
        {
            materialRenderers[i].material.color = materialColors[i];
        }
    }
}
