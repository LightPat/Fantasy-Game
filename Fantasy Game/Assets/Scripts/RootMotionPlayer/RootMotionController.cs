using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightPat.Core
{
    public class RootMotionController : MonoBehaviour
    {
        public float moveTransitionSpeed;
        public float sensitivity;
        public float bodyRotationSpeed;

        public float mouseUpXRotLimit;
        public float mouseDownXRotLimit;

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

        bool disableLookInput;
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
            // float y = Mathf.SmoothDamp(animator.GetFloat("y"), yTarget, ref velocity, Time.deltaTime * moveTransitionSpeed, moveTransitionSpeed);
            float y = Mathf.Lerp(animator.GetFloat("y"), yTarget, Time.deltaTime * moveTransitionSpeed);
            animator.SetFloat("y", y);
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
                animator.SetBool("crouching", crouching);
            }
        }

        void OnSlot1()
        {
            //GetComponent<Rigidbody>().AddForce(Vector3.forward * 10, ForceMode.VelocityChange);
            GetComponent<Rigidbody>().AddTorque(Vector3.up * 100, ForceMode.VelocityChange);
        }
    }
}
