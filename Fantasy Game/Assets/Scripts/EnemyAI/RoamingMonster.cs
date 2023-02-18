using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using LightPat.ProceduralAnimations.Spider;

namespace LightPat.EnemyAI
{
    public class RoamingMonster : Enemy
    {
        [Header("Chase Settings")]
        public float visionDistance = 20;
        public float chaseSpeed = 7;
        public float maxChaseDistance = 30;
        public float stopDistance = 7;
        public float chaseRotationSpeed = 5;
        [Header("Roam Settings")]
        public float roamRadius = 20;
        public float roamSpeed = 2;
        public float roamingRotationSpeed = 1;

        private Transform target;
        private Vector3 startingPosition;
        private Vector3 roamingPosition;
        private bool lookingAround = true;
        private Rigidbody rb;
        private Animator animator;
        private bool radiusBHit;

        private void Start()
        {
            startingPosition = transform.position;
            rb = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
            roamingPosition = transform.position + new Vector3(0.1f,0,0.1f);
        }

        private void Update()
        {
            if (!IsOwner) { return; }

            if (target == null)
            {
                // If we don't have a target check a raycast
                RaycastHit[] allHits = Physics.RaycastAll(transform.position, transform.forward, visionDistance);
                System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

                foreach (RaycastHit hit in allHits)
                {
                    if (hit.transform.gameObject == gameObject) { continue; }

                    if (hit.transform.TryGetComponent(out Attributes hitAttributes))
                    {
                        if (hitAttributes.team != GetComponent<Attributes>().team)
                            target = hit.transform;
                    }

                    break;
                }

                if (!rb.isKinematic) { return; }
                // Roaming Logic
                // If we are turning to look at our new roaming position
                roamingPosition.y = transform.position.y;
                Debug.Log(Time.time + " " + lookingAround);
                if (lookingAround)
                {
                    transform.rotation = Quaternion.RotateTowards(rb.rotation, Quaternion.LookRotation(roamingPosition - transform.position), roamingRotationSpeed);
                    if (Quaternion.Angle(transform.rotation, Quaternion.LookRotation(roamingPosition - transform.position)) < 1)
                    {
                        lookingAround = false;
                    }
                }
                else if (Vector3.Distance(transform.position, roamingPosition) > 1) // If we haven't reached our roaming position yet
                {
                    transform.rotation = Quaternion.RotateTowards(rb.rotation, Quaternion.LookRotation(roamingPosition - transform.position), roamingRotationSpeed);
                    transform.position = transform.position + (Time.deltaTime * roamSpeed * transform.forward);
                }
                else // Once we've reached our roaming position, get a new one
                {
                    lookingAround = true;
                    roamingPosition = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), 0, Random.Range(-roamRadius, roamRadius));

                    radiusBHit = Physics.Raycast(transform.position + Quaternion.LookRotation(roamingPosition - transform.position) * Vector3.forward, roamingPosition - transform.position);

                    if (radiusBHit)
                    {
                        StartCoroutine(RefreshRoamingPosition());
                    }
                }
            }
            else
            {
                if (Vector3.Distance(target.position, transform.position) < attackReach)
                {
                    Attack();
                }
            }
        }

        private void FixedUpdate()
        {
            if (!IsOwner) { return; }
            if (rb.isKinematic) { return; }
            if (animator.GetBool("landing")) { return; }

            // If we don't have a target yet, roam
            if (target == null)
            {
                // Roaming Logic
                // If we are turning to look at our new roaming position
                roamingPosition.y = transform.position.y;
                if (lookingAround)
                {
                    rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, Quaternion.LookRotation(roamingPosition - transform.position), roamingRotationSpeed));
                    if (Quaternion.Angle(transform.rotation, Quaternion.LookRotation(roamingPosition - transform.position)) < 1)
                    {
                        lookingAround = false;
                    }
                }
                else if (Vector3.Distance(transform.position, roamingPosition) > 1) // If we haven't reached our roaming position yet
                {
                    Vector3 moveForce = transform.forward * roamSpeed;
                    rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, Quaternion.LookRotation(roamingPosition - transform.position), roamingRotationSpeed));
                    moveForce.x -= rb.velocity.x;
                    moveForce.z -= rb.velocity.z;
                    moveForce.y = 0;
                    rb.AddForce(moveForce, ForceMode.VelocityChange);                        
                }
                else // Once we've reached our roaming position, get a new one
                {
                    lookingAround = true;
                    roamingPosition = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), 0, Random.Range(-roamRadius, roamRadius));

                    radiusBHit = Physics.Raycast(transform.position + Quaternion.LookRotation(roamingPosition - transform.position) * Vector3.forward, roamingPosition - transform.position);

                    if (radiusBHit)
                    {
                        StartCoroutine(RefreshRoamingPosition());
                    }
                }
            }
            else // Once we have a target
            {
                // If we are not right next to the target, move toward it
                if (Vector3.Distance(target.position, transform.position) > stopDistance)
                {
                    rb.MovePosition(rb.position + (Time.fixedDeltaTime * chaseSpeed * transform.forward));
                }
                else if (Vector3.Distance(target.position, transform.position) > maxChaseDistance) // If the target is super far away, stop following it
                {
                    target = null;
                    return;
                }

                if (target.position - transform.position != Vector3.zero)
                {
                    Quaternion chaseRotation = Quaternion.RotateTowards(rb.rotation, Quaternion.LookRotation(target.position - transform.position), chaseRotationSpeed);
                    chaseRotation = Quaternion.Euler(0, chaseRotation.eulerAngles.y, 0);
                    rb.MoveRotation(chaseRotation);
                }
            }
        }

        [Header("Attack Settings")]
        public float attackDamage = 20;
        public float attackReach = 7;
        public float attackCooldown = 1.5f;
        bool allowAttack = true;
        private void Attack()
        {
            if (!allowAttack) { return; }

            // If we don't have a target check a raycast
            RaycastHit[] allHits = Physics.RaycastAll(transform.position, transform.forward, attackReach);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform.gameObject == gameObject) { continue; }

                if (hit.transform.TryGetComponent(out Attributes hitAttributes))
                {
                    hitAttributes.InflictDamage(attackDamage, gameObject);
                    StartCoroutine(AttackCooldown());
                }
                break;
            }
        }

        private IEnumerator RefreshRoamingPosition()
        {
            roamingPosition = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), 0, Random.Range(-roamRadius, roamRadius));
            //while (Physics.Raycast(transform.position + Quaternion.LookRotation(roamingPosition - transform.position) * Vector3.forward, roamingPosition - transform.position))
            //{
            //    roamingPosition = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), 0, Random.Range(-roamRadius, roamRadius));
            //}

            yield return new WaitForEndOfFrame();
        }

        private IEnumerator AttackCooldown()
        {
            allowAttack = false;
            yield return new WaitForSeconds(attackCooldown);
            allowAttack = true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            if (!Application.isPlaying)
            {
                Gizmos.DrawWireSphere(transform.position, roamRadius);
            }
            else
            {
                Gizmos.DrawWireSphere(new Vector3(startingPosition.x, transform.position.y, startingPosition.z), roamRadius);
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(startingPosition, 0.1f);

            Gizmos.color = Color.black;
            Gizmos.DrawSphere(roamingPosition, 1f);

            if (lookingAround)
            {
                Gizmos.color = Color.green;
                Vector3 pos = transform.position;
                pos.y += 8;
                Gizmos.DrawCube(pos, Vector3.one / 2);
            }
            else
            {
                Gizmos.color = Color.red;
                Vector3 pos = transform.position;
                pos.y += 8;
                Gizmos.DrawCube(pos, Vector3.one / 2);
            }
        }

        void OnAttacked(OnAttackedData data)
        {
            lookingAround = true;
            roamingPosition = data.inflicterPosition;
        }
    }
}
