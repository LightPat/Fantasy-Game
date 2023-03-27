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
        public RigWeightTarget neckAimRig;
        public Transform aimTarget;
        public float moveTransitionSpeed = 4;
        [Header("Combat Settings")]
        public float combatSphereRadius;
        public float combatTargetStopDistance;
        [Header("MoveToTarget Settings")]
        public float sphereCastRadius;
        public float weaponStopDistance;
        [Header("Roam Settings")]
        public float roamRadius;
        public float roamingRotationSpeed;

        Vector3 startingPosition;
        Vector3 roamingPosition;
        bool lookingAround = true;
        Attributes attributes;
        Rigidbody rb;
        Animator animator;
        HumanoidWeaponAnimationHandler humanoidWeaponAnimationHandler;
        WeaponLoadout weaponLoadout;
        Weapon targetWeapon;
        [SerializeField] private fightingState fightState;

        public void MoveToPoint(Vector3 worldPosition, float stopDistance = 1, bool sprint = false)
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
                if (sprint)
                    move *= 2;
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
            if (rb)
                rb.MoveRotation(Quaternion.LookRotation(new Vector3(worldPosition.x, transform.position.y, worldPosition.z) - transform.position, Vector3.up));
            else
                transform.rotation = Quaternion.LookRotation(new Vector3(worldPosition.x, transform.position.y, worldPosition.z) - transform.position, Vector3.up);
        }

        public float RotateBodyTowardsPoint(Vector3 worldPosition, float rotationSpeed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(worldPosition.x, transform.position.y, worldPosition.z) - transform.position, Vector3.up);
            if (rb)
                rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime));
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            return Quaternion.Angle(transform.rotation, targetRotation);
        }

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
            humanoidWeaponAnimationHandler = GetComponent<HumanoidWeaponAnimationHandler>();
            weaponLoadout = GetComponent<WeaponLoadout>();
            attributes = GetComponent<Attributes>();
            startingPosition = transform.position;
            roamingPosition = transform.position + transform.forward;

            if (weaponLoadout.equippedWeapon)
                fightState = fightingState.combat;
            else
                fightState = fightingState.roaming;
        }

        private void Update()
        {
            if (attributes.HP.Value <= 0) { return; }

            if (fightState == fightingState.stationary)
            {
                animator.SetFloat("moveInputX", Mathf.Lerp(animator.GetFloat("moveInputX"), 0, Time.deltaTime * moveTransitionSpeed));
                animator.SetFloat("moveInputY", Mathf.Lerp(animator.GetFloat("moveInputY"), 0, Time.deltaTime * moveTransitionSpeed));
            }

            if (fightState == fightingState.combat)
            {
                bool attributesFound = false;
                RaycastHit[] allHits = Physics.SphereCastAll(transform.position, combatSphereRadius, transform.forward, 1);
                System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
                foreach (RaycastHit hit in allHits)
                {
                    if (hit.transform == transform) { continue; }
                    if (hit.transform.GetComponent<Attributes>())
                    {
                        // Ignore dead targets
                        if (hit.transform.GetComponent<Attributes>().HP.Value <= 0) { continue; }
                        
                        // Aim for the head
                        Vector3 targetPoint = hit.transform.GetComponent<Attributes>().headCollider.transform.position;
                        RotateBodyToPoint(targetPoint);
                        LookAtPoint(targetPoint);
                        MoveToPoint(targetPoint, combatTargetStopDistance, true);
                        humanoidWeaponAnimationHandler.Attack1(true);
                        attributesFound = true;
                        break;
                    }
                }

                if (!attributesFound)
                {
                    humanoidWeaponAnimationHandler.Attack1(false);
                    OnRoaming();
                }
            }

            if (fightState == fightingState.roaming)
                OnRoaming();

            if (fightState == fightingState.moveToTarget)
            {
                if (targetWeapon)
                {
                    Vector3 centerPoint = targetWeapon.GetComponentInChildren<Renderer>().bounds.center;
                    LookAtPoint(centerPoint);
                    RotateBodyTowardsPoint(centerPoint, roamingRotationSpeed);
                    MoveToPoint(centerPoint, weaponStopDistance, true);

                    if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(centerPoint.x, centerPoint.z)) < weaponStopDistance)
                    {
                        humanoidWeaponAnimationHandler.EquipWeapon(targetWeapon.GetComponent<NetworkedWeapon>());
                        targetWeapon = null;
                        fightState = fightingState.combat;
                    }
                }
            }

            if (!weaponLoadout.equippedWeapon)
            {
                RaycastHit[] allHits = Physics.SphereCastAll(transform.position, sphereCastRadius, transform.forward, 1);
                System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
                foreach (RaycastHit hit in allHits)
                {
                    if (hit.transform == transform) { continue; }
                    if (hit.transform.GetComponent<Weapon>())
                    {
                        if (!targetWeapon)
                        {
                            targetWeapon = hit.transform.GetComponent<Weapon>();
                            fightState = fightingState.moveToTarget;
                            break;
                        }
                    }
                }

                //Utilities.DrawBoxCastBox(new Vector3(transform.position.x, transform.position.y + 1, transform.position.z) - transform.forward, new Vector3(3, 2, 3), transform.rotation, transform.forward, 5, Color.red);
                //RaycastHit[] allHits = Physics.BoxCastAll(new Vector3(transform.position.x, transform.position.y + 1, transform.position.z) - transform.forward, new Vector3(3, 2, 3), transform.forward, transform.rotation, 5);
                //System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
                //foreach (RaycastHit hit in allHits)
                //{
                //    if (hit.transform == transform) { continue; }
                //    if (hit.transform.GetComponent<Weapon>())
                //    {
                //        if (!targetWeapon)
                //        {
                //            targetWeapon = hit.transform.GetComponent<Weapon>();
                //            fightState = fightingState.moveToTarget;
                //            break;
                //        }
                //    }
                //}
            }
        }

        void OnRoaming()
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
            else if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(roamingPosition.x, roamingPosition.z)) > 2) // If we haven't reached our roaming position yet
            {
                MoveToPoint(roamingPosition, 0);
                RotateBodyTowardsPoint(roamingPosition, roamingRotationSpeed);
            }
            else // Once we've reached our roaming position, get a new one
            {
                //lookingAround = true;
                roamingPosition = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), 0, Random.Range(-roamRadius, roamRadius));

                if (Physics.Raycast(transform.position + Quaternion.LookRotation(roamingPosition - transform.position) * Vector3.forward, roamingPosition - transform.position))
                {
                    roamingPosition = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), 0, Random.Range(-roamRadius, roamRadius));
                    //while (Physics.Raycast(transform.position + Quaternion.LookRotation(roamingPosition - transform.position) * Vector3.forward, roamingPosition - transform.position) | Vector3.Distance(transform.position, roamingPosition) < roamRadius)
                    //{
                    //    roamingPosition = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), 0, Random.Range(-roamRadius, roamRadius));
                    //}
                    RotateBodyTowardsPoint(roamingPosition, roamingRotationSpeed);
                }
            }
        }

        void OnProjectileHit()
        {

        }

        private void OnDrawGizmosSelected()
        {
            // Weapon hits
            Gizmos.color = Color.green;
            RaycastHit[] allHits = Physics.SphereCastAll(transform.position, sphereCastRadius, transform.forward, 1);
            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform.GetComponent<Weapon>())
                {
                    Gizmos.DrawWireSphere(hit.transform.position, 1);
                }
            }

            // Attribute hits
            Gizmos.color = Color.red;
            allHits = Physics.SphereCastAll(transform.position, combatSphereRadius, transform.forward, 1);
            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform == transform) { continue; }
                if (hit.transform.GetComponent<Attributes>())
                {
                    Gizmos.DrawWireSphere(hit.transform.position, 1);
                }
            }

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