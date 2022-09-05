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
        [Header("Transition Points")]
        public Transform rifleStowTransition;
        public Transform greatSwordStowTransition;
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
                    //if (equipWeaponRunning) { return; }

                    // If we already have a weapon equipped stow the weapon
                    //if (weaponManager.equippedWeapon != null) { StartCoroutine(StowWeapon()); }

                    StartCoroutine(EquipWeapon(hit));

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

        void OnSlot0()
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

        private void QuerySlot(int slot)
        {
            Weapon chosenWeapon = weaponManager.GetWeapon(slot);
            if (chosenWeapon == null) { return; }

            if (weaponManager.equippedWeapon == chosenWeapon)
                StartCoroutine(StowWeapon());
            else
                StartCoroutine(DrawWeapon(slot));
        }

        private IEnumerator EquipWeapon(RaycastHit hit)
        {
            Weapon weapon = hit.transform.GetComponent<Weapon>();

            // Remove the physics and collider components
            Destroy(weapon.GetComponent<Rigidbody>());

            // Change grip class weight
            Transform gripPoint = GetGripPoint(weapon.GetComponent<Weapon>().weaponClass);
            gripPoint.GetComponentInParent<RigWeightTarget>().weightTarget = 1;

            // Reach out right hand to grab weapon handle
            rightArmRig.GetComponent<RigWeightTarget>().weightSpeed = reachSpeed;
            rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            rightHandTarget.GetComponent<FollowTarget>().target = weapon.GetComponent<Weapon>().rightHandGrip;

            // Transition into the weapon's animations
            weightManager.SetLayerWeight(weapon.GetComponent<Weapon>().weaponClass, 1);

            // Wait until hands have reached the weapon handle
            yield return new WaitUntil(() => rightArmRig.weight == 1);
            // Don't move right hand while reparenting
            rightHandTarget.GetComponent<FollowTarget>().target = null;
            // Parent weapon to the constraint object, typically this is the right hand
            weapon.transform.SetParent(gripPoint, true);

            // Activate left hand
            leftArmRig.GetComponent<RigWeightTarget>().weightSpeed = reachSpeed;
            leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            leftHandTarget.GetComponent<FollowTarget>().target = weapon.GetComponent<Weapon>().leftHandGrip;

            if (weapon.weaponClass == "Rifle")
            {
                rightHandTarget.GetComponent<FollowTarget>().target = weapon.rightHandGrip;
            }
            else if (weapon.weaponClass == "Great Sword")
            {
                rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 0;
                yield return new WaitUntil(() => rightArmRig.weight == 0);
                rightHandTarget.GetComponent<FollowTarget>().target = rightHandIK.data.tip;
            }
            else
            {
                Debug.LogError("You are trying to equip a weapon class that hasn't been implemented yet" + weapon + " " + weapon.weaponClass);
            }

            weaponManager.AddWeapon(weapon.GetComponent<Weapon>());
            weaponManager.DrawWeapon(weaponManager.weapons.Count - 1); // Draw most recently added weapon
        }

        private IEnumerator StowWeapon()
        {
            Weapon chosenWeapon = weaponManager.equippedWeapon;

            // Turn off hand IKs
            leftHandTarget.GetComponent<FollowTarget>().target = leftHandIK.data.tip;
            rightHandTarget.GetComponent<FollowTarget>().target = rightHandIK.data.tip;
            leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 0;
            rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 0;

            animator.SetBool("stow" + chosenWeapon.weaponClass, true);
            yield return null;
            animator.SetBool("stow" + chosenWeapon.weaponClass, false);

            // Parent weapon to move with right hand
            chosenWeapon.transform.SetParent(GetTransitionPoint(chosenWeapon.weaponClass), true);
            chosenWeapon.ChangeOffset("transition");
            GetGripPoint(chosenWeapon.weaponClass).GetComponentInParent<RigWeightTarget>().weightTarget = 0;

            // Wait until stow animation has finished playing
            int animLayerIndex = animator.GetLayerIndex("Draw/Stow Weapon");
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("StowWeapon"));
            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));

            // Change to stowed mode
            weightManager.SetLayerWeight(chosenWeapon.weaponClass, 0);
            chosenWeapon.transform.SetParent(GetStowPoint(chosenWeapon.stowPoint), true);
            chosenWeapon.ChangeOffset("stowed");
            weaponManager.StowWeapon();
        }

        private IEnumerator DrawWeapon(int slotIndex)
        {
            Weapon chosenWeapon = weaponManager.GetWeapon(slotIndex);

            animator.SetBool("draw" + chosenWeapon.weaponClass, true);
            yield return null;
            animator.SetBool("draw" + chosenWeapon.weaponClass, false);

            int animLayerIndex = animator.GetLayerIndex("Draw/Stow Weapon");
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("DrawWeapon"));
            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));

            // Parent weapon to move with right hand
            GetGripPoint(chosenWeapon.weaponClass).GetComponentInParent<RigWeightTarget>().weightTarget = 1;
            chosenWeapon.transform.SetParent(GetTransitionPoint(chosenWeapon.weaponClass), true);
            chosenWeapon.ChangeOffset("transition");

            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("ToCombat"));
            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));

            // Change to player mode
            weightManager.SetLayerWeight(chosenWeapon.weaponClass, 1);
            chosenWeapon.transform.SetParent(GetGripPoint(chosenWeapon.weaponClass), true);
            chosenWeapon.ChangeOffset("player");
            weaponManager.DrawWeapon(slotIndex);

            // Turn on hand IKs
            if (chosenWeapon.weaponClass == "Rifle")
            {
                leftHandTarget.GetComponent<FollowTarget>().target = chosenWeapon.leftHandGrip;
                rightHandTarget.GetComponent<FollowTarget>().target = chosenWeapon.rightHandGrip;
                leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
                rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            }
            else if (chosenWeapon.weaponClass == "Great Sword")
            {
                leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
                leftHandTarget.GetComponent<FollowTarget>().target = chosenWeapon.leftHandGrip;
            }
            else
            {
                Debug.LogWarning("This weapon doesn't have a valid class when trying to draw it " + chosenWeapon + " " + chosenWeapon.weaponClass);
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
                Debug.LogWarning("The weapon you are trying to stow has an invalid stow type" + stowType);
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
                Debug.LogWarning("Invalid weapon grip class " + weaponClass);
                return null;
            }
        }

        private Transform GetTransitionPoint(string weaponClass)
        {
            if (weaponClass == "Great Sword")
            {
                return greatSwordStowTransition;
            }
            else if (weaponClass == "Rifle")
            {
                return rifleStowTransition;
            }
            else
            {
                Debug.LogWarning("The weapon you are trying to reparent has an invalid weapon class" + weaponClass);
                return null;
            }
        }
    }
}
