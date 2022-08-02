using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LightPat.Editor
{
    public class CloneCapsuleColliderTree : MonoBehaviour
    {
        // This is meant to mirror a transform's tree of CapsuleColliders across axises
        [MenuItem("Tools/Clone Capsule Collider Tree")]
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
            if (!mirroredRoot.GetComponent<CapsuleCollider>() & root.GetComponent<CapsuleCollider>())
            {
                CapsuleCollider rootCol = root.GetComponent<CapsuleCollider>();
                CapsuleCollider col = mirroredRoot.gameObject.AddComponent<CapsuleCollider>();
                col.center = rootCol.center;
                col.center = new Vector3(col.center.x * -1, col.center.y, col.center.z);
                col.radius = rootCol.radius;
                col.height = rootCol.height;
                col.direction = rootCol.direction;
            }
            else
            {
                Debug.Log(mirroredRoot + " already has a CapsuleCollider");
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
