using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class PlayerCameraFollow : MonoBehaviour
    {
        public RootMotionController player;
        public Transform target;
        public float rotationSpeed;
        public bool UpdateRotation;

        bool previousRotationState;
        private void Update()
        {
            transform.position = target.position;

            if (UpdateRotation)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target.rotation, rotationSpeed * Time.deltaTime);
            }

            // This is used for when we switch UpdateRotation to on, so that when we return we don't flip back to the wrong camera rotation
            if (!previousRotationState & UpdateRotation)
            {
                player.ResetCameraXRotation();
            }

            previousRotationState = UpdateRotation;
        }
    }
}
