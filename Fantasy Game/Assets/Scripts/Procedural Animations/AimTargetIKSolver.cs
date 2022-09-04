using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using UnityEngine.Animations.Rigging;
using LightPat.Core.Player;

namespace LightPat.ProceduralAnimations
{
    public class AimTargetIKSolver : MonoBehaviour
    {
        public Rig aimRig;

        private void Update()
        {
            if (!Camera.main.GetComponent<PlayerCameraFollow>().updateRotationWithTarget)
                transform.position = Camera.main.transform.position + Camera.main.transform.forward * 3;
        }
    }
}
