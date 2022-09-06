using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Util;

namespace LightPat.Core.Player
{
    public class PlayerCameraFollow : MonoBehaviour
    {
        public PlayerController player;
        public RigWeightTarget aimRig;
        public Transform target;
        public float rotationSpeed;
        public bool updateRotationWithTarget;
        bool previousRotationState;

        private void Update()
        {
            transform.position = target.position;

            if (updateRotationWithTarget)
            {
                //transform.rotation = Quaternion.RotateTowards(transform.rotation, target.rotation, rotationSpeed * Time.deltaTime);
                transform.rotation = target.rotation;

                // Track our vertical rotation
                if (transform.eulerAngles.x > player.mouseDownXRotLimit) // If our number is greater than the positive look bound, make it a negative angle
                {
                    player.rotationX = 360 - transform.eulerAngles.x;
                }
                else
                {
                    player.rotationX = transform.eulerAngles.x;
                }
            }
            else if (previousRotationState) // Set Z rotation to zero once we exit rotating with a bone
            {
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
            }

            previousRotationState = updateRotationWithTarget;
        }
    }
}