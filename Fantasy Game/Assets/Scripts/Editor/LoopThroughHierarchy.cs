using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LightPat
{
    public class LoopThroughHierarchy : MonoBehaviour
    {
        private static void NewMenuOption()
        {
            PhysicMaterial mat = (PhysicMaterial)AssetDatabase.LoadAssetAtPath("Assets/Player.physicMaterial", typeof(PhysicMaterial));
            Transform root = Selection.activeTransform;

            LoopThrough(root, mat);
        }

        static void LoopThrough(Transform root, PhysicMaterial mat)
        {
            if (root.GetComponent<CapsuleCollider>())
            {
                CapsuleCollider[] col = root.GetComponents<CapsuleCollider>();
                foreach (CapsuleCollider c in col)
                {
                    c.material = mat;
                }
            }

            for (int i = 0; i < root.childCount; i++)
            {
                if (root.GetChild(i).childCount > 0)
                {
                    LoopThrough(root.GetChild(i), mat);
                }
            }
        }
    }
}
