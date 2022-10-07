using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class SwordBlockingIKSolver : MonoBehaviour
    {
        public Transform parentBone;
        public Vector3 localPosition;
        public Vector3 localRotation;
        public bool inverse;

        private void Update()
        {
            if (!inverse)
            {
                transform.rotation = parentBone.rotation * Quaternion.Euler(localRotation);
                transform.position = parentBone.position + transform.rotation * localPosition;
            }
            else
            {
                transform.rotation = parentBone.rotation * Quaternion.Euler(localRotation);
                transform.position = parentBone.position + transform.rotation * new Vector3(localPosition.x * -1, localPosition.y, localPosition.z);
            }
        }
    }
}
