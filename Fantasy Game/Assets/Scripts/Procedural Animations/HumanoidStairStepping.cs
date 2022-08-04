using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace LightPat.ProceduralAnimations
{
    public class HumanoidStairStepping : MonoBehaviour
    {
        public Transform rootTransform;
        public Transform footBone;
        public Transform otherFootTarget;
        public float upperRayHeight;
        public float lowerRayHeight;
        public float rayDistance;
        public float horizontalRayOffset;
        public float walkingLerpSpeed;
        public float runningLerpSpeed;

        private Vector3 upperRayStart;
        private Vector3 lowerRayStart;
        private Vector3 initialLocalPosition;
        private Vector3 newPosition;
        private Vector3 oldPosition;
        
        private float lerpProgress = 1;

        private void Start()
        {
            // The upper ray starts at a set height above the position of the foot
            initialLocalPosition = transform.localPosition;
            newPosition = transform.position;
            oldPosition = transform.position;
        }

        private void Update()
        {
            upperRayStart = new Vector3(footBone.position.x, footBone.position.y + upperRayHeight, footBone.position.z + horizontalRayOffset);
            lowerRayStart = new Vector3(footBone.position.x, footBone.position.y + lowerRayHeight, footBone.position.z + horizontalRayOffset);

            Debug.DrawRay(upperRayStart, transform.forward * rayDistance, Color.black, Time.deltaTime);
            Debug.DrawRay(lowerRayStart, transform.forward * rayDistance, Color.red, Time.deltaTime);

            // If we have a lower hit
            RaycastHit lowerHit;
            if (Physics.Raycast(lowerRayStart, transform.forward, out lowerHit, rayDistance))
            {
                if (lowerHit.transform == rootTransform) { return; }

                if (!Physics.Raycast(upperRayStart, transform.forward, rayDistance))
                {
                    if (lerpProgress >= 1)
                    {
                        // If we are in front of an object short enough for us to step on and we are not interpolating
                        lerpProgress = 0;

                        // Raycast vertically between the upper ray and lower ray to get the top point of the step
                        RaycastHit verticalHit;
                        Physics.Raycast(new Vector3(upperRayStart.x, upperRayStart.y, upperRayStart.z + horizontalRayOffset), Vector3.down, out verticalHit, upperRayStart.y - lowerRayStart.y);
                        Debug.DrawRay(new Vector3(upperRayStart.x, upperRayStart.y, upperRayStart.z + horizontalRayOffset), Vector3.down * (upperRayStart.y - lowerRayStart.y), Color.green, 5f);

                        newPosition = verticalHit.point;
                        transform.position = verticalHit.point;
                    }
                }
                else
                {
                    // If we are against a wall too high for us to step on
                    if (lerpProgress >= 1)
                    {
                        oldPosition = transform.position;
                    }
                }
            }
            else
            {
                // If we have nothing in front of us
                if (lerpProgress >= 1)
                {
                    oldPosition = transform.position;
                }
            }

            if (lerpProgress < 1)
            {
                Vector3 interpolatedPosition = Vector3.Lerp(oldPosition, newPosition, lerpProgress);
                // Scale height with how high the step is (use verticalHeight.distance)
                interpolatedPosition.y += Mathf.Sin(lerpProgress * Mathf.PI) / 2;

                transform.position = interpolatedPosition;

                lerpProgress += Time.deltaTime * walkingLerpSpeed;
            }
            else
            {
                // If we are not lerping
                transform.localPosition = initialLocalPosition;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(oldPosition, 0.1f);

            Gizmos.color = Color.gray;
            Gizmos.DrawSphere(newPosition, 0.1f);
        }
    }
}
