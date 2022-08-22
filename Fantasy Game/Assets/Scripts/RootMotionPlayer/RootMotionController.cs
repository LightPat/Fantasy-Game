using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightPat.Core
{
    public class RootMotionController : MonoBehaviour
    {
        [Header("Animation Parameter Settings")]
        public float moveTransitionSpeed;

        Animator animator;
        AnimationLayerWeightManager weightManager;

        // Used in PlayerCameraFollow Script
        public void ResetCameraXRotation()
        {
            rotationX = 0;
        }

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            weightManager = GetComponent<AnimationLayerWeightManager>();
        }

        void OnEscape()
        {
            disableLookInput = !disableLookInput;
        }

        [HideInInspector] public Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
            if (moveInput.y <= 0 & sprinting) { sprintTarget = 2; }
            if (moveInput == Vector2.zero & sprinting) { sprinting = false; }
        }

        [Header("Mouse Look Settings")]
        public float sensitivity;
        public float bodyRotationSpeed;
        public float mouseUpXRotLimit;
        public float mouseDownXRotLimit;
        public bool disableLookInput;

        Vector2 lookInput;
        Vector3 bodyRotation;
        float rotationX;
        float rotationY;
        void OnLook(InputValue value)
        {
            if (disableLookInput) { return; }

            lookInput = value.Get<Vector2>();

            rotationX -= sensitivity * lookInput.y;
            rotationY += sensitivity * lookInput.x;
            rotationX = Mathf.Clamp(rotationX, mouseUpXRotLimit, mouseDownXRotLimit);
            Camera.main.transform.eulerAngles = new Vector3(rotationX, rotationY, 0);

            if (rotationX <= 90)
            {
                bodyRotation = new Vector3(transform.eulerAngles.x, Camera.main.transform.eulerAngles.y + lookInput.x * sensitivity, transform.eulerAngles.z);
            }
        }

        private void Update()
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(bodyRotation), Time.deltaTime * bodyRotationSpeed);

            float xTarget = moveInput.x;
            if (sprinting) { xTarget *= sprintTarget; }
            float x = Mathf.Lerp(animator.GetFloat("x"), xTarget, Time.deltaTime * moveTransitionSpeed);
            animator.SetFloat("x", x);

            float yTarget = moveInput.y;
            if (sprinting) { yTarget *= sprintTarget; }
            float y = Mathf.Lerp(animator.GetFloat("y"), yTarget, Time.deltaTime * moveTransitionSpeed);
            animator.SetFloat("y", y);

            if (moveInput == Vector2.zero)
            {
                // Only change move layer weight and idle time if we are at rest
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Standing Idle"))
                {
                    weightManager.SetLayerWeight("Moving", 0);

                    // This is used so that some states that don't have exit transitions can "remember" that the user moved during their playtime, also so that crouching and jumping is not considered "idle"
                    if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Idle Loop")).IsTag("PauseIdleTime"))
                    {
                        animator.SetFloat("idleTime", animator.GetFloat("idleTime") + Time.deltaTime);
                    }
                }

                // Only change Idle Loop layer weight if idleTime is greater than 10 and we have no moveInput
                if (animator.GetFloat("idleTime") > 10)
                    weightManager.SetLayerWeight("Idle Loop", 1);
            }
            else // if moveInput is not Vector2.zero
            {
                animator.SetFloat("idleTime", 0);
                // Only change move layer weight if we are not in our idle loop
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Standing Idle"))
                    weightManager.SetLayerWeight("Moving", 1);
            }

            // Change the weight of the idle Loop once we have exited whatever idle animation we were in
            if (animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Idle Loop")).IsName("Not Idle Looping"))
                weightManager.SetLayerWeight("Idle Loop", 0);

            if (crouching)
            {
                animator.SetFloat("idleTime", 0);
                // Only change crouch layer weight if we are not in our idle loop
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Standing Idle"))
                    weightManager.SetLayerWeight("Crouching", 1);
            }
            else
            {
                // Only change crouch layer weight if we are not in our idle loop
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Standing Idle"))
                    weightManager.SetLayerWeight("Crouching", 0);
            }

            // If we jump set idleTime to 0
            if (animator.GetBool("jumping")) { animator.SetFloat("idleTime", 0); }
        }

        bool sprinting;
        float sprintTarget;
        void OnSprint(InputValue value)
        {
            if (value.isPressed & moveInput != Vector2.zero)
            {
                sprinting = !sprinting;
                sprintTarget = 2;
                ascending = true;
            }
        }

        bool ascending = true;
        void OnScaleSprint()
        {
            if (sprinting & !crouching & moveInput.y > 0)
            {
                if (ascending)
                {
                    sprintTarget += 1;
                    if (sprintTarget == 4)
                    {
                        ascending = false;
                    }
                }
                else
                {
                    sprintTarget -= 1;
                    if (sprintTarget == 2)
                    {
                        ascending = true;
                    }
                }
            }
        }

        bool crouching;
        void OnCrouch(InputValue value)
        {
            if (value.isPressed)
            {
                crouching = !crouching;
            }
        }
    }
}
