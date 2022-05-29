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
        public float chaseSpeed = 3f;
        public float maxChaseDistance = 15f;
        public float stopDistance = 2f;
        [Header("Roam Settings")]
        public float roamRadius = 50f;
        public float roamSpeed = 2f;
        private Vector3 startingPosition;
        private Vector3 roamingPosition;
        private bool lookingAround = true;
        private Transform target;
        private Rigidbody rb;

        private RaycastHit visionHit;
        private RaycastHit radiusHit;
        private bool visionBHit;
        private bool radiusBHit;

        private void Start()
        {
            startingPosition = transform.position;
            rb = GetComponent<Rigidbody>();
            roamingPosition = transform.position + new Vector3(0.1f,0,0.1f);
        }

        private void Update()
        {
            if (target == null)
            {
                // If we don't have a target check a raycast
                visionBHit = Physics.Raycast(transform.position, transform.forward, out visionHit, visionDistance);

                if (visionBHit)
                {
                    if (visionHit.transform.GetComponent<PlayerController>())
                    {
                        target = visionHit.transform;
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
            // If we don't have a target yet, roam
            if (target == null)
            {
                // Roaming Logic
                // If we are turning to look at our new roaming position
                roamingPosition.y = transform.position.y;
                if (lookingAround)
                {
                    rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, Quaternion.LookRotation(roamingPosition - transform.position), 4));
                    if (Quaternion.Angle(transform.rotation, Quaternion.LookRotation(roamingPosition - transform.position)) < 1)
                    {
                        lookingAround = false;
                    }
                }
                else if (Vector3.Distance(transform.position, roamingPosition) > 1) // If we haven't reached our roaming position yet
                {
                    Vector3 moveForce = transform.forward * roamSpeed;
                    rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, Quaternion.LookRotation(roamingPosition - transform.position), 4));
                    moveForce.x -= rb.velocity.x;
                    moveForce.z -= rb.velocity.z;
                    moveForce.y = 0;
                    rb.AddForce(moveForce, ForceMode.VelocityChange);
                }
                else // Once we've reached our roaming position, get a new one
                {
                    lookingAround = true;
                    roamingPosition = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), 0, Random.Range(-roamRadius, roamRadius));

                    radiusBHit = Physics.Raycast(transform.position + Quaternion.LookRotation(roamingPosition - transform.position) * Vector3.forward, roamingPosition - transform.position, out radiusHit);

                    if (radiusBHit)
                    {
                        Debug.DrawRay(transform.position + Quaternion.LookRotation(roamingPosition - transform.position) * Vector3.forward, roamingPosition - transform.position, Color.blue, 20f);
                        StartCoroutine(RefreshRoamingPosition());
                    }
                    else
                    {
                        Debug.DrawRay(transform.position + Quaternion.LookRotation(roamingPosition - transform.position) * Vector3.forward, roamingPosition - transform.position, Color.green, 20f);
                    }
                }
            }
            else // Once we have a target
            {
                // If we are not right next to the target, move toward it
                if (Vector3.Distance(target.position, transform.position) > stopDistance)
                {
                    Vector3 moveForce = transform.forward * chaseSpeed;
                    moveForce.x -= rb.velocity.x;
                    moveForce.z -= rb.velocity.z;
                    // Never let the rigidbody jump when chasing a player
                    moveForce.y = 0;
                    rb.AddForce(moveForce, ForceMode.VelocityChange);
                }
            }
        }

        private IEnumerator RefreshRoamingPosition()
        {
            roamingPosition = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), 0, Random.Range(-roamRadius, roamRadius));
            while (Physics.Raycast(transform.position + Quaternion.LookRotation(roamingPosition - transform.position) * Vector3.forward, roamingPosition - transform.position, out radiusHit))
            {
                roamingPosition = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), 0, Random.Range(-roamRadius, roamRadius));
            }

            Debug.DrawRay(transform.position + transform.forward, roamingPosition - transform.position, Color.black, 20f);
            yield return new WaitForEndOfFrame();
        }
    }
}
