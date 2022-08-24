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
            animator = GetComponentInChildren<Animator>();
            rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            animator.SetFloat("yVelocity", rb.velocity.y);
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

        bool jumping;
        void OnJump()
        {
            if (IsAirborne() | IsJumping() | IsLanding() | rb.velocity.y > 1) { return; }
            StartCoroutine(Jump());
        }

        private IEnumerator Jump()
        {
            animator.SetBool("jumping", true);

            float jumpForce = Mathf.Sqrt(jumpHeight * -2 * Physics.gravity.y);
            if (animator.GetFloat("y") < 1.1 & animator.GetFloat("y") > -1.1 & animator.GetFloat("x") < 1.1 & animator.GetFloat("x") > -1.1) // Standing Jump
            {
                yield return new WaitForSeconds(0.2f);
                rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.VelocityChange);
            }
            else // Running Jump
            {
                yield return null;
            }

            animator.SetBool("jumping", false);
        }
        
        Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        bool landingCollision;
        private void OnCollisionEnter(Collision collision)
        {
            if ((IsAirborne() | IsJumping()) & !IsLanding())
            {
                if (landingCollision) { return; }

                animator.SetFloat("landingVelocity", collision.relativeVelocity.magnitude);

                Debug.Log(collision.relativeVelocity);

                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Free Fall") | animator.GetCurrentAnimatorStateInfo(0).IsName("Falling On Stomach")) // Free fall 
                {
                    animator.Play("Land Flat On Stomach");
                }
                else if (moveInput.y > 0 & collision.relativeVelocity.magnitude > 10) // If I'm holding W, do the breakfall roll
                {
                    animator.Play("Breakfall Roll");
                }
                else
                {
                    animator.Play("Landing");
                }

                landingCollision = true;
                StartCoroutine(ResetLandingBool());
            }
        }

        private IEnumerator ResetLandingBool()
        {
            yield return null;
            landingCollision = false;
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
    }
}
