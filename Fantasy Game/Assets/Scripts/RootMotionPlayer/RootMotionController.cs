using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightPat.Core
{
    public class RootMotionController : MonoBehaviour
    {
        [Header("Animation Settings")]
        public float moveTransitionSpeed;
        public float moveLayerTransitionSpeed;
        public float crouchLayerTransitionSpeed;

        Animator animator;

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
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
        void OnLook(InputValue value)
        {
            if (disableLookInput) { return; }

            lookInput = value.Get<Vector2>();

            rotationX -= sensitivity * lookInput.y;
            rotationX = Mathf.Clamp(rotationX, mouseUpXRotLimit, mouseDownXRotLimit);
            Camera.main.transform.eulerAngles = new Vector3(rotationX, Camera.main.transform.eulerAngles.y + lookInput.x * sensitivity, 0);

            if (rotationX <= 90)
            {
                bodyRotation = new Vector3(transform.eulerAngles.x, Camera.main.transform.eulerAngles.y + lookInput.x * sensitivity, transform.eulerAngles.z);
            }
        }

        public void StartUpdateLookBound(float min, float max)
        {
            StartCoroutine(UpdateReferenceLookBound(min, max));
        }

        private IEnumerator UpdateReferenceLookBound(float min, float max)
        {
            yield return new WaitForSeconds(0.25f);
            mouseUpXRotLimit = min;
            mouseDownXRotLimit = max;
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
                if (!animator.GetCurrentAnimatorStateInfo(0).IsTag("PauseIdleTime"))
                    animator.SetFloat("idleTime", animator.GetFloat("idleTime") + Time.deltaTime);
                // Only change move layer weight if we are not in our idle loop
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Standing Idle"))
                    animator.SetLayerWeight(animator.GetLayerIndex("Moving"), Mathf.MoveTowards(animator.GetLayerWeight(animator.GetLayerIndex("Moving")), 0, Time.deltaTime * moveLayerTransitionSpeed));
            }
            else
            {
                animator.SetFloat("idleTime", 0);
                // Only change move layer weight if we are not in our idle loop
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Standing Idle"))
                    animator.SetLayerWeight(animator.GetLayerIndex("Moving"), Mathf.MoveTowards(animator.GetLayerWeight(animator.GetLayerIndex("Moving")), 1, Time.deltaTime * moveLayerTransitionSpeed));
            }

            if (crouching)
            {
                // Only change crouch layer weight if we are not in our idle loop
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Standing Idle"))
                    animator.SetLayerWeight(animator.GetLayerIndex("Crouching"), Mathf.MoveTowards(animator.GetLayerWeight(animator.GetLayerIndex("Crouching")), 1, Time.deltaTime * crouchLayerTransitionSpeed));
            }
            else
            {
                // Only change crouch layer weight if we are not in our idle loop
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Standing Idle"))
                    animator.SetLayerWeight(animator.GetLayerIndex("Crouching"), Mathf.MoveTowards(animator.GetLayerWeight(animator.GetLayerIndex("Crouching")), 0, Time.deltaTime * crouchLayerTransitionSpeed));
            }
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
