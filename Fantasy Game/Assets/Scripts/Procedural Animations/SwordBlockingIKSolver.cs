using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class SwordBlockingIKSolver : MonoBehaviour
    {
        public Transform parentBone;
        public Vector3 leftBlockRotation;
        public Vector3 leftMults;
        public Vector3 rightBlockRotation;
        public Vector3 rightMults;

        Animator animator;

        private void Start()
        {
            animator = GetComponentInParent<Animator>();
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
