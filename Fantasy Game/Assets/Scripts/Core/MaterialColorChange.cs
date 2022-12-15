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
        materialRenderers = GetComponentsInChildren<Renderer>();

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
