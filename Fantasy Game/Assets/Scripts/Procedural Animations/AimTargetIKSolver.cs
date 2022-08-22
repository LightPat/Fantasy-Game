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

        private void Update()
        {
            if (!Camera.main.GetComponent<PlayerCameraFollow>().UpdateRotation)
                transform.position = Camera.main.transform.position + Camera.main.transform.forward;
            if (aimRig.weight < 0.1f)
                transform.position = Vector3.Lerp(transform.position, Camera.main.transform.position + Camera.main.transform.forward, Time.deltaTime);
        }
    }
}
