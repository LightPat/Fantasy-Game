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

        AnimationLayerWeightManager weightManager;
        Animator animator;
        RigBuilder rigBuilder;

        private void Start()
        {
            weightManager = GetComponentInChildren<AnimationLayerWeightManager>();
            animator = GetComponentInChildren<Animator>();
            rigBuilder = GetComponentInChildren<RigBuilder>();
        }

        [Header("Reach Procedural Anim Settings")]
        public float reach;
        public float reachSpeed;
        public Rig armsRig;
        public Rig weaponRig;
        public TwoBoneIKConstraint rightHandIK;
        public TwoBoneIKConstraint leftHandIK;
        public Transform rightHandTarget;
        public Transform leftHandTarget;
        public Transform weaponSlot;
        void OnInteract()
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, reach))
            {
                if (hit.transform.GetComponent<Weapon>())
                {
                    //if (equippedWeapon != null) { equippedWeapon.SetActive(false); }
                    StartCoroutine(EquipWeapon(hit));
                }
            }
        }

        private IEnumerator EquipWeapon(RaycastHit hit)
        {
            Transform weapon = hit.transform;

            GetComponent<PlayerController>().disableLookInput = true;

            // Remove the physics and collider components
            Destroy(weapon.GetComponent<Rigidbody>());
            foreach (Collider c in weapon.GetComponentsInChildren<Collider>())
            {
                c.enabled = false;
            }

            // Reach out hands to grab weapon handle
            armsRig.GetComponent<RigWeightTarget>().weightSpeed = reachSpeed;
            armsRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            rightHandTarget.GetComponent<FollowTarget>().target = weapon.Find("ref_right_hand_grip");
            leftHandTarget.GetComponent<FollowTarget>().target = weapon.Find("ref_left_hand_grip");

            // Transition into the weapon's animations
            weightManager.SetLayerWeight(weapon.GetComponent<Weapon>().weaponClass, 1);
            animator.SetBool("combat", true);

            // Wait until hands have reached the weapon handle
            yield return new WaitUntil(() => armsRig.weight >= 0.9);

            // Don't move IK targets
            rightHandTarget.GetComponent<FollowTarget>().target = null;
            leftHandTarget.GetComponent<FollowTarget>().target = null;

            // Parent weapon to the constraint object
            weapon.SetParent(weaponSlot, true);

            armsRig.GetComponent<RigWeightTarget>().weightTarget = 0;

            yield return new WaitUntil(() => armsRig.weight <= 0.1);

            // Set target back to the hand bone
            rightHandTarget.GetComponent<FollowTarget>().target = rightHandIK.data.tip;
            leftHandTarget.GetComponent<FollowTarget>().target = leftHandIK.data.tip;

            equippedWeapon = weapon.gameObject;

            GetComponent<PlayerController>().disableLookInput = false;
            OnSlot1();
        }

        [Header("Attack Settings")]
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

        public float drawSpeed = 1;
        bool stowDrawRunning;
        void OnSlot1() // TODO not finished yet
        {
            if (stowDrawRunning) { return; }
            if (equippedWeapon == null) { return; }
            if ((animator.GetCurrentAnimatorStateInfo(0).IsTag("Draw Weapon") | animator.GetCurrentAnimatorStateInfo(0).IsTag("Stow Weapon"))
                | animator.IsInTransition(0)) { return; }

            animator.SetFloat("drawSpeed", drawSpeed);

            if (!animator.GetBool("combat"))
            {
                StartCoroutine(DrawWeapon());
            }
            else // If we are in combat mode
            {
                StartCoroutine(StowWeapon());
            }
        }

        private IEnumerator DrawWeapon()
        {
            stowDrawRunning = true;
            animator.SetBool("stowWeapon", true);
            yield return null;
            animator.SetBool("stowWeapon", false);

            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Draw To Combat"));
            weaponConstraint.GetComponent<MultiParentConstraintWeightManager>().SetObjectWeightTarget(0, 1); // Change right hand weight to 1
            weaponConstraint.GetComponent<MultiParentConstraintWeightManager>().SetObjectWeightTarget(2, 0); // Change spine's weight to 0
            equippedWeapon.GetComponent<Weapon>().ChangeOffset("transition"); // Switch to one handed offset values

            // Wait for animation to finish, then change offset and weights
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).length <= animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            weaponConstraint.GetComponent<MultiParentConstraintWeightManager>().SetObjectWeightTarget(1, 1); // Change left hand weight to 1
            equippedWeapon.GetComponent<Weapon>().ChangeOffset("player");

            animator.SetBool("combat", true);
            stowDrawRunning = false;
        }

        public MultiParentConstraint weaponConstraint;
        private IEnumerator StowWeapon()
        {
            stowDrawRunning = true;
            animator.SetBool("stowWeapon", true);
            weaponConstraint.GetComponent<MultiParentConstraintWeightManager>().SetObjectWeightTarget(1, 0); // Change left hand weight to 0
            equippedWeapon.GetComponent<Weapon>().ChangeOffset("transition"); // Switch to one handed offset values
            yield return null;
            animator.SetBool("stowWeapon", false);

            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Stow Weapon"));
            // Switch to stowed mode
            weaponConstraint.GetComponent<MultiParentConstraintWeightManager>().SetObjectWeightTarget(0, 0); // Change right hand weight to 0
            weaponConstraint.GetComponent<MultiParentConstraintWeightManager>().SetObjectWeightTarget(2, 1); // Change spine's weight to 1
            equippedWeapon.GetComponent<Weapon>().ChangeOffset("stowed");

            // Wait for the stow animation to finish playing, then change the layer weight
            //yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).length <= animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            //weightManager.SetLayerWeight(equippedWeapon.GetComponent<Weapon>().weaponClass, 0);
            animator.SetBool("combat", false);
            stowDrawRunning = false;
        }
    }
}
