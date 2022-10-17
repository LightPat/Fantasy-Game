using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using LightPat.Core.Player;
using LightPat.ProceduralAnimations;

namespace LightPat.EnemyAI
{
    public class YBotEnemy : Enemy
    {
        public float boxCastDistance;
        public float weaponStopDistance;
        public Camera firstPersonCamera;
        public RigWeightTarget neckAimRig;
        public Transform aimTarget;
        public float moveTransitionSpeed = 4;
        [Header("Roam Settings")]
        public float roamRadius;
        public float roamingRotationSpeed;

        Vector3 startingPosition;
        Vector3 roamingPosition;
        bool lookingAround = true;
        public bool roaming;
        Rigidbody rb;
        Animator animator;
        HumanoidWeaponAnimationHandler humanoidWeaponAnimationHandler;
        WeaponLoadout weaponLoadout;
        Weapon targetWeapon;

        public void MoveToPoint(Vector3 worldPosition, float stopDistance = 1)
        {
            Vector2 startPos = new Vector2(transform.position.x, transform.position.z);
            Vector2 endPos = new Vector2(worldPosition.x, worldPosition.z);

            if (Vector2.Distance(startPos, endPos) < stopDistance)
            {
                animator.SetFloat("moveInputX", Mathf.Lerp(animator.GetFloat("moveInputX"), 0, Time.deltaTime * moveTransitionSpeed));
                animator.SetFloat("moveInputY", Mathf.Lerp(animator.GetFloat("moveInputY"), 0, Time.deltaTime * moveTransitionSpeed));
            }
            else
            {
                Vector2 dir = (endPos - startPos).normalized;
                Vector3 move = (Quaternion.Inverse(transform.rotation) * new Vector3(dir.x, 0, dir.y)).normalized;
                animator.SetFloat("moveInputX", Mathf.Lerp(animator.GetFloat("moveInputX"), move.x, Time.deltaTime * moveTransitionSpeed));
                animator.SetFloat("moveInputY", Mathf.Lerp(animator.GetFloat("moveInputY"), move.z, Time.deltaTime * moveTransitionSpeed));
            }
        }

        public void LookAtPoint(Vector3 worldPosition)
        {
            aimTarget.position = worldPosition;
            neckAimRig.weightTarget = 1;
        }

        public void RotateBodyToPoint(Vector3 worldPosition)
        {
            rb.MoveRotation(Quaternion.LookRotation(new Vector3(worldPosition.x, transform.position.y, worldPosition.z) - transform.position, Vector3.up));
        }

        public float RotateBodyTowardsPoint(Vector3 worldPosition, float rotationSpeed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(worldPosition.x, transform.position.y, worldPosition.z) - transform.position, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime));
            return Quaternion.Angle(transform.rotation, targetRotation);
        }

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
            humanoidWeaponAnimationHandler = GetComponent<HumanoidWeaponAnimationHandler>();
            weaponLoadout = GetComponent<WeaponLoadout>();
            startingPosition = transform.position;
            roamingPosition = transform.position + transform.forward;
        }

        private void Update()
        {
            if (roaming)
            {
                if (lookingAround)
                {
                    animator.SetFloat("moveInputX", Mathf.Lerp(animator.GetFloat("moveInputX"), 0, Time.deltaTime * moveTransitionSpeed));
                    animator.SetFloat("moveInputY", Mathf.Lerp(animator.GetFloat("moveInputY"), 0, Time.deltaTime * moveTransitionSpeed));
                    if (RotateBodyTowardsPoint(roamingPosition, roamingRotationSpeed) < 5)
                    {
                        lookingAround = false;
                    }
                }
                else if (Vector3.Distance(transform.position, roamingPosition) > 2) // If we haven't reached our roaming position yet
                {
                    MoveToPoint(roamingPosition, 0);
                    RotateBodyTowardsPoint(roamingPosition, roamingRotationSpeed);

                    if (Physics.Raycast(transform.position + Quaternion.LookRotation(roamingPosition - transform.position) * Vector3.forward, roamingPosition - transform.position))
                    {
                        roamingPosition = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), 0, Random.Range(-roamRadius, roamRadius));
                        while (Physics.Raycast(transform.position + Quaternion.LookRotation(roamingPosition - transform.position) * Vector3.forward, roamingPosition - transform.position))
                        {
                            roamingPosition = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), 0, Random.Range(-roamRadius, roamRadius));
                        }
                        RotateBodyTowardsPoint(roamingPosition, roamingRotationSpeed);
                    }
                }
                else // Once we've reached our roaming position, get a new one
                {
                    //lookingAround = true;
                    roamingPosition = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), 0, Random.Range(-roamRadius, roamRadius));

                    if (Physics.Raycast(transform.position + Quaternion.LookRotation(roamingPosition - transform.position) * Vector3.forward, roamingPosition - transform.position))
                    {
                        roamingPosition = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), 0, Random.Range(-roamRadius, roamRadius));
                        while (Physics.Raycast(transform.position + Quaternion.LookRotation(roamingPosition - transform.position) * Vector3.forward, roamingPosition - transform.position))
                        {
                            roamingPosition = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), 0, Random.Range(-roamRadius, roamRadius));
                        }
                        RotateBodyTowardsPoint(roamingPosition, roamingRotationSpeed);
                    }
                }
            }

            if (!weaponLoadout.equippedWeapon)
            {
                if (targetWeapon)
                {
                    Vector3 point = targetWeapon.GetComponentInChildren<Renderer>().bounds.center;
                    LookAtPoint(point);
                    RotateBodyToPoint(point);
                    MoveToPoint(point, weaponStopDistance);
                    roaming = false;
                }
                else
                {
                    roaming = true;
                }

                Utilities.DrawBoxCastBox(new Vector3(transform.position.x, transform.position.y + 1, transform.position.z) - transform.forward, new Vector3(3, 2, 3), transform.rotation, transform.forward, boxCastDistance, Color.red);
                RaycastHit[] allHits = Physics.BoxCastAll(new Vector3(transform.position.x, transform.position.y + 1, transform.position.z) - transform.forward, new Vector3(3, 2, 3), transform.forward, transform.rotation, boxCastDistance);
                System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
                foreach (RaycastHit hit in allHits)
                {
                    if (hit.transform == transform) { continue; }
                    if (hit.transform.GetComponent<Weapon>())
                    {
                        if (!targetWeapon)
                        {
                            targetWeapon = hit.transform.GetComponent<Weapon>();
                            break;
                        }
                        else
                        {
                            Vector3 centerPoint = targetWeapon.GetComponentInChildren<Renderer>().bounds.center;
                            if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(centerPoint.x, centerPoint.z)) < weaponStopDistance)
                            {
                                humanoidWeaponAnimationHandler.EquipWeapon(hit.transform.GetComponent<Weapon>());
                                targetWeapon = null;
                            }
                        }
                    }
                }
            }
            else
            {
                roaming = true;
            }
        }

        private void OnDrawGizmos()
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
    }
}