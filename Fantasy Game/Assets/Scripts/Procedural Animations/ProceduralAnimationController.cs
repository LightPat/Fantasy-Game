using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightPat.ProceduralAnimations
{
    public class ProceduralAnimationController : MonoBehaviour
    {
        public bool move;
        public float moveSpeed = 0.05f;
        public bool addForce;
        public Vector3 moveVector;
        public bool addTorque;
        public Vector3 torqueVector;

        void Update()
        {
            //GetComponent<Rigidbody>().Sleep();
            //Debug.Log(GetComponent<Rigidbody>().IsSleeping());
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
                rb.AddForce(moveForce, ForceMode.VelocityChange);
            }

            if (addTorque)
            {
                GetComponent<Rigidbody>().AddTorque(torqueVector, ForceMode.VelocityChange);
            }
        }

        private Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }
    }
}
