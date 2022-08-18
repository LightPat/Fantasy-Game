using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

namespace LightPat.ProceduralAnimations
{
    public class StairStepIKSolver : MonoBehaviour
    {
        public HumanoidStairStepController controller;
        public Transform footBone;
        public StairStepIKSolver otherFoot;
        public bool permissionToLerp;
        public bool permissionToMove = true;
        public float rayDistance;

        private Vector3 initialLocalPosition;
        private Vector3 newPosition;
        private Vector3 oldPosition;
        private RaycastHit verticalHit;
        private float lerpProgress = 1;
        private float initialFootHeight;

        private void Start()
        {
            rayDistance = controller.rayDistance;
            initialLocalPosition = transform.localPosition;
            newPosition = transform.position;
            oldPosition = transform.position;
        }

        private void Update()
        {
            // Shoot raycasts
            Vector3 upperRayStart = footBone.position + transform.forward * controller.horizontalRayOffset;
            upperRayStart.y += controller.upperRayHeight;

            Vector3 lowerRayStart = footBone.position + transform.forward * controller.horizontalRayOffset;
            lowerRayStart.y += controller.lowerRayHeight;

            //Debug.DrawRay(upperRayStart, transform.forward * rayDistance, Color.black, Time.deltaTime);
            //Debug.DrawRay(lowerRayStart, transform.forward * rayDistance, Color.red, Time.deltaTime);

            RaycastHit lowerHit;
            if (Physics.Raycast(lowerRayStart, transform.forward, out lowerHit, rayDistance))
            {
                if (lowerHit.transform.CompareTag("Stairs")) { return; }
                // If we hit ourself, ignore this frame
                if (lowerHit.transform == controller.rootTransform) { return; }

                if (!Physics.Raycast(upperRayStart, transform.forward, rayDistance))
                {
                    if (lerpProgress >= 1)
                    {
                        // If we are in front of an object short enough for us to step on and we are not interpolating
                        lerpProgress = 0;

                        // Raycast vertically between the endpoints of the upper ray and lower ray to get the top point of the step
                        Vector3 verticalRayStart = upperRayStart + transform.forward * rayDistance;
                        Physics.Raycast(verticalRayStart, Vector3.down, out verticalHit, upperRayStart.y - lowerRayStart.y);
                        Debug.DrawRay(verticalRayStart, Vector3.down * (upperRayStart.y - lowerRayStart.y), Color.green, 5f);

                        newPosition = new Vector3(verticalHit.point.x, verticalHit.point.y + controller.stepLandingVerticalOffset, verticalHit.point.z) + transform.forward * controller.stepLandingHorizontalOffset;
                    }
                }
                else if (lerpProgress >= 1)
                {
                    // If we are against a wall too high for us to step on
                    oldPosition = transform.position;
                }
            }
            else if (lerpProgress >= 1)
            {
                // If we have nothing in front of us
                oldPosition = transform.position;
            }

            //  & controller.rootTransform.GetComponent<PlayerController>().moveInput != Vector2.zero
            if (lerpProgress < 1 & permissionToLerp)
            {
                // Scale lerp speed with velocity
                Rigidbody rb = controller.rootTransform.GetComponent<Rigidbody>();
                float velocityAverage = Mathf.Abs(rb.velocity.x + rb.velocity.y + rb.velocity.z) * controller.lerpSpeedMultiplier;
                float angularVelocityAverage = Mathf.Abs(rb.angularVelocity.x + rb.angularVelocity.y + rb.angularVelocity.z) * controller.angularLerpSpeedMultiplier;
                float lerpSpeed = velocityAverage + angularVelocityAverage;
                if (lerpSpeed < controller.minimumLerpSpeed)
                {
                    lerpSpeed = controller.minimumLerpSpeed;
                }

                Vector3 interpolatedPosition = Vector3.Lerp(oldPosition, newPosition, lerpProgress);
                // Scale height with how high the step is (use verticalHeight.distance)
                interpolatedPosition.y += Mathf.Sin(lerpProgress * Mathf.PI) * (controller.upperRayHeight - verticalHit.distance + controller.stepLandingVerticalOffset);
                transform.position = interpolatedPosition;
                lerpProgress += Time.deltaTime * lerpSpeed;
            }
            else if (permissionToMove)
            {
                // Scale lerp speed with velocity
                Rigidbody rb = controller.rootTransform.GetComponent<Rigidbody>();
                float velocityAverage = Mathf.Abs(rb.velocity.x + rb.velocity.y + rb.velocity.z) * controller.lerpSpeedMultiplier;
                float angularVelocityAverage = Mathf.Abs(rb.angularVelocity.x + rb.angularVelocity.y + rb.angularVelocity.z) * controller.angularLerpSpeedMultiplier;
                float lerpSpeed = velocityAverage + angularVelocityAverage;
                if (lerpSpeed < controller.minimumLerpSpeed)
                {
                    lerpSpeed = controller.minimumLerpSpeed;
                }
                transform.localPosition = Vector3.Lerp(transform.localPosition, initialLocalPosition, lerpSpeed * Time.deltaTime); // lerp to this
            }
        }

        //private Vector3 gizmoPoint;
        //private void OnDrawGizmos()
        //{
        //    Gizmos.color = Color.black;
        //    Gizmos.DrawSphere(oldPosition, 0.1f);

        //    Gizmos.color = Color.gray;
        //    Gizmos.DrawSphere(newPosition, 0.1f);

        //    Gizmos.color = Color.green;
        //    Gizmos.DrawSphere(gizmoPoint, 0.1f);
        //}

        public bool IsMoving()
        {
            return lerpProgress < 1 & lerpProgress != 0;
        }
    }
}
