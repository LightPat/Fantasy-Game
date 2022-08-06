using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
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

        private Rig rig;

        private void Start()
        {
            rig = GetComponent<Rig>();
            Time.timeScale = 0.1f;
        }

        private void Update()
        {
            // Check which foot is in front of the other
            StairStepIKSolver frontFoot;
            StairStepIKSolver backFoot;
            if (Vector3.Distance(leftFootIK.footBone.transform.position, rootTransform.position + rootTransform.forward * 5)
                < Vector3.Distance(rightFootIK.footBone.transform.position, rootTransform.position + rootTransform.forward * 5))
            {
                // If the distance between the left foot and rootTransform is greater than the distance between the right foot and the rootTransform
                frontFoot = leftFootIK;
                backFoot = rightFootIK;
            }
            else
            {
                frontFoot = rightFootIK;
                backFoot = leftFootIK;
            }

            // If either foot is lerping, activate the rig, otherwise, deactivate it
            if (leftFootIK.IsMoving() | rightFootIK.IsMoving())
            {
                rig.weight = Mathf.Lerp(rig.weight, 1, 7 * Time.deltaTime);
                Rigidbody rootRigidbody = rootTransform.GetComponent<Rigidbody>();
                //rootRigidbody.velocity = new Vector3(rootRigidbody.velocity.x, 0, rootRigidbody.velocity.z);
            }
            else
            {
                rig.weight = Mathf.Lerp(rig.weight, 0, 7 * Time.deltaTime);

                frontFoot.permissionToLerp = false;
                backFoot.permissionToLerp = true;
            }

            frontFoot.rayDistance = rayDistance;
            backFoot.rayDistance = rayDistance + Vector3.Distance(backFoot.transform.position, frontFoot.transform.position) + 0.1f;

            if (rootTransform.GetComponent<PlayerController>().moveInput == Vector2.zero)
            {
                leftFootIK.permissionToMove = false;
                rightFootIK.permissionToMove = false;
            }
            else
            {
                leftFootIK.permissionToMove = true;
                rightFootIK.permissionToMove = true;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y + 1.5f, transform.position.z), 0.1f);
        }
    }
}