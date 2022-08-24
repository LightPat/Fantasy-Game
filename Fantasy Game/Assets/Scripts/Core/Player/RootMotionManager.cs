using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace LightPat.Core.Player
{
    public class RootMotionManager : MonoBehaviour
    {
        Animator animator;
        Rigidbody rb;
        Vector3 transformVelocity;
        Vector3 oldPos;
        bool prevBoolState;
        public bool forceTransferred;

        private void Start()
        {
            animator = GetComponent<Animator>();
            rb = GetComponentInParent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            transformVelocity = (rb.position - oldPos) / Time.deltaTime;

            bool isGrounded = IsGrounded();

            if (!isGrounded & prevBoolState)
            {
                if (!forceTransferred)
                {
                    forceTransferred = true;
                    rb.AddForce(new Vector3(transformVelocity.x, 0, transformVelocity.z), ForceMode.VelocityChange);
                }
            }

            if (isGrounded & !prevBoolState)
            {
                forceTransferred = false;
            }

            prevBoolState = isGrounded;
            oldPos = rb.position;
        }

        public float sweepTestDistanceMultiplier;
        private void OnAnimatorMove()
        {
            // If we are not in a running jump or our velocity is greater than 3.3, do not apply root motion
            if (!PhysicsCheck()) { return; }

            RaycastHit hit;
            if (rb.SweepTest(animator.deltaPosition, out hit, animator.deltaPosition.magnitude * sweepTestDistanceMultiplier))
            {
                return;
            }

            transform.parent.position += animator.deltaPosition;
            transform.parent.rotation *= animator.deltaRotation;
        }

        bool PhysicsCheck()
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle") & rb.velocity.magnitude > 3.3) { return false; }
            if (animator.GetCurrentAnimatorStateInfo(0).IsTag("Running Jump") & rb.velocity.magnitude > 10.2) { return false; }

            return true;
        }

        public float isGroundedDistance;
        bool IsGrounded()
        {
            RaycastHit hit;
            return rb.SweepTest(Vector3.down, out hit, isGroundedDistance);
        }
    }
}
