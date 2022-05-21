using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightPat.Core
{
    public class PlayerController : MonoBehaviour
    {
        private Rigidbody rb;
        private float currentSpeed;
        private Transform verticalRotate;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            currentSpeed = walkingSpeed;
            verticalRotate = transform.Find("Vertical Rotate");
        }


        private void Update()
        {
            lookInput *= (sensitivity);
            lookEulers.x += lookInput.x;

            // This prevents the rotation from increasing or decreasing infinitely if the player does a bunch of spins horizontally
            if (lookEulers.x >= 360)
            {
                lookEulers.x = lookEulers.x - 360;
            }
            else if (lookEulers.x <= -360)
            {
                lookEulers.x = lookEulers.x + 360;
            }

            /* First Person Camera Rotation Logic
            Remember that the camera is a child of the player, so we don't need to worry about horizontal rotation, that has already been calculated
            Calculate vertical rotation for the first person camera if you're not looking straight up or down already
            If we reach the top or bottom of our vertical look bound, set the total rotation amount to 1 over or 1 under the bound
            Otherwise, just change the rotation by the lookInput */
            if (lookEulers.y < -verticalLookBound)
            {
                lookEulers.y = -verticalLookBound - 1;

                if (lookInput.y > 0)
                {
                    lookEulers.y += lookInput.y;
                }
            }
            else if (lookEulers.y > verticalLookBound)
            {
                lookEulers.y = verticalLookBound + 1;

                if (lookInput.y < 0)
                {
                    lookEulers.y += lookInput.y;
                }
            }
            else
            {
                lookEulers.y += lookInput.y;
            }

            Quaternion newRotation = Quaternion.Euler(0, lookEulers.x, 0);
            rb.MoveRotation(newRotation);
            verticalRotate.rotation = Quaternion.Euler(-lookEulers.y, lookEulers.x, 0);
        }

        void FixedUpdate()
        {
            Vector3 moveForce = rb.rotation * new Vector3(moveInput.x, 0, moveInput.y) * currentSpeed;
            moveForce.x -= rb.velocity.x;
            moveForce.z -= rb.velocity.z;
            rb.AddForce(moveForce, ForceMode.VelocityChange);

            //// Falling Gravity velocity increase
            //if (rb.velocity.y < 0)
            //{
            //    rb.AddForce(new Vector3(0, (fallingGravityScale * -1), 0), ForceMode.VelocityChange);
            //}
        }



        [Header("Move Settings")]
        public float walkingSpeed = 5f;
        private Vector2 moveInput;
        void OnMove(InputValue value)
        {
            if (value.Get<Vector2>() != Vector2.zero)
            {
            }
            else
            {
            }

            moveInput = value.Get<Vector2>();
        }

        [Header("Look Settings")]
        public float sensitivity = 1f;
        public float verticalLookBound = 90f;
        private Vector3 lookEulers;
        private Vector2 lookInput;
        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>();
        }

        [Header("Interact Settings")]
        public float reach = 4f;
        void OnInteract()
        {
            RaycastHit hit;
            // Raycast gameObject that we are looking at if it is in the range of our reach
            bool bHit = Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, reach);

            if (bHit)
            {
                if (hit.collider.GetComponent<Interactable>())
                {
                    hit.collider.GetComponent<Interactable>().Invoke();
                }
            }
        }
    }
}
