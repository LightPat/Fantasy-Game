using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using LightPat.Core.Player;

namespace LightPat.ProceduralAnimations.Spider
{
    public class HumanoidStairStepController : MonoBehaviour
    {
        public Transform rootTransform;
        public StairStepIKSolver leftFootIK;
        public StairStepIKSolver rightFootIK;
        public float upperRayHeight;
        public float lowerRayHeight;
        public float rayDistance;
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

            frontFoot.rayDistance = rayDistance;
            backFoot.rayDistance = rayDistance + Vector3.Distance(backFoot.transform.position, frontFoot.transform.position);

            // If either foot is lerping, activate the rig, otherwise, deactivate it
            if (leftFootIK.IsMoving() | rightFootIK.IsMoving())
            {
                rig.weight = Mathf.Lerp(rig.weight, 1, 7 * Time.deltaTime);
            }
            else
            {
                rig.weight = Mathf.Lerp(rig.weight, 0, 7 * Time.deltaTime);

                frontFoot.permissionToLerp = false;
                backFoot.permissionToLerp = true;
            }

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
    }
}