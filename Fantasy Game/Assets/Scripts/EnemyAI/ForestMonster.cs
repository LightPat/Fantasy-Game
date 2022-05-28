using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

namespace LightPat.EnemyAI
{
    public class ForestMonster : Enemy
    {
        public float visionDistance = 10f;
        public float chaseSpeed = 2f;
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
                RaycastHit hit;
                bool bHit = Physics.Raycast(transform.position, transform.forward, out hit, visionDistance);

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
                transform.LookAt(target.position);
            }
        }

        private void FixedUpdate()
        {
            if (target == null)
            {

            }
            else
            {
                Vector3 moveForce = transform.forward * chaseSpeed;
                moveForce.x -= rb.velocity.x;
                moveForce.z -= rb.velocity.z;
                rb.AddForce(moveForce, ForceMode.VelocityChange);
            }
        }

        public override void Attack()
        {
            Debug.Log("Attacking");
        }
    }
}
