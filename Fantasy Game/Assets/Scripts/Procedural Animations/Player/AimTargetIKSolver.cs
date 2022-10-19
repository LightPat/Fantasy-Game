using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class AimTargetIKSolver : MonoBehaviour
    {
        public Transform mainCamera;
        public bool disableUpdate;
        public Vector3 offset;

        private void Update()
        {
            if (disableUpdate) { return; }
            if (!mainCamera) { return; }
            transform.position = mainCamera.position + mainCamera.forward * 3 + offset;
        }
    }
}