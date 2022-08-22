using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightPat.Core
{
    public class AirborneAnimationHandler : MonoBehaviour
    {
        public float speed;

        Animator animator;
        Rigidbody rb;
        bool falling;

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
                Vector3 moveForce = rb.rotation * new Vector3(moveInput.x, 0, moveInput.y);
                moveForce.x -= rb.velocity.x;
                moveForce.z -= rb.velocity.z;
                rb.AddForce(moveForce * speed, ForceMode.VelocityChange);
            }
        }

        [Header("Jump Settings")]
        public float jumpHeight;
        bool jumping;
        void OnJump()
        {
            animator.SetBool("jumping", true);
            float jumpForce = Mathf.Sqrt(jumpHeight * -2 * Physics.gravity.y);
            rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.VelocityChange);
            StartCoroutine(t());
        }

        private IEnumerator t()
        {
            yield return null;
            animator.SetBool("jumping", false);
        }

        
        Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Falling Idle"))
            {
                animator.SetFloat("landingVelocity", collision.relativeVelocity.magnitude);
                animator.Play("Idle.Landing", 0);
            }
        }
    }
}