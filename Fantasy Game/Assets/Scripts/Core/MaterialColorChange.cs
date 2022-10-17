using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialColorChange : MonoBehaviour
{
    public Color[] materialColors;

    void Start()
    {
        Renderer[] materialRenderers = GetComponentsInChildren<Renderer>();

        for (int i = 0; i < materialColors.Length; i++)
        {
            materialRenderers[i].material.color = materialColors[i];
        }
    }
}
