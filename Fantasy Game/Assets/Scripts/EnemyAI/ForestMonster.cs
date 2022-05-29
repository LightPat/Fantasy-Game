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
        public float chaseSpeed = 3f;
        public float maxChaseDistance = 15f;
        public float stopDistance = 2f;
        [Header("Roam Settings")]
        public float roamRadius = 50f;
        public float roamSpeed = 2f;
        private Vector3 startingPosition;
        private Vector3 roamingPosition;
        private Transform target;
        private Rigidbody rb;

        private void Start()
        {
            startingPosition = transform.position;
            rb = GetComponent<Rigidbody>();
            roamingPosition = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), 0, Random.Range(-roamRadius, roamRadius));
        }

        private void Update()
        {
            if (target == null)
            {
                // If we don't have a target check a raycast
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
                // If the target is super far away, stop following it
                if (Vector3.Distance(target.position, transform.position) > maxChaseDistance)
                {
                    target = null;
                    return;
                }

                rb.MoveRotation(Quaternion.LookRotation(target.position - transform.position));
                Attack();
            }
        }

        private void FixedUpdate()
        {
            if (target == null)
            {
                // Roaming Logic
                if (Vector3.Distance(transform.position, roamingPosition) > 3)
                {
                    roamingPosition.y = transform.position.y;
                    rb.MoveRotation(Quaternion.LookRotation(roamingPosition - transform.position));
                    Vector3 moveForce = transform.forward * roamSpeed;
                    moveForce.x -= rb.velocity.x;
                    moveForce.z -= rb.velocity.z;
                    moveForce.y = 0;
                    rb.AddForce(moveForce, ForceMode.VelocityChange);
                }
                else // Once we've reached our roaming position, get a new one
                {
                    roamingPosition = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), 0, Random.Range(-roamRadius, roamRadius));
                }
            }
            else
            {
                // If we are not right next to the target, move toward it
                if (Vector3.Distance(target.position, transform.position) > stopDistance)
                {
                    Vector3 moveForce = transform.forward * chaseSpeed;
                    moveForce.x -= rb.velocity.x;
                    moveForce.z -= rb.velocity.z;
                    // Never let the rigidbody jump
                    moveForce.y = 0;
                    rb.AddForce(moveForce, ForceMode.VelocityChange);
                }
            }
        }
    }
}
