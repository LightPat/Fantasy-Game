using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Util
{
    public class SwordBlockingIKSolver : MonoBehaviour
    {
        public Transform parentBone;
        public Vector3 leftBlockRotation;
        public Vector3 leftMults;
        public Vector3 rightBlockRotation;
        public Vector3 rightMults;
        public float positiveZChangeLimit;
        public float negativeZChangeLimit;
        public float scrollSensitivity;

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
                transform.rotation = parentBone.rotation * Quaternion.Euler(leftBlockRotation);
                transform.position = parentBone.position + parentBone.right * leftMults.x + parentBone.up * leftMults.y + parentBone.forward * leftMults.z;
            }
            else // Right block
            {
                transform.rotation = parentBone.rotation * Quaternion.Euler(rightBlockRotation);
                transform.position = parentBone.position + parentBone.right * rightMults.x + parentBone.up * rightMults.y + parentBone.forward * rightMults.z;
            }
        }
    }
}
