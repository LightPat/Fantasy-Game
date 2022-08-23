using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Misc
{
    public class ApplyRootMotionToParent : MonoBehaviour
    {
        private Animator animator;
        private Rigidbody rb;

        private void Start()
        {
            animator = GetComponent<Animator>();
            rb = GetComponentInParent<Rigidbody>();
        }

        private void OnAnimatorMove()
        {
            if (rb.velocity.y < -3.3 & !IsRunningJump()) { return; }

            transform.parent.position += animator.deltaPosition;
            transform.parent.rotation *= animator.deltaRotation;
        }

        bool IsRunningJump()
        {
            return animator.GetCurrentAnimatorStateInfo(0).IsTag("Running Jump");
        }
    }
}
