using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using UnityEngine.Animations.Rigging;

namespace LightPat.ProceduralAnimations
{
    public class AimTargetIKSolver : MonoBehaviour
    {
        public Rig aimRig;

        float previousWeight;

        private void Update()
        {
            if (!Camera.main.GetComponent<PlayerCameraFollow>().UpdateRotation)
                transform.position = Camera.main.transform.position + Camera.main.transform.forward;

            previousWeight = aimRig.weight;
        }
    }
}
