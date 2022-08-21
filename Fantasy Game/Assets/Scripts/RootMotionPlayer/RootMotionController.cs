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
        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>();
            //transform.Rotate(new Vector3(0, lookInput.x * 0.2f, 0));
            Vector3 baseEulers = Camera.main.transform.eulerAngles;
            Vector3 targetEulers = new Vector3(baseEulers.x - lookInput.y * 0.2f, baseEulers.y + lookInput.x * 0.2f, baseEulers.z);
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
        }

        private void Update()
        {
            //float turn = Mathf.Lerp(animator.GetFloat("turn"), lookInput.x, Time.deltaTime);
            //animator.SetFloat("turn", turn);

            float xTarget = moveInput.x;
            if (sprinting) { xTarget *= sprintTarget; }
            float x = Mathf.MoveTowards(animator.GetFloat("x"), xTarget, Time.deltaTime * moveTransitionSpeed);
            animator.SetFloat("x", x);

            float yTarget = moveInput.y;
            if (sprinting) { yTarget *= sprintTarget; }
            float y = Mathf.MoveTowards(animator.GetFloat("y"), yTarget, Time.deltaTime * moveTransitionSpeed);
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
            if (sprinting & !crouching & moveInput == new Vector2(0,1))
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
    }
}
