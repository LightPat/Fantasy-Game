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

        Rigidbody rb;
        Animator animator;
        HumanoidWeaponAnimationHandler humanoidWeaponAnimationHandler;
        WeaponLoadout weaponLoadout;
        Weapon targetWeapon;

        public void MoveToPoint(Vector3 worldPosition, float stopDistance)
        {
            Vector2 startPos = new Vector2(transform.position.x, transform.position.z);
            Vector2 endPos = new Vector2(worldPosition.x, worldPosition.z);

            if (Vector2.Distance(startPos, endPos) < stopDistance)
            {
                animator.SetFloat("moveInputX", 0);
                animator.SetFloat("moveInputY", 0);
            }
            else
            {
                Vector2 dir = (startPos - endPos).normalized;
                animator.SetFloat("moveInputX", Mathf.Lerp(animator.GetFloat("moveInputX"), dir.x, Time.deltaTime * moveTransitionSpeed));
                animator.SetFloat("moveInputY", Mathf.Lerp(animator.GetFloat("moveInputY"), dir.y, Time.deltaTime * moveTransitionSpeed));
            }
        }

        public void LookAtPoint(Vector3 worldPosition)
        {
            aimTarget.position = worldPosition;
            neckAimRig.weightTarget = 1;
        }

        public void TurnBodyToPoint(Vector3 worldPosition)
        {
            rb.MoveRotation(Quaternion.LookRotation(new Vector3(worldPosition.x, transform.position.y, worldPosition.z) - transform.position, Vector3.up));
        }

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
            humanoidWeaponAnimationHandler = GetComponent<HumanoidWeaponAnimationHandler>();
            weaponLoadout = GetComponent<WeaponLoadout>();
        }

        private void Update()
        {
            if (!weaponLoadout.equippedWeapon)
            {
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
                                neckAimRig.weightTarget = 0;
                                targetWeapon = null;
                            }
                        }
                    }
                }

                if (targetWeapon)
                {
                    Vector3 point = targetWeapon.GetComponentInChildren<Renderer>().bounds.center;
                    LookAtPoint(point);
                    TurnBodyToPoint(point);
                    MoveToPoint(point, weaponStopDistance);
                }
            }
        }
    }
}