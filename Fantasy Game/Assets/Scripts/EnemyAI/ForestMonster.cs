using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

namespace LightPat.EnemyAI
{
    public class ForestMonster : Enemy
    {
        [Header("Chase Settings")]
        public float visionDistance = 10f;
        public float FOV = 30;
        public float chaseSpeed = 2f;
        public float maxChaseDistance = 15f;
        public float stopDistance = 2f;
        private Transform target;
        private Rigidbody rb;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (target == null)
            {
                // If we don't have a target check a raycast
                RaycastHit hit;
                bool bHit = Physics.Raycast(transform.position, transform.forward, out hit, visionDistance);
                Debug.DrawRay(transform.position, transform.forward * visionDistance, Color.blue, 2f);

                if (bHit)
                {
                    if (hit.transform.GetComponent<PlayerController>())
                    {
                        target = hit.transform;
                    }
                }
            }
            else
            {
                // If the target is super far away, stop following it
                if (Vector3.Distance(target.position, transform.position) > maxChaseDistance)
                {
                    target = null;
                    return;
                }

                transform.LookAt(target.position);
                Attack();
            }
        }

        private void FixedUpdate()
        {
            if (target == null)
            {
                // Add wandering code here
            }
            else
            {
                // If we are right next to the target, stop moving toward it
                if (Vector3.Distance(target.position, transform.position) > stopDistance)
                {
                    Vector3 moveForce = transform.forward * chaseSpeed;
                    moveForce.x -= rb.velocity.x;
                    moveForce.z -= rb.velocity.z;
                    moveForce.y = 0;
                    rb.AddForce(moveForce, ForceMode.VelocityChange);
                }
            }
        }
    }
}