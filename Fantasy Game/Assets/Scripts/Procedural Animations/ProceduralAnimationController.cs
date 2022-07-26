using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightPat.ProceduralAnimations
{
    public class ProceduralAnimationController : MonoBehaviour
    {
        public float moveSpeed = 0.05f;
        public bool addForce;
        public bool move;
        public Vector3 moveVector;

        void Update()
        {
            if (move)
            {
                transform.Translate(transform.forward * moveSpeed);
            }
        }

        private void FixedUpdate()
        {
            if (addForce)
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                Vector3 moveForce = moveVector;
                moveForce.x -= rb.velocity.x;
                moveForce.z -= rb.velocity.z;
                if (moveForce.x < 0)
                {
                    moveForce.x = 0;
                }
                if (moveForce.z < 0)
                {
                    moveForce.z = 0;
                }
                rb.AddForce(moveForce, ForceMode.VelocityChange);
            }
        }

        private Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }
    }
}
