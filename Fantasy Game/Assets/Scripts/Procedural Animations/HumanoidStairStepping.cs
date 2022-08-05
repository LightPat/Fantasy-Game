using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

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
        public float lerpSpeedMultiplier;
        public float angularLerpSpeedMultiplier;
        public float minimumLerpSpeed;
        public float stepLandingVerticalOffset;
        public float stepLandingHorizontalOffset;
        public float footSpacing;

        private Vector3 upperRayStart;
        private Vector3 lowerRayStart;
        private Vector3 initialLocalPosition;
        private Vector3 initialOtherLocalPosition;
        private Vector3 newPosition;
        private Vector3 oldPosition;
        private RaycastHit verticalHit;
        private float lerpSpeed;

        private float lerpProgress = 1;
        private float firstFootLerpProgress = 1;
        private float otherFootLerpProgress = 1;

        private void Start()
        {
            // The upper ray starts at a set height above the position of the foot
            initialLocalPosition = transform.localPosition;
            initialOtherLocalPosition = otherFootTarget.localPosition;
            newPosition = transform.position;
            oldPosition = transform.position;
        }

        private void Update()
        {
            upperRayStart = new Vector3(footBone.position.x, footBone.position.y + upperRayHeight, footBone.position.z + horizontalRayOffset);
            lowerRayStart = new Vector3(footBone.position.x, footBone.position.y + lowerRayHeight, footBone.position.z + horizontalRayOffset);

            upperRayStart = RotateAroundPivot(upperRayStart, footBone.position, rootTransform.eulerAngles);
            lowerRayStart = RotateAroundPivot(lowerRayStart, footBone.position, rootTransform.eulerAngles);

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
                        firstFootLerpProgress = 0;
                        otherFootLerpProgress = 0;

                        // Raycast vertically between the upper ray and lower ray to get the top point of the step
                        Vector3 verticalRayStart = upperRayStart + transform.forward * rayDistance;
                        Physics.Raycast(verticalRayStart, Vector3.down, out verticalHit, upperRayStart.y - lowerRayStart.y);
                        Debug.DrawRay(verticalRayStart, Vector3.down * (upperRayStart.y - lowerRayStart.y), Color.green, 1f);

                        newPosition = new Vector3(verticalHit.point.x, verticalHit.point.y + stepLandingVerticalOffset, verticalHit.point.z) + transform.forward * stepLandingHorizontalOffset;
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
                Rigidbody rb = rootTransform.GetComponent<Rigidbody>();
                float velocityAverage = Mathf.Abs(rb.velocity.x + rb.velocity.y + rb.velocity.z) * lerpSpeedMultiplier;
                float angularVelocityAverage = Mathf.Abs(rb.angularVelocity.x + rb.angularVelocity.y + rb.angularVelocity.z) * angularLerpSpeedMultiplier;

                lerpSpeed = velocityAverage + angularVelocityAverage;
                if (lerpSpeed < minimumLerpSpeed)
                {
                    lerpSpeed = minimumLerpSpeed;
                }

                // If we have no moveInput from player, wait at different poses
                if (rootTransform.GetComponent<PlayerController>().moveInput == Vector2.zero)
                {
                    if (firstFootLerpProgress == 0)
                    {
                        return;
                    }
                    else if (firstFootLerpProgress >= 1 & otherFootLerpProgress == 0)
                    {
                        return;
                    }
                }

                if (firstFootLerpProgress < 1)
                {
                    // Lerp first foot
                    Vector3 interpolatedPosition = Vector3.Lerp(oldPosition, newPosition, firstFootLerpProgress);
                    // Scale height with how high the step is (use verticalHeight.distance)
                    interpolatedPosition.y += Mathf.Sin(firstFootLerpProgress * Mathf.PI) * (upperRayHeight - verticalHit.distance);
                    transform.position = interpolatedPosition;
                    firstFootLerpProgress += Time.deltaTime * lerpSpeed;
                    // Finish first foot lerp before second foot lerp
                    lerpProgress += Time.deltaTime * lerpSpeed / 2;
                }
                else if (otherFootLerpProgress < 1)
                {
                    // Lerp second foot
                    Vector3 interpolatedPosition = Vector3.Lerp(new Vector3(oldPosition.x + footSpacing, oldPosition.y, oldPosition.z),
                        RotateAroundPivot(new Vector3(newPosition.x + footSpacing, newPosition.y, newPosition.z), transform.position, transform.eulerAngles),
                        otherFootLerpProgress);
                    interpolatedPosition.y += Mathf.Sin(otherFootLerpProgress * Mathf.PI) * (upperRayHeight - verticalHit.distance);
                    otherFootTarget.position = interpolatedPosition;
                    otherFootLerpProgress += Time.deltaTime * lerpSpeed;
                    // Finish first foot lerp before second foot lerp
                    lerpProgress += Time.deltaTime * lerpSpeed / 2;
                }
            }
            else
            {
                // If we are not lerping
                transform.localPosition = Vector3.Lerp(transform.localPosition, initialLocalPosition, Time.deltaTime * lerpSpeed);
                otherFootTarget.localPosition = Vector3.Lerp(otherFootTarget.localPosition, initialOtherLocalPosition, Time.deltaTime * lerpSpeed);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(oldPosition, 0.1f);

            Gizmos.color = Color.gray;
            Gizmos.DrawSphere(newPosition, 0.1f);
        }

        Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, Vector3 eulerAngles)
        {
            Vector3 dir = point - pivot; // get point direction relative to pivot
            dir = Quaternion.Euler(eulerAngles) * dir; // rotate it
            point = dir + pivot; // calculate rotated point
            return point;
        }
    }
}