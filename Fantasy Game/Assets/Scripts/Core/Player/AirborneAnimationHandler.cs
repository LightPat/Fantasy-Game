using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

namespace LightPat.Core.Player
{
    public class AirborneAnimationHandler : MonoBehaviour
    {
        public float jumpHeight;
        public float runningJumpForce;
        public float airborneMoveSpeed;

        Animator animator;
        Rigidbody rb;
        RootMotionManager rootMotionManager;
        WeaponManager weaponManager;

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            rb = GetComponent<Rigidbody>();
            rootMotionManager = GetComponentInChildren<RootMotionManager>();
            weaponManager = GetComponent<WeaponManager>();
        }

        bool prevGrounded;
        private void Update()
        {
            bool isGrounded = IsGrounded();

            animator.SetFloat("yVelocity", rb.velocity.y);
            animator.SetBool("falling", !isGrounded);

            if (IsAirborne() | IsJumping())
            {
                rootMotionManager.drag = 0;
            }
            else
            {
                rootMotionManager.drag = 1;
            }

            // If we were falling on the last frame and we are not on this one
            if (!prevGrounded & isGrounded)
            {
                if (new Vector2(rb.velocity.x, rb.velocity.z).magnitude > breakfallRollThreshold)
                    animator.SetBool("breakfallRoll", true);
                    animator.SetFloat("landingAngle", Vector2.SignedAngle(new Vector2(rb.velocity.x, rb.velocity.z), new Vector2(transform.forward.x, transform.forward.z)));
            }

            prevGrounded = isGrounded;
        }

        private void FixedUpdate()
        {
            if (IsAirborne())
            {
                Vector3 moveForce = rb.rotation * new Vector3(moveInput.x, 0, moveInput.y) * airborneMoveSpeed;
                // If rigidbody's velocity magnitude is greater than moveForce's magnitude
                if (new Vector2(rb.velocity.x, rb.velocity.z).magnitude > new Vector2(moveForce.x, moveForce.z).magnitude) { return; }

                moveForce.x -= rb.velocity.x;
                moveForce.z -= rb.velocity.z;

                rb.AddForce(moveForce, ForceMode.VelocityChange);
            }
        }

        void OnJump()
        {
            if (IsAirborne() | IsJumping() | IsLanding() | rb.velocity.y > 1 | animator.IsInTransition(animator.GetLayerIndex("Airborne"))) { return; }
            StartCoroutine(Jump());
        }

        public float jumpForceDelay;
        private IEnumerator Jump()
        {
            animator.SetBool("jumping", true);

            yield return new WaitForSeconds(jumpForceDelay);
            if (!animator.GetBool("running"))
            {
                float jumpForce = Mathf.Sqrt(jumpHeight * -2 * Physics.gravity.y);
                rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.VelocityChange);
            }
            else
            {
                // Jump in direction rigidbody is moving
                rb.AddForce(rb.velocity + transform.up * runningJumpForce, ForceMode.VelocityChange);
            }

            yield return null;
            animator.SetBool("jumping", false);
        }
        
        Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        public float breakfallRollThreshold;
        bool landingCollisionRunning;
        private void OnCollisionEnter(Collision collision)
        {
            if (landingCollisionRunning) { return; }

            if ((IsAirborne() | IsJumping()) & !IsLanding())
            {
                animator.SetFloat("landingMagnitude", collision.relativeVelocity.magnitude);

                landingCollisionRunning = true;
                StartCoroutine(ResetLandingBool());
            }
        }

        private IEnumerator ResetLandingBool()
        {
            yield return new WaitUntil(() => !(IsAirborne() | IsJumping()) & !IsLanding());
            landingCollisionRunning = false;
            animator.SetBool("breakfallRoll", false);
        }

        bool IsAirborne()
        {
            return animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Airborne")).IsTag("Airborne");
        }

        bool IsJumping()
        {
            if (weaponManager.equippedWeapon == null)
            {
                return animator.GetCurrentAnimatorStateInfo(0).IsTag("Jumping");
            }
            else
            {
                return animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex(weaponManager.equippedWeapon.weaponClass)).IsTag("Jumping");
            }
        }

        bool IsLanding()
        {
            return animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Airborne")).IsTag("Landing");
        }

        public float isGroundedDistance;
        bool IsGrounded()
        {
            RaycastHit hit;
            return rb.SweepTest(Vector3.down, out hit, isGroundedDistance);
        }
    }
}
