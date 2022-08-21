using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightPat.Core
{
    public class RootMotionController : MonoBehaviour
    {
        public float moveTransitionSpeed;
        public float verticalLookBound;
        public float sensitivity;
        public float bodyRotationSpeed;

        Animator animator;

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
        }

        [HideInInspector] public Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
            if (moveInput.y <= 0 & sprinting) { sprintTarget = 2; }
            if (moveInput == Vector2.zero & sprinting) { sprinting = false; }
        }

        Vector2 lookInput;
        Vector3 bodyRotation;
        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>();
            Vector3 baseEulers = Camera.main.transform.eulerAngles;
            Vector3 targetEulers = new Vector3(baseEulers.x - lookInput.y * sensitivity, baseEulers.y + lookInput.x * sensitivity, baseEulers.z);
            float upperBound = 360 - verticalLookBound;
            if (targetEulers.x > verticalLookBound & targetEulers.x < upperBound & lookInput.y < 0)
            {
                targetEulers.x = verticalLookBound;
            }
            else if (targetEulers.x > verticalLookBound & targetEulers.x < upperBound & lookInput.y > 0)
            {
                targetEulers.x = upperBound;
            }
            Camera.main.transform.eulerAngles = targetEulers;
            bodyRotation = new Vector3(transform.eulerAngles.x, baseEulers.y + lookInput.x * sensitivity, transform.eulerAngles.z);
            //transform.eulerAngles = bodyRotation;
            //bodyOffset = transform.eulerAngles - bodyRotation;
            //transform.eulerAngles -= bodyOffset;
        }

        private void Update()
        {
            //float turn = Mathf.Lerp(animator.GetFloat("turn"), lookInput.x, Time.deltaTime);
            //animator.SetFloat("turn", turn);
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
