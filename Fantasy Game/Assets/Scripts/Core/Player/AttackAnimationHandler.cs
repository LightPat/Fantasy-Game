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
        [Header("Weapon Grip Points")]
        public Transform greatSwordGrip;
        public Transform rifleGrip;
        public Transform rifleStowTransition;
        [Header("Stow Points")]
        public Transform spineStow;
        public Transform leftHipStow;

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

                    string weaponClass = hit.transform.GetComponent<Weapon>().weaponClass;
                    if (weaponClass == "Great Sword")
                    {
                        StartCoroutine(EquipGreatSword(hit));
                    }
                    else if (weaponClass == "Rifle")
                    {
                        StartCoroutine(EquipRifle(hit));
                    }
                    else
                    {
                        Debug.LogWarning("You are trying to equip a weapon with an invalid weapon class " + hit.transform.GetComponent<Weapon>());
                    }
                }
            }
        }

        [Header("Attack1 Settings")]
        public float attackReach;
        public float attackDamage;
        void OnAttack1(InputValue value) // TODO change this to attack1
        {
            animator.SetBool("attack1", value.isPressed);
            if (!value.isPressed) { return; }

            if (weaponManager.equippedWeapon != null) { return; }
            RaycastHit[] allHits = Physics.RaycastAll(Camera.main.transform.position, Camera.main.transform.forward, attackReach);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform == transform)
                {
                    continue;
                }

                if (hit.transform.GetComponent<Attributes>())
                {
                    //hit.transform.GetComponent<Attributes>().InflictDamage(attackDamage, gameObject);
                }
                break;
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
                animator.SetBool("attack2", value.isPressed);
            }
        }

        void OnMelee()
        {
            animator.SetBool("melee", true);
            StartCoroutine(ResetMeleeBool());
        }

        private IEnumerator ResetMeleeBool()
        {
            yield return null;
            animator.SetBool("melee", false);
        }

        void OnSlot0() // TODO not finished yet
        {
            QuerySlot(0);
        }

        void OnSlot1()
        {
            QuerySlot(1);
        }

        void OnSlot2()
        {
            QuerySlot(2);
        }

        public bool stowDrawRunning;
        private void QuerySlot(int slotIndex)
        {
            if (stowDrawRunning) { return; }
            int slot = slotIndex;
            if (weaponManager.GetWeapon(slot) == null) { return; }
            if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex(weaponManager.GetWeapon(slot).weaponClass)).IsName("Idle")) { return; }

            if (weaponManager.equippedWeapon == weaponManager.GetWeapon(slot))
            {
                StartCoroutine(StowWeapon());
            }
            else
            {
                if (weaponManager.equippedWeapon != null)
                {
                    //StartCoroutine(SwitchWeapon(slot));
                    return;
                }

                StartCoroutine(DrawWeapon(slot));
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (weaponManager.equippedWeapon == null) { return; }
            if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex(weaponManager.equippedWeapon.weaponClass)).IsTag("CollisionAttack"))
            {
                return;
            }

            for (int i = 0; i < collision.contactCount; i++)
            {
                // If the collision is detected on one of our equippedWeapon's colliders
                if (collision.GetContact(i).thisCollider.GetComponentInParent<Weapon>() == weaponManager.equippedWeapon)
                {
                    if (collision.transform.GetComponent<Attributes>())
                    {
                        collision.transform.GetComponent<Attributes>().InflictDamage(weaponManager.equippedWeapon.baseDamage, gameObject);
                    }
                }
            }
        }

        private IEnumerator EquipRifle(RaycastHit hit)
        {
            equipWeaponRunning = true;
            // Wait until our stow or draw coroutine is finished
            yield return new WaitUntil(() => !stowDrawRunning);

            Transform weapon = hit.transform;
            playerController.disableLookInput = true;

            // Remove the physics and collider components
            Destroy(weapon.GetComponent<Rigidbody>());

            // Change grip weight
            Transform gripPoint = GetGripPoint(weapon.GetComponent<Weapon>().weaponClass);
            gripPoint.GetComponentInParent<RigWeightTarget>().weightTarget = 1;

            // Reach out hands to grab weapon handle
            rightArmRig.GetComponent<RigWeightTarget>().weightSpeed = reachSpeed;
            rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            rightHandTarget.GetComponent<FollowTarget>().target = weapon.GetComponent<Weapon>().rightHandGrip;

            // Transition into the weapon's animations
            weightManager.SetLayerWeight(weapon.GetComponent<Weapon>().weaponClass, 1);

            // Wait until hands have reached the weapon handle
            yield return new WaitUntil(() => rightArmRig.weight >= 0.9);

            // Activate secondary hand
            leftArmRig.GetComponent<RigWeightTarget>().weightSpeed = reachSpeed;
            leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            leftHandTarget.GetComponent<FollowTarget>().target = weapon.GetComponent<Weapon>().leftHandGrip;

            // Parent weapon to the constraint object, typically this is the right hand
            weapon.SetParent(gripPoint, true);

            weaponManager.AddWeapon(weapon.GetComponent<Weapon>());
            weaponManager.DrawWeapon(weaponManager.weapons.Count - 1); // Draw most recently added weapon

            playerController.disableLookInput = false;
            equipWeaponRunning = false;
        }

        bool equipWeaponRunning;
        private IEnumerator EquipGreatSword(RaycastHit hit)
        {
            equipWeaponRunning = true;
            // Wait until our stow or draw coroutine is finished
            yield return new WaitUntil(() => !stowDrawRunning);

            Transform weapon = hit.transform;
            playerController.disableLookInput = true;

            // Remove the physics and collider components
            Destroy(weapon.GetComponent<Rigidbody>());

            // Change grip weight
            Transform gripPoint = GetGripPoint(weapon.GetComponent<Weapon>().weaponClass);
            gripPoint.GetComponentInParent<RigWeightTarget>().weightTarget = 1;

            // Reach out hands to grab weapon handle
            rightArmRig.GetComponent<RigWeightTarget>().weightSpeed = reachSpeed;
            rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            rightHandTarget.GetComponent<FollowTarget>().target = weapon.GetComponent<Weapon>().rightHandGrip;

            // Transition into the weapon's animations
            weightManager.SetLayerWeight(weapon.GetComponent<Weapon>().weaponClass, 1);

            // Wait until hands have reached the weapon handle
            yield return new WaitUntil(() => rightArmRig.weight >= 0.9);

            // Activate secondary hand
            leftArmRig.GetComponent<RigWeightTarget>().weightSpeed = reachSpeed;
            leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            leftHandTarget.GetComponent<FollowTarget>().target = weapon.GetComponent<Weapon>().leftHandGrip;

            // Don't move IK target while reparenting
            rightHandTarget.GetComponent<FollowTarget>().target = null;

            // Parent weapon to the constraint object, typically this is the right hand
            weapon.SetParent(gripPoint, true);
            Sheath sheath = weapon.GetComponentInChildren<Sheath>(true);
            if (sheath)
            {
                sheath.transform.gameObject.SetActive(true);
                sheath.transform.SetParent(GetStowPoint(weapon.GetComponent<Weapon>().stowPoint), true);
                sheath.hasPlayer = true;
            }

            rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 0;
            yield return new WaitUntil(() => rightArmRig.weight <= 0.1);
            // Set target back to the hand bone since this is the hand that controls the weapon
            rightHandTarget.GetComponent<FollowTarget>().target = rightHandIK.data.tip;

            weaponManager.AddWeapon(weapon.GetComponent<Weapon>());
            weaponManager.DrawWeapon(weaponManager.weapons.Count - 1); // Draw most recently added weapon

            playerController.disableLookInput = false;
            equipWeaponRunning = false;
        }

        private IEnumerator DrawWeapon(int slotIndex)
        {
            stowDrawRunning = true;
            Weapon chosenWeapon = weaponManager.GetWeapon(slotIndex);

            float originalSpeed = playerController.animatorSpeed;
            playerController.animatorSpeed = chosenWeapon.drawSpeed;

            animator.SetBool("drawWeapon", true);
            weightManager.SetLayerWeight(chosenWeapon.weaponClass, 1);
            yield return null;
            animator.SetBool("drawWeapon", false);

            int animLayerIndex = animator.GetLayerIndex(chosenWeapon.weaponClass);
            GetGripPoint(chosenWeapon.weaponClass).GetComponentInParent<RigWeightTarget>().weightTarget = 1;

            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsName("Draw Weapon"));
            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));

            if (chosenWeapon.weaponClass == "Rifle")
            {
                chosenWeapon.ChangeOffset("transition");
                chosenWeapon.transform.SetParent(rifleStowTransition, true);

                yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsName("Draw To Combat"));
                yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));

                rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
                rightHandTarget.GetComponent<FollowTarget>().target = chosenWeapon.rightHandGrip;
            }

            // Switch to player mode
            chosenWeapon.transform.SetParent(GetGripPoint(chosenWeapon.weaponClass), true);
            chosenWeapon.ChangeOffset("player");

            // Wait for animation to finish, then change offset and weights
            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));

            weaponManager.DrawWeapon(slotIndex);

            leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            leftHandTarget.GetComponent<FollowTarget>().target = weaponManager.equippedWeapon.leftHandGrip;

            playerController.animatorSpeed = originalSpeed;
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsName("Idle"));
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
            int animLayerIndex = animator.GetLayerIndex(weaponManager.equippedWeapon.weaponClass);
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsName("Stow Weapon"));

            if (weaponManager.equippedWeapon.weaponClass == "Rifle")
            {
                rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 0;
                rightHandTarget.GetComponent<FollowTarget>().target = rightHandIK.data.tip;

                weaponManager.equippedWeapon.ChangeOffset("transition");
                weaponManager.equippedWeapon.transform.SetParent(rifleStowTransition, true);
            }

            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));
            // Switch to stowed mode
            // Check weapon component for where to stow
            weaponManager.equippedWeapon.transform.SetParent(GetStowPoint(weaponManager.equippedWeapon.stowPoint), true);
            weaponManager.equippedWeapon.ChangeOffset("stowed");

            GetGripPoint(weaponManager.equippedWeapon.weaponClass).GetComponentInParent<RigWeightTarget>().weightTarget = 0;

            // Wait for the stow animation to finish playing, then change the layer weight
            weightManager.SetLayerWeight(weaponManager.equippedWeapon.weaponClass, 0);
            weaponManager.StowWeapon();

            playerController.animatorSpeed = originalSpeed;
            stowDrawRunning = false;
        }

        private IEnumerator SwitchWeapon(int slotIndex)
        {
            stowDrawRunning = true;

            playerController.animatorSpeed = weaponManager.equippedWeapon.drawSpeed;

            animator.SetBool("stowWeapon", true);
            animator.SetBool("switchWeapon", true);

            leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 0;
            leftHandTarget.GetComponent<FollowTarget>().target = leftHandIK.data.tip;
            yield return null;
            animator.SetBool("stowWeapon", false);

            // Wait until stow animation finishes playing and we start drawing the other weapon
            int animLayerIndex = animator.GetLayerIndex(weaponManager.equippedWeapon.weaponClass);
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsName("Stow Weapon"));
            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));
            // Switch to stowed mode
            // Check weapon component for where to stow
            weaponManager.equippedWeapon.transform.SetParent(GetStowPoint(weaponManager.equippedWeapon.stowPoint), true);
            weaponManager.equippedWeapon.ChangeOffset("stowed");

            float originalSpeed = playerController.animatorSpeed;
            playerController.animatorSpeed = weaponManager.GetWeapon(slotIndex).drawSpeed;

            weightManager.SetLayerWeight(weaponManager.equippedWeapon.weaponClass, 0);
            weaponManager.StowWeapon();

            // Start drawing our other weapon
            animLayerIndex = animator.GetLayerIndex(weaponManager.GetWeapon(slotIndex).weaponClass);
            weightManager.SetLayerWeight(weaponManager.GetWeapon(slotIndex).weaponClass, 1);
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsName("Draw To Combat"));

            animator.SetBool("switchWeapon", false);
            // Switch to player mode
            weaponManager.GetWeapon(slotIndex).transform.SetParent(GetGripPoint(weaponManager.GetWeapon(slotIndex).GetComponent<Weapon>().weaponClass), true);
            weaponManager.GetWeapon(slotIndex).ChangeOffset("player");

            // Wait for draw animation to finish, then change offset and weights
            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));

            weaponManager.DrawWeapon(slotIndex);

            leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            leftHandTarget.GetComponent<FollowTarget>().target = weaponManager.equippedWeapon.leftHandGrip;

            playerController.animatorSpeed = originalSpeed;
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsName("Draw To Combat"));
            stowDrawRunning = false;
        }

        private Transform GetStowPoint(string stowType)
        {
            if (stowType == "Spine")
            {
                return spineStow;
            }
            else if (stowType == "Left Hip")
            {
                return leftHipStow;
            }
            else
            {
                Debug.LogWarning("The weapon you are trying to stow has an invalid stow type");
                return null;
            }
        }

        private Transform GetGripPoint(string weaponClass)
        {
            if (weaponClass == "Great Sword")
            {
                return greatSwordGrip;
            }
            else if (weaponClass == "Rifle")
            {
                return rifleGrip;
            }
            else
            {
                Debug.LogWarning("The weapon you are trying to reparent has an invalid weapon class");
                return null;
            }
        }
    }
}
