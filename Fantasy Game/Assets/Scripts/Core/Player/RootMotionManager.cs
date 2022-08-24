using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core.Player
{
    public class RootMotionManager : MonoBehaviour
    {
        public Vector3 velocity;

        Vector3 oldPos;

        private Animator animator;
        private Rigidbody rb;
        bool prevBoolState;
        public bool forceTransferred;

        private void Start()
        {
            animator = GetComponent<Animator>();
            rb = GetComponentInParent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            velocity = (rb.position - oldPos) / Time.deltaTime;

            
            if (!IsGrounded() & prevBoolState)
            {
                if (!forceTransferred)
                {
                    forceTransferred = true;
                    rb.AddForce(new Vector3(velocity.x, 0, velocity.z), ForceMode.VelocityChange);
                }
            }

            if (IsGrounded() & !prevBoolState)
            {
                forceTransferred = false;
            }

            prevBoolState = IsGrounded();
            oldPos = rb.position;
        }

        float min;
        private void OnAnimatorMove()
        {
            if (rb.velocity.magnitude > min)
            {
                min = rb.velocity.magnitude;
                Debug.Log(min);
            }

            // If we are not in a running jump or our velocity is greater than 3.3, do not apply root motion
            if (!RootMotionCheck()) { return; }

            transform.parent.position += animator.deltaPosition;
            transform.parent.rotation *= animator.deltaRotation;
        }

        bool RootMotionCheck()
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle") & rb.velocity.magnitude > 3.3) { return false; }
            if (animator.GetCurrentAnimatorStateInfo(0).IsTag("Running Jump") & rb.velocity.magnitude > 10.2) { return false; }

            return true;
        }

        bool IsGrounded()
        {
            // TODO this isn't really an elegant solution, if you stand on the edge of something it doesn't realize that you are still grouded
            // If you check for velocity = 0 then you can double jump since the apex of your jump's velocity is 0
            // Check if the player is touching a gameObject under them
            // May need to change 1.5f to be a different number if you switch the asset of the player model

            RaycastHit hit;
            bool bHit = Physics.Raycast(new Vector3(transform.position.x, transform.position.y, transform.position.z), Vector3.up * -1, out hit, 3);
            if (hit.transform == transform.parent)
            {
                return true;
            }
            return bHit;
        }
    }
}
