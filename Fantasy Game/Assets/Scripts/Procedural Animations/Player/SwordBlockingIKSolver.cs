using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class SwordBlockingIKSolver : MonoBehaviour
    {
        public Transform neckBone;
        public Transform rightShoulderBone;
        public Transform leftShoulderBone;
        public Vector3 leftBlockRotation;
        public Vector3 leftMults;
        public Vector3 rightBlockRotation;
        public Vector3 rightMults;
        public float positiveZChangeLimit;
        public float negativeZChangeLimit;
        public float scrollSensitivity;
        public bool disableUpdate;

        Animator animator;
        Vector3 startingLeftRot;
        Vector3 startingRightRot;

        public void ScrollInput(Vector2 input)
        {
            rightBlockRotation.z = Mathf.Clamp(rightBlockRotation.z + input.y * scrollSensitivity, startingRightRot.z + negativeZChangeLimit, startingRightRot.z + positiveZChangeLimit);
            leftBlockRotation.z = Mathf.Clamp(leftBlockRotation.z + input.y * scrollSensitivity, startingLeftRot.z + negativeZChangeLimit, startingLeftRot.z + positiveZChangeLimit);
        }

        public void ResetRotation()
        {
            leftBlockRotation = startingLeftRot;
            rightBlockRotation = startingRightRot;
            animator.SetBool("mirrorIdle", false);
        }

        private void Start()
        {
            animator = GetComponentInParent<Animator>();
            startingLeftRot = leftBlockRotation;
            startingRightRot = rightBlockRotation;
        }

        private void Update()
        {
            if (animator.GetFloat("lookAngle") < 0) // Left block
            {
                transform.rotation = neckBone.rotation * Quaternion.Euler(leftBlockRotation);
                transform.position = neckBone.position + neckBone.right * leftMults.x + neckBone.up * leftMults.y + neckBone.forward * leftMults.z;
            }
            else // Right block
            {
                transform.rotation = neckBone.rotation * Quaternion.Euler(rightBlockRotation);
                transform.position = neckBone.position + neckBone.right * rightMults.x + neckBone.up * rightMults.y + neckBone.forward * rightMults.z;
            }
        }
    }
}
