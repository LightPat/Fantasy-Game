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
        PlayerController playerController;
        WeaponManager weaponManager;

        private void Start()
        {
            weightManager = GetComponentInChildren<AnimatorLayerWeightManager>();
            animator = GetComponentInChildren<Animator>();
            weaponManager = GetComponent<WeaponManager>();
            playerController = GetComponent<PlayerController>();
        }

        [Header("Reach Procedural Anim Settings")]
        public float reach;
        public float reachSpeed;
        [Header("Rigging Assignments")]
        public Rig rightArmRig;
        public Rig leftArmRig;
        public TwoBoneIKConstraint rightHandIK;
        public TwoBoneIKConstraint leftHandIK;
        public Transform rightHandTarget;
        public Transform leftHandTarget;
        public Transform weaponParent;
        [Header("Stow Points")]
        public Transform spineStow;
        public Transform leftHipStow;
        void OnInteract()
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, reach))
            {
                if (hit.transform.GetComponent<Weapon>())
                {
                    if (equipWeaponRunning) { return; }

                    // If we already have a weapon equipped stow the weapon
                    if (weaponManager.equippedWeapon != null) { StartCoroutine(StowWeapon()); }

                    StartCoroutine(EquipWeapon(hit));
                }
            }
        }

        bool equipWeaponRunning;
        private IEnumerator EquipWeapon(RaycastHit hit)
        {
            equipWeaponRunning = true;
            // Wait until our stow or draw coroutine is finished
            yield return new WaitUntil(() => !stowDrawRunning);

            Transform weapon = hit.transform;

            playerController.disableLookInput = true;

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

            // Parent weapon to the constraint object, typically this is the right hand
            weapon.SetParent(weaponParent, true);

            rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 0;

            yield return new WaitUntil(() => rightArmRig.weight <= 0.1);

            // Set target back to the hand bone since this is the hand that controls the weapon
            rightHandTarget.GetComponent<FollowTarget>().target = rightHandIK.data.tip;

            weaponManager.AddWeapon(weapon.GetComponent<Weapon>());
            weaponManager.DrawWeapon(weaponManager.weapons.Count-1); // Draw most recently added weapon

            playerController.disableLookInput = false;
            animator.SetBool("pickUpWeapon", false);
            equipWeaponRunning = false;
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
                playerController.rotateBodyWithCamera = value.isPressed;
                animator.SetBool("attack2", value.isPressed);
            }
        }

        bool stowDrawRunning;
        void OnSlot0() // TODO not finished yet
        {
            if (stowDrawRunning) { return; }
            if (weaponManager.GetWeapon(0) == null) { return; }
            if ((animator.GetCurrentAnimatorStateInfo(0).IsTag("Draw Weapon") | animator.GetCurrentAnimatorStateInfo(0).IsTag("Stow Weapon"))
                | animator.IsInTransition(0)) { return; }

            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Weapon Combat Idle"))
            {
                StartCoroutine(StowWeapon());

                if (weaponManager.equippedWeapon != weaponManager.GetWeapon(0))
                {
                    StartCoroutine(DrawWeapon(0));
                }
            }
            else
            {
                StartCoroutine(DrawWeapon(0));
            }
        }

        void OnSlot1()
        {
            if (stowDrawRunning) { return; }
            if (weaponManager.GetWeapon(1) == null) { return; }
            if ((animator.GetCurrentAnimatorStateInfo(0).IsTag("Draw Weapon") | animator.GetCurrentAnimatorStateInfo(0).IsTag("Stow Weapon"))
                | animator.IsInTransition(0)) { return; }

            if (animator.GetCurrentAnimatorStateInfo(1).IsName("Weapon Combat Idle"))
            {
                StartCoroutine(StowWeapon());

                if (weaponManager.equippedWeapon != weaponManager.GetWeapon(1))
                {
                    StartCoroutine(DrawWeapon(1));
                }
            }
            else
            {
                StartCoroutine(DrawWeapon(1));
            }
        }

        private IEnumerator DrawWeapon(int slotIndex)
        {
            yield return new WaitUntil(() => !stowDrawRunning);

            stowDrawRunning = true;
            
            float originalSpeed = playerController.animatorSpeed;
            playerController.animatorSpeed = weaponManager.GetWeapon(slotIndex).drawSpeed;

            animator.SetBool("stowWeapon", true);
            weightManager.SetLayerWeight(weaponManager.GetWeapon(slotIndex).weaponClass, 1);
            yield return null;
            animator.SetBool("stowWeapon", false);

            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Draw To Combat"));
            // Switch to player mode
            weaponManager.GetWeapon(slotIndex).transform.SetParent(weaponParent, true);
            weaponManager.GetWeapon(slotIndex).ChangeOffset("player");

            // Wait for animation to finish, then change offset and weights
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).length <= animator.GetCurrentAnimatorStateInfo(0).normalizedTime);

            weaponManager.DrawWeapon(slotIndex);

            leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            leftHandTarget.GetComponent<FollowTarget>().target = weaponManager.equippedWeapon.transform.Find("ref_left_hand_grip");

            playerController.animatorSpeed = originalSpeed;
            stowDrawRunning = false;
        }

        private IEnumerator StowWeapon()
        {
            stowDrawRunning = true;
            
            float originalSpeed = playerController.animatorSpeed;
            playerController.animatorSpeed = weaponManager.equippedWeapon.drawSpeed;

            animator.SetBool("stowWeapon", true);

            leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 0;
            leftHandTarget.GetComponent<FollowTarget>().target = leftHandIK.data.tip;
            yield return null;
            animator.SetBool("stowWeapon", false);

            // Wait for stow weapon animation to finish
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Stow Weapon"));
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).length <= animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            // Switch to stowed mode

            // Check weapon component for what to stow
            if (weaponManager.equippedWeapon.stowPoint == "Spine")
            {
                weaponManager.equippedWeapon.transform.SetParent(spineStow, true);
            }
            else if (weaponManager.equippedWeapon.stowPoint == "Left Hip")
            {
                weaponManager.equippedWeapon.transform.SetParent(leftHipStow, true);
            }
            
            weaponManager.equippedWeapon.ChangeOffset("stowed");

            // Wait for the stow animation to finish playing, then change the layer weight
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"));
            weightManager.SetLayerWeight(weaponManager.equippedWeapon.weaponClass, 0);
            weaponManager.StowWeapon();

            playerController.animatorSpeed = originalSpeed;
            stowDrawRunning = false;
        }
    }
}
