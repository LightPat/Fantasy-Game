using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

namespace LightPat.ProceduralAnimations
{
    public class HumanoidStairStepController : MonoBehaviour
    {
        [Header("New Settings")]
        public Transform rootTransform;
        public StairStepIKSolver leftFootIK;
        public StairStepIKSolver rightFootIK;

        [Header("Old Settings")]
        public Transform footBone;
        public Transform otherFootTarget;
        public float upperRayHeight;
        public float lowerRayHeight;
        // Create an upperRayDistance that goes longer than the lower ray so that you can account for the foot's space on the stair
        public float rayDistance;
        public float stepDistance;
        public float horizontalRayOffset;
        public float lerpSpeedMultiplier;
        public float angularLerpSpeedMultiplier;
        public float minimumLerpSpeed;
        public float stepLandingVerticalOffset;
        public float stepLandingHorizontalOffset;
        public float footSpacing;

        private void Start()
        {
            leftFootIK.permissionToLerp = true;
        }

        private void Update()
        {
            // Do raycasting here and assign a new position to each starStepIKSolver using a method
            /*
            Vector3 upperLeftRayStart = leftFootIK.footBone.position + leftFootIK.transform.forward * horizontalRayOffset;
            upperLeftRayStart.y += upperRayHeight;
            Vector3 lowerLeftRayStart = leftFootIK.footBone.position + leftFootIK.transform.forward * horizontalRayOffset;
            lowerLeftRayStart.y += lowerRayHeight;

            Vector3 upperRightRayStart = rightFootIK.footBone.position + rightFootIK.transform.forward * horizontalRayOffset;
            upperRightRayStart.y += upperRayHeight;
            Vector3 lowerRightRayStart = rightFootIK.footBone.position + rightFootIK.transform.forward * horizontalRayOffset;
            lowerRightRayStart.y += lowerRayHeight;

            Debug.DrawRay(upperLeftRayStart, leftFootIK.transform.forward * rayDistance, Color.black, Time.deltaTime);
            Debug.DrawRay(lowerLeftRayStart, leftFootIK.transform.forward * rayDistance, Color.red, Time.deltaTime);
            Debug.DrawRay(upperRightRayStart, rightFootIK.transform.forward * rayDistance, Color.black, Time.deltaTime);
            Debug.DrawRay(lowerRightRayStart, rightFootIK.transform.forward * rayDistance, Color.red, Time.deltaTime);
            */

            // Handle "Stairs" animation bool

            // Lerp root transform with other feet
            if (leftFootIK.IsMoving() | rightFootIK.IsMoving())
            {
                rootTransform.position += rootTransform.forward * 0.01f;
            }
            
            if (leftFootIK.IsMoving())
            {
                rightFootIK.permissionToLerp = false;
            }
            else
            {
                rightFootIK.permissionToLerp = true;
            }

            if (!leftFootIK.IsMoving() & !rightFootIK.IsMoving())
            {
                leftFootIK.permissionToMove = true;
                rightFootIK.permissionToMove = true;
            }
            else
            {
                leftFootIK.permissionToMove = false;
                rightFootIK.permissionToMove = false;
            }

            // Handle choosing which foot moves first
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y + 1.5f, transform.position.z), 0.1f);
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