using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Util;

namespace LightPat.Core.Player
{
    public class PlayerCameraFollow : MonoBehaviour
    {
        public PlayerController playerController;
        public Transform target;
        public float zRotDecay;
        public bool updateRotationWithTarget;

        bool previousRotationState;

        private void Update()
        {
            transform.position = target.position;

            if (updateRotationWithTarget & !previousRotationState)
            {
                playerController.aimRig.weightTarget = 0;
                playerController.disableLookInput = true;
            }
            else if (!updateRotationWithTarget & previousRotationState)
            {
                if (transform.eulerAngles.x > playerController.mouseDownXRotLimit) // If our vertical rotation is greater than the positive look bound, make it a negative angle
                {
                    playerController.rotationX = 360 - transform.eulerAngles.x;
                }
                else
                {
                    playerController.rotationX = transform.eulerAngles.x;
                }
                playerController.rotationY = transform.eulerAngles.y;
                playerController.aimRig.weightTarget = 1;
                playerController.disableLookInput = false;
            }

            if (updateRotationWithTarget)
            {
                transform.rotation = target.rotation;
            }
            else // Interpolate z rotation to 0
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0), Time.deltaTime * zRotDecay);
            }

            previousRotationState = updateRotationWithTarget;
        }
    }
}