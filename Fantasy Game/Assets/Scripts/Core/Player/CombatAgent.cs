using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using LightPat.ProceduralAnimations;

namespace LightPat.Core.Player
{
    public class CombatAgent : MonoBehaviour
    {
        public GameObject equippedWeapon = null;
        public bool combat;

        AnimationLayerWeightManager weightManager;

        private void Start()
        {
            weightManager = GetComponentInChildren<AnimationLayerWeightManager>();
        }

        [Header("Reach Procedural Anim Settings")]
        public float reach;
        public float reachSpeed;
        public RigBuilder rigBuilder;
        public Rig armsRig;
        public Rig weaponRig;
        public TwoBoneIKConstraint rightHandIK;
        public TwoBoneIKConstraint leftHandIK;
        public Transform rightHandTarget;
        public Transform leftHandTarget;
        void OnInteract()
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, reach))
            {
                if (hit.transform.GetComponent<Weapon>())
                {
                    //if (equippedWeapon != null) { equippedWeapon.SetActive(false); }

                    StartCoroutine(SetupWeaponConstraint(hit));
                }
            }
        }


        [Header("ASSIGN")]
        public Transform weaponSlot;
        public Transform right;
        public Transform left;
        private IEnumerator SetupWeaponConstraint(RaycastHit hit)
        {
            Transform weapon = hit.transform;

            // Remove the physics and collider components
            Destroy(weapon.GetComponent<Rigidbody>());
            foreach (Collider c in weapon.GetComponentsInChildren<Collider>())
            {
                c.enabled = false;
            }

            // Reach out hands to grab weapon handle
            //weightManager.SetLayerWeight(weapon.GetComponent<Weapon>().weaponClass, 1);
            armsRig.GetComponent<RigWeightTarget>().weightSpeed = reachSpeed;
            armsRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            rightHandTarget.GetComponent<FollowTarget>().target = weapon.Find("ref_right_hand_grip");
            leftHandTarget.GetComponent<FollowTarget>().target = weapon.Find("ref_left_hand_grip");

            // Wait until hands have reached the weapon handle
            yield return new WaitUntil(() => armsRig.weight >= 0.9);

            // Don't move IK targets
            rightHandTarget.GetComponent<FollowTarget>().target = null;
            leftHandTarget.GetComponent<FollowTarget>().target = null;

            // Set parent to the weapon rig
            weapon.SetParent(weaponSlot);
            weapon.localPosition = Vector3.zero;
            weapon.localEulerAngles = Vector3.zero;

            // Set Multi Parent Constraint source objects
            //WeightedTransformArray sourceObjects = new WeightedTransformArray(0);
            //WeightedTransform rightHand = new WeightedTransform(rightHandIK.data.tip, 1);
            //WeightedTransform leftHand = new WeightedTransform(leftHandIK.data.tip, 1);
            //sourceObjects.Add(rightHand);
            //sourceObjects.Add(leftHand);
            //weapon.GetComponent<MultiParentConstraint>().data.sourceObjects = sourceObjects;
            //rigBuilder.Build();

            // Assign rig weights so that the arms interpolate into the weapon's animations
            weaponRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            armsRig.GetComponent<RigWeightTarget>().weightTarget = 0;

            // TODO
            //Transform m = weapon.Find("Model");
            //m.localPosition += weapon.GetComponent<Weapon>().heldPositionOffset;
            //m.localEulerAngles += weapon.GetComponent<Weapon>().heldRotationOffset;

            yield return new WaitUntil(() => armsRig.weight <= 0.1);

            // Set target back to the hand bone
            rightHandTarget.GetComponent<FollowTarget>().target = rightHandIK.data.tip;
            leftHandTarget.GetComponent<FollowTarget>().target = leftHandIK.data.tip;
        }

        public float attackReach;
        public float attackDamage;
        void OnAttack2(InputValue value) // TODO change this to attack1
        {
            if (!value.isPressed) { return; }

            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, attackReach))
            {
                if (hit.transform.GetComponent<Attributes>())
                {
                    hit.transform.GetComponent<Attributes>().InflictDamage(attackDamage, gameObject);
                }
            }
        }

        void OnSlot1() // TODO not finished yet
        {
            combat = !combat;
            if (combat)
            {
                if (equippedWeapon == null)
                {
                    weightManager.SetLayerWeight("Fists", 1);
                }
                else
                {
                    // GetComponent<Weapon>().weaponClass;
                    weightManager.SetLayerWeight("", 1);
                }
            }
            else
            {
                if (equippedWeapon == null)
                {
                    weightManager.SetLayerWeight("Fists", 0);
                }
                else
                {
                    // GetComponent<Weapon>().weaponClass;
                    weightManager.SetLayerWeight("", 1);
                }
            }
        }
    }
}
