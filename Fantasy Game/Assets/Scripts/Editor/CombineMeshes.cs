using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LightPat.Editor
{
    public class CombineMeshes : MonoBehaviour
    {
        [MenuItem("Tools/Combine Selected Meshes")]
        private static void NewMenuOption()
        {
            GameObject[] selectedObjects = Selection.gameObjects;

            MeshFilter[] meshFilters = new MeshFilter[selectedObjects.Length];
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];
            int i = 0;
            foreach (GameObject g in selectedObjects)
            {
                meshFilters[i] = g.GetComponent<MeshFilter>();
                combine[i].mesh = g.GetComponent<MeshFilter>().sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                i++;
            }

            Mesh combinedMesh = new Mesh();
            combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            combinedMesh.CombineMeshes(combine);
            Debug.Log(combinedMesh);

            GameObject n = new GameObject("Combined Mesh");
            n.AddComponent<MeshFilter>();
            n.GetComponent<MeshFilter>().sharedMesh = combinedMesh;
            n.AddComponent<MeshRenderer>();
        }
    }
}
