using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.ProceduralAnimations
{
    public class AimTargetIKSolver : NetworkBehaviour
    {
        public Transform mainCamera;
        public bool disableUpdate;
        public Vector3 offset;

        private void Update()
        {
            if (!IsOwner) { return; }
            if (disableUpdate) { return; }
            if (!mainCamera) { return; }
            transform.position = mainCamera.position + mainCamera.forward * 3 + offset;
        }
    }
}