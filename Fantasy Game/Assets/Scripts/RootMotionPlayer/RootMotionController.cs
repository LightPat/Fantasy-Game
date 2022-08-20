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
            Debug.Log(lookInput);
            transform.Rotate(new Vector3(0, lookInput.x * 0.2f, 0));
            //Camera.main.transform.Rotate(new Vector3(-lookInput.y, lookInput.x, 0) * 0.2f);
            //Camera.main.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y, 0);
        }

        private void Update()
        {
            float turn = Mathf.Lerp(animator.GetFloat("turn"), lookInput.x, Time.deltaTime * 4);
            animator.SetFloat("turn", turn);

            float xTarget = moveInput.x;
            if (sprinting) { xTarget *= 2; }
            float x = Mathf.Lerp(animator.GetFloat("x"), xTarget, Time.deltaTime * 4);
            animator.SetFloat("x", x);

            float yTarget = moveInput.y;
            if (sprinting) { yTarget *= sprintTarget; }
            float y = Mathf.Lerp(animator.GetFloat("y"), yTarget, Time.deltaTime * 4);
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
