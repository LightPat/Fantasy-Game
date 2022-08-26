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
        
        // For IK animation
        public Rig armsRig;
        public Transform rightHandTarget;
        public Transform leftHandTarget;

        // For MultiParentConstraint
        public TwoBoneIKConstraint rightHandIK;
        public TwoBoneIKConstraint leftHandIK;
        public RigBuilder rigBuilder;
        public Rig weaponRig;

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

        private IEnumerator SetupWeaponConstraint(RaycastHit hit)
        {
            Transform weapon = hit.transform;

            armsRig.GetComponent<RigWeightTarget>().weightSpeed = reachSpeed;
            armsRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            rightHandTarget.GetComponent<FollowTarget>().target = weapon.Find("ref_right_hand_grip");
            leftHandTarget.GetComponent<FollowTarget>().target = weapon.Find("ref_left_hand_grip");

            yield return new WaitUntil(() => armsRig.weight >= 0.9);

            rightHandTarget.GetComponent<FollowTarget>().target = null;
            leftHandTarget.GetComponent<FollowTarget>().target = null;

            weightManager.SetLayerWeight(weapon.GetComponent<Weapon>().weaponClass, 1);
            weapon.SetParent(weaponRig.transform);

            // Set Multi Parent source objects
            WeightedTransformArray sourceObjects = new WeightedTransformArray(0);
            WeightedTransform rightHand = new WeightedTransform(rightHandIK.data.tip, 1);
            WeightedTransform leftHand = new WeightedTransform(leftHandIK.data.tip, 1);
            sourceObjects.Add(rightHand);
            sourceObjects.Add(leftHand);
            hit.transform.GetComponent<MultiParentConstraint>().data.sourceObjects = sourceObjects;
            rigBuilder.Build();

            // Remove the physics and collider components
            Destroy(weapon.GetComponent<Rigidbody>());
            foreach (Collider c in weapon.GetComponentsInChildren<Collider>())
            {
                c.enabled = false;
            }

            // Set target back to the hand bone
            rightHandTarget.GetComponent<FollowTarget>().target = rightHandIK.data.tip;
            leftHandTarget.GetComponent<FollowTarget>().target = leftHandIK.data.tip;
            armsRig.GetComponent<RigWeightTarget>().weightTarget = 0;
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
