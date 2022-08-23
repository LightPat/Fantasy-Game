using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LightPat.Editor
{
    public class CloneCapsuleColliderTree : MonoBehaviour
    {
        // This is meant to clone a transform's tree of CapsuleColliders to an object with an identical transform structure
        [MenuItem("Tools/Capsule Colliders/Clone Capsule Collider Tree")]
        private static void NewMenuOption()
        {
            Transform[] selectedObjects = Selection.transforms;

            Transform CapsuleColliderDataTree = null;
            Transform targetCopyObject = null;

            int counter = 0;
            foreach (Transform g in selectedObjects)
            {
                if (g.GetComponent<CapsuleCollider>())
                {
                    CapsuleColliderDataTree = g;
                }
                else
                {
                    targetCopyObject = g;
                }
                if (counter > 1)
                {
                    Debug.LogError("You selected more than 2 objects, exiting without doing anything.");
                    return;
                }
                counter++;
            }

            if (targetCopyObject == null | CapsuleColliderDataTree == null)
            {
                Debug.LogError("One of your selected objects has a CapsuleCollider when it shouldn't or neither object has a CapsuleCollider");
                return;
            }

            CloneCapsuleCollidersInAllChildren(CapsuleColliderDataTree, targetCopyObject);
        }

        static void CloneCapsuleCollidersInAllChildren(Transform root, Transform mirroredRoot)
        {
            if (mirroredRoot.name == root.name)
            {
                CapsuleCollider[] rootCols = root.GetComponents<CapsuleCollider>();
                CapsuleCollider[] cols = new CapsuleCollider[rootCols.Length];

                foreach (CapsuleCollider rootCol in rootCols)
                {
                    CapsuleCollider col = mirroredRoot.gameObject.AddComponent<CapsuleCollider>();
                    col.center = rootCol.center;
                    col.radius = rootCol.radius;
                    col.height = rootCol.height;
                    col.direction = rootCol.direction;

                    //if (rootCol.direction == 2) // z to x
                    //{
                    //    col.center = new Vector3(rootCol.center.z, rootCol.center.x, -rootCol.center.y);
                    //    col.direction = 0;
                    //}
                    //if (rootCol.direction == 1) // y to x
                    //{
                    //    if (root.position.y < 0)
                    //    {
                    //        col.center = new Vector3(rootCol.center.z, -rootCol.center.x, rootCol.center.y);
                    //    }
                    //    else
                    //    {
                    //        col.center = new Vector3(rootCol.center.z, rootCol.center.x, rootCol.center.y);
                    //    }

                    //    col.direction = 0;
                    //}
                    if (rootCol.direction == 0) // x to y
                    {
                        //col.center = new Vector3(rootCol.center.z, rootCol.center.x, -rootCol.center.y);
                        col.center = new Vector3(rootCol.center.z, -rootCol.center.x, rootCol.center.y);

                        col.direction = 1;
                    }
                }
            }

            for (int i = 0; i < root.childCount; i++)
            {
                if (root.GetChild(i).childCount > 0)
                {
                    CloneCapsuleCollidersInAllChildren(root.GetChild(i), mirroredRoot.GetChild(i));
                }
            }
        }
    }
}
