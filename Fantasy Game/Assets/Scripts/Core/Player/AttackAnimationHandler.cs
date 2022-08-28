using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using LightPat.Util;

namespace LightPat.Core.Player
{
    public class AttackAnimationHandler : MonoBehaviour
    {
        AnimatorLayerWeightManager weightManager;
        Animator animator;
        WeaponManager weaponManager;

        private void Start()
        {
            weightManager = GetComponentInChildren<AnimatorLayerWeightManager>();
            animator = GetComponentInChildren<Animator>();
            weaponManager = GetComponent<WeaponManager>();
        }

        [Header("Reach Procedural Anim Settings")]
        public float reach;
        public float reachSpeed;
        public Rig rightArmRig;
        public Rig leftArmRig;
        public TwoBoneIKConstraint rightHandIK;
        public TwoBoneIKConstraint leftHandIK;
        public Transform rightHandTarget;
        public Transform leftHandTarget;
        public Transform weaponParent;
        public Transform backStowSlot;
        void OnInteract()
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, reach))
            {
                if (hit.transform.GetComponent<Weapon>())
                {
                    //if (weaponManager.equippedWeapon != null) { weaponManager.equippedWeapon.SetActive(false); }
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
            rightArmRig.GetComponent<RigWeightTarget>().weightSpeed = reachSpeed;
            rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            rightHandTarget.GetComponent<FollowTarget>().target = weapon.Find("ref_right_hand_grip");

            // Transition into the weapon's animations
            weightManager.SetLayerWeight(weapon.GetComponent<Weapon>().weaponClass, 1);

            // Wait until hands have reached the weapon handle
            yield return new WaitUntil(() => rightArmRig.weight >= 0.9);
            animator.SetBool("pickUpWeapon", true);

            // Activate secondary hand
            leftArmRig.GetComponent<RigWeightTarget>().weightSpeed = reachSpeed;
            leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            leftHandTarget.GetComponent<FollowTarget>().target = weapon.Find("ref_left_hand_grip");

            // Don't move IK target while reparenting
            rightHandTarget.GetComponent<FollowTarget>().target = null;

            // Parent weapon to the constraint object
            weapon.SetParent(weaponParent, true);

            rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 0;

            yield return new WaitUntil(() => rightArmRig.weight <= 0.1);

            // Set target back to the hand bone since this is the hand that controls the weapon
            rightHandTarget.GetComponent<FollowTarget>().target = rightHandIK.data.tip;

            weaponManager.AddWeapon(weapon.GetComponent<Weapon>());
            weaponManager.DrawWeapon(0);

            GetComponent<PlayerController>().disableLookInput = false;
            animator.SetBool("pickUpWeapon", false);
            //OnSlot1();
        }

        [Header("Attack1 Settings")]
        public float attackReach;
        public float attackDamage;
        void OnAttack1(InputValue value) // TODO change this to attack1
        {
            animator.SetBool("attack1", value.isPressed);
            GetComponent<PlayerController>().rotateBodyWithCamera = value.isPressed;
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

        void OnAttack2(InputValue value)
        {
            if (weaponManager.equippedWeapon == null) // If we have no weapon active in our hands, activate fist combat
            {
                if (!value.isPressed) { return; }
                if (!animator.GetBool("fistCombat"))
                {
                    animator.SetBool("fistCombat", true);
                }
                else
                {
                    animator.SetBool("fistCombat", false);
                }
            }
            else // If we have an equipped weapon do the secondary attack
            {
                GetComponent<PlayerController>().rotateBodyWithCamera = value.isPressed;
                animator.SetBool("attack2", value.isPressed);
            }
        }

        [Header("Slot 1")]
        public float drawSpeed = 1;
        bool stowDrawRunning;
        void OnSlot1() // TODO not finished yet
        {
            if (stowDrawRunning) { return; }
            if (weaponManager.GetWeapon(0) == null) { return; }
            if ((animator.GetCurrentAnimatorStateInfo(0).IsTag("Draw Weapon") | animator.GetCurrentAnimatorStateInfo(0).IsTag("Stow Weapon"))
                | animator.IsInTransition(0)) { return; }

            animator.SetFloat("drawSpeed", drawSpeed);

            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Weapon Combat Idle"))
            {
                StartCoroutine(StowWeapon());
            }
            else
            {
                StartCoroutine(DrawWeapon());
            }
        }

        private IEnumerator DrawWeapon()
        {
            stowDrawRunning = true;
            animator.SetBool("stowWeapon", true);
            weightManager.SetLayerWeight(weaponManager.GetWeapon(0).weaponClass, 1);
            yield return null;
            animator.SetBool("stowWeapon", false);

            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Draw To Combat"));
            // Switch to player mode
            weaponManager.GetWeapon(0).transform.SetParent(weaponParent, true);
            weaponManager.GetWeapon(0).ChangeOffset("player");

            // Wait for animation to finish, then change offset and weights
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).length <= animator.GetCurrentAnimatorStateInfo(0).normalizedTime);

            weaponManager.DrawWeapon(0);

            leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            leftHandTarget.GetComponent<FollowTarget>().target = weaponManager.equippedWeapon.transform.Find("ref_left_hand_grip");

            stowDrawRunning = false;
        }

        private IEnumerator StowWeapon()
        {
            stowDrawRunning = true;
            animator.SetBool("stowWeapon", true);

            leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 0;
            leftArmRig.GetComponent<RigWeightTarget>().weightSpeed = 30;
            leftHandTarget.GetComponent<FollowTarget>().target = leftHandIK.data.tip;
            yield return null;
            animator.SetBool("stowWeapon", false);

            // Wait for stow weapon animation to finish
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Stow Weapon"));
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).length <= animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            // Switch to stowed mode
            weaponManager.equippedWeapon.transform.SetParent(backStowSlot, true);
            weaponManager.equippedWeapon.GetComponent<Weapon>().ChangeOffset("stowed");

            // Wait for the stow animation to finish playing, then change the layer weight
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"));
            weightManager.SetLayerWeight(weaponManager.GetWeapon(0).weaponClass, 0);
            leftArmRig.GetComponent<RigWeightTarget>().weightSpeed = 5;
            weaponManager.StowWeapon();
            stowDrawRunning = false;
        }
    }
}
