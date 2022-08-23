using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core.Player
{
    public class PlayerCameraFollow : MonoBehaviour
    {
        public PlayerController player;
        public Transform target;
        public float rotationSpeed;
        public bool UpdateRotationWithTarget;
        public int returnFrames;

        bool previousRotationState;
        int returnFrameCounter;

        private void Update()
        {
            transform.position = target.position;

            // This is used for when we switch UpdateRotation to on; we save the camera's orientation
            if (!previousRotationState & UpdateRotationWithTarget)
            {
                returnFrameCounter = 0;
                player.ResetCameraXRotation();
            }
            // This is used for when we switch UpdateRotation to off; we return to the original vertical rotation
            if (previousRotationState & !UpdateRotationWithTarget)
            {
                returnFrameCounter++;
            }

            // Return camera to a resting position if we exit a state where we were rotating with our target
            if (returnFrameCounter != 0 & returnFrameCounter < returnFrames+1)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(5, player.rotationY, 0), rotationSpeed * Time.deltaTime);
                returnFrameCounter++;
            }

            if (UpdateRotationWithTarget)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, rotationSpeed * Time.deltaTime);
            }

            previousRotationState = UpdateRotationWithTarget;
        }
    }
}