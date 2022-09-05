using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightPat.Core.Player
{
    public class AirborneAnimationHandler : MonoBehaviour
    {
        public float jumpHeight;
        public float airborneMoveSpeed;

        Animator animator;
        Rigidbody rb;

        private void Start()
        {
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            animator.SetFloat("yVelocity", rb.velocity.y);
            animator.SetBool("falling", !IsGrounded());
        }

        private void FixedUpdate()
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Falling Idle") | animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"))
            {
                Vector3 moveForce = rb.rotation * new Vector3(moveInput.x, 0, moveInput.y) * airborneMoveSpeed;
                moveForce.x -= rb.velocity.x;
                moveForce.z -= rb.velocity.z;
                rb.AddForce(moveForce, ForceMode.VelocityChange);
            }
        }

        void OnJump()
        {
            if (IsAirborne() | IsJumping() | IsLanding() | rb.velocity.y > 1 | animator.IsInTransition(0)) { return; }
            StartCoroutine(Jump());
        }

        public float jumpForceDelay;
        private IEnumerator Jump()
        {
            animator.SetBool("jumping", true);

            float jumpForce = Mathf.Sqrt(jumpHeight * -2 * Physics.gravity.y);
            if (!animator.GetBool("running"))
            {
                yield return new WaitForSeconds(jumpForceDelay);
                rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.VelocityChange);
            }

            yield return new WaitForFixedUpdate();
            animator.SetBool("jumping", false);
        }
        
        Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        bool landingCollisionRunning;
        private void OnCollisionEnter(Collision collision)
        {
            if (landingCollisionRunning) { return; }

            if ((IsAirborne() | IsJumping()) & !IsLanding())
            {
                animator.SetFloat("landingVelocity", collision.relativeVelocity.magnitude);

                landingCollisionRunning = true;
                StartCoroutine(ResetLandingBool());
            }
        }

        private IEnumerator ResetLandingBool()
        {
            yield return new WaitUntil(() => !(IsAirborne() | IsJumping()) & !IsLanding());
            landingCollisionRunning = false;
        }

        bool IsAirborne()
        {
            return animator.GetCurrentAnimatorStateInfo(0).IsTag("Airborne");
        }

        bool IsJumping()
        {
            return animator.GetCurrentAnimatorStateInfo(0).IsTag("Jumping");
        }

        bool IsLanding()
        {
            return animator.GetCurrentAnimatorStateInfo(0).IsTag("Landing");
        }

        public float isGroundedDistance;
        bool IsGrounded()
        {
            RaycastHit hit;
            return rb.SweepTest(Vector3.down, out hit, isGroundedDistance);
        }
    }
}
