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
        public float speed;

        float previousWeight;

        private void Update()
        {
            if (!Camera.main.GetComponent<PlayerCameraFollow>().UpdateRotation)
                transform.position = Vector3.Lerp(transform.position, Camera.main.transform.position + Camera.main.transform.forward, Time.deltaTime * speed);

            previousWeight = aimRig.weight;
        }
    }
}
