using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightPat.Core
{
    public class RootMotionController : MonoBehaviour
    {
        private Animator animator;

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
        }

        public Vector2 moveInput;
        void OnMove(InputValue value)
        {
            if (!sprinting) { moveInput = value.Get<Vector2>(); }
        }

        public Vector2 lookInput;
        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>();
            transform.Rotate(new Vector3(0, lookInput.x * 0.2f, 0));
            Vector3 baseEulers = Camera.main.transform.eulerAngles;
            Camera.main.transform.eulerAngles = new Vector3(baseEulers.x - lookInput.y * 0.2f, baseEulers.y + lookInput.x * 0.2f, baseEulers.z);
            //Camera.main.transform.Rotate(new Vector3(0, lookInput.x * 0.2f, 0));
        }

        private void Update()
        {
            Debug.Log(transform.forward);
            float turn = Mathf.Lerp(animator.GetFloat("turn"), lookInput.x, Time.deltaTime);
            animator.SetFloat("turn", turn);

            float xTarget = moveInput.x;
            if (sprinting) { xTarget *= 2; }
            float x = Mathf.Lerp(animator.GetFloat("x"), xTarget, Time.deltaTime);
            animator.SetFloat("x", x);

            float yTarget = moveInput.y;
            if (sprinting) { yTarget *= sprintTarget; }
            float y = Mathf.Lerp(animator.GetFloat("y"), yTarget, Time.deltaTime);
            animator.SetFloat("y", y);
        }

        public bool sprinting;
        public float sprintTarget;
        void OnSprint(InputValue value)
        {
            if (value.isPressed & moveInput != Vector2.zero)
            {
                sprinting = !sprinting;
                sprintTarget = 2;
            }
        }

        void OnTapW()
        {
            if (sprinting & sprintTarget != 4)
            {
                sprintTarget += 1;
            }
        }
    }
}
