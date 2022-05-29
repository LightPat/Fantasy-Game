using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

namespace LightPat.FriendlyAI
{
    public class Guard : Friendly
    {
        public Transform target;
        public float walkSpeed = 2f;
        public float chaseSpeed = 4f;
        private Rigidbody rb;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            
        }

        private void FixedUpdate()
        {
            if (target == null)
            {
                Vector3 moveForce = transform.forward * walkSpeed;
                moveForce.x -= rb.velocity.x;
                moveForce.z -= rb.velocity.z;
                rb.AddForce(moveForce, ForceMode.VelocityChange);
            }
        }
    }
}
