using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using LightPat.Util;
using System;

namespace LightPat.Core.Player
{
    public class WeaponAnimationHandler : MonoBehaviour
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
        public Rig spineAimRig;
        public RigWeightTarget rightFingerRig;
        public RigWeightTarget leftFingerRig;
        [Header("Weapon Grip Points")]
        public Transform rifleGrip;
        public Transform pistolGrip;
        public Transform greatSwordGrip;
        [Header("Transition Points")]
        public Transform rifleStowTransition;
        public Transform pistolStowTransition;
        public Transform greatSwordStowTransition;
        [Header("Stow Points")]
        public Transform spineStow;
        public Transform leftHipStow;
        public Transform rightHipStow;

        PlayerController playerController;
        AnimatorLayerWeightManager weightManager;
        Animator animator;
        WeaponManager weaponManager;
        FollowTarget[] rightFingerIKs;
        FollowTarget[] leftFingerIKs;

        private void Start()
        {
            playerController = GetComponent<PlayerController>();
            weightManager = GetComponentInChildren<AnimatorLayerWeightManager>();
            animator = GetComponentInChildren<Animator>();
            weaponManager = GetComponent<WeaponManager>();
            rightFingerIKs = rightFingerRig.GetComponentsInChildren<FollowTarget>();
            leftFingerIKs = leftFingerRig.GetComponentsInChildren<FollowTarget>();
        }

        void OnInteract()
        {
            RaycastHit[] allHits = Physics.RaycastAll(Camera.main.transform.position, Camera.main.transform.forward, reach);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform == transform)
                {
                    continue;
                }

                if (hit.transform.GetComponent<Weapon>())
                {
                    if (equipWeaponRunning) { return; }

                    // If we already have a weapon equipped just put the weapon we click in our reserves
                    if (weaponManager.equippedWeapon == null)
                    {
                        StartCoroutine(EquipWeapon(hit));
                    }
                    else
                    {
                        Weapon weapon = hit.transform.GetComponent<Weapon>();

                        // Remove the physics and collider components
                        Destroy(weapon.GetComponent<Rigidbody>());

                        // Parent weapon to stowPoint
                        weapon.transform.SetParent(GetStowPoint(weapon.stowPoint), true);
                        weapon.ChangeOffset("stowed");

                        if (weapon.GetComponent<Rifle>())
                        {
                            // Do nothing
                        }
                        else if (weapon.GetComponent<GreatSword>())
                        {
                            Sheath sheath = weapon.GetComponentInChildren<Sheath>(true);
                            if (sheath)
                            {
                                sheath.gameObject.SetActive(true);
                                sheath.transform.SetParent(GetStowPoint(weapon.stowPoint), true);
                                sheath.hasPlayer = true;
                            }
                        }
                        else if (weapon.GetComponent<Pistol>())
                        {
                            // Do nothing
                        }
                        else
                        {
                            Debug.LogError("You are trying to equip a weapon class that hasn't been implemented yet" + weapon + " " + weapon.animationClass);
                        }

                        weaponManager.AddWeapon(weapon.GetComponent<Weapon>());
                    }
                }
                break;
            }
        }

        void OnAttack1(InputValue value)
        {
            animator.SetBool("attack1", value.isPressed);
            if (!value.isPressed) { return; }

            if (weaponManager.equippedWeapon == null) { return; }

            weaponManager.equippedWeapon.Attack1();
        }

        private void Update()
        {
            if (weaponManager.equippedWeapon == null) { return; }
            if (animator.GetBool("attack1"))
            {
                weaponManager.equippedWeapon.Attack1();
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
                if (value.isPressed)
                {
                    animator.SetBool("attack2", !animator.GetBool("attack2"));
                }
                //animator.SetBool("attack2", value.isPressed);

                if (weaponManager.equippedWeapon.GetComponent<GreatSword>())
                {
                    GetComponent<Attributes>().blocking = value.isPressed;
                }
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

        void OnReload()
        {
            if (weaponManager.equippedWeapon == null) { return; }

            StartCoroutine(weaponManager.equippedWeapon.Reload());
        }

        void OnQueryWeaponSlot(InputValue value)
        {
            int slot = Convert.ToInt32(value.Get()) - 1;

            if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Draw/Stow Weapon")).IsName("Empty")) { return; }
            Weapon chosenWeapon = weaponManager.GetWeapon(slot);
            if (chosenWeapon == null) { return; }

            if (weaponManager.equippedWeapon == chosenWeapon)
                StartCoroutine(StowWeapon());
            else if (weaponManager.equippedWeapon != null)
                StartCoroutine(SwitchWeapon(slot));
            else
                StartCoroutine(DrawWeapon(slot));
        }

        bool equipWeaponRunning;
        private IEnumerator EquipWeapon(RaycastHit hit)
        {
            equipWeaponRunning = true;
            Weapon weapon = hit.transform.GetComponent<Weapon>();

            // Remove the physics and collider components
            Destroy(weapon.GetComponent<Rigidbody>());

            // Change grip class weight
            Transform gripPoint = GetGripPoint(weapon.GetComponent<Weapon>());
            gripPoint.GetComponentInParent<RigWeightTarget>().weightTarget = 1;

            // Reach out right hand to grab weapon handle
            rightArmRig.GetComponent<RigWeightTarget>().weightSpeed = reachSpeed;
            rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            rightHandTarget.GetComponent<FollowTarget>().target = weapon.GetComponent<Weapon>().rightHandGrip;
            rightFingerRig.weightSpeed = reachSpeed;

            // Transition into the weapon's animations
            weightManager.SetLayerWeight(weapon.GetComponent<Weapon>().animationClass, 1);

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
            leftFingerRig.weightSpeed = reachSpeed;

            if (weapon.GetComponent<Rifle>())
            {
                spineAimRig.GetComponent<RigWeightTarget>().weightTarget = 1;
                rightHandTarget.GetComponent<FollowTarget>().target = weapon.rightHandGrip;

                Rifle rifleComponent = weapon.GetComponent<Rifle>();
                Transform rightFingers = rifleComponent.rightFingersGrips;
                Transform leftFingers = rifleComponent.leftFingersGrips;
                for (int i = 0; i < rightFingerIKs.Length; i++)
                {
                    rightFingerIKs[i].target = rightFingers.GetChild(i);
                    leftFingerIKs[i].target = leftFingers.GetChild(i);
                }
                rightFingerRig.weightTarget = 1;
                leftFingerRig.weightTarget = 1;
            }
            else if (weapon.GetComponent<GreatSword>())
            {
                rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 0;
                Transform leftFingers = weapon.GetComponent<GreatSword>().leftFingersGrips;
                for (int i = 0; i < rightFingerIKs.Length; i++)
                {
                    leftFingerIKs[i].target = leftFingers.GetChild(i);
                }
                leftFingerRig.weightTarget = 1;
                yield return new WaitUntil(() => rightArmRig.weight == 0);
                rightHandTarget.GetComponent<FollowTarget>().target = rightHandIK.data.tip;
                playerController.lookAngleUI.gameObject.SetActive(true);
            }
            else if (weapon.GetComponent<Pistol>())
            {
                spineAimRig.GetComponent<RigWeightTarget>().weightTarget = 1;
                rightHandTarget.GetComponent<FollowTarget>().target = weapon.rightHandGrip;

                Pistol pistolComponent = weapon.GetComponent<Pistol>();
                gripPoint.parent.GetComponentInChildren<PistolPositionSolver>().UpdateMultipliers(pistolComponent.forwardMult, pistolComponent.rightMult, pistolComponent.upMult);

                Transform rightFingers = pistolComponent.rightFingersGrips;
                Transform leftFingers = pistolComponent.leftFingersGrips;
                for (int i = 0; i < rightFingerIKs.Length; i++)
                {
                    rightFingerIKs[i].target = rightFingers.GetChild(i);
                    leftFingerIKs[i].target = leftFingers.GetChild(i);
                }
                rightFingerRig.weightTarget = 1;
                leftFingerRig.weightTarget = 1;
            }
            else
            {
                Debug.LogError("Invalid weapon class on weaponEquip()" + weapon + " " + weapon.animationClass);
            }

            int slot = weaponManager.AddWeapon(weapon.GetComponent<Weapon>());
            weaponManager.DrawWeapon(slot); // Draw most recently added weapon
            playerController.rotateBodyWithCamera = true;
            equipWeaponRunning = false;
        }

        private IEnumerator StowWeapon()
        {
            Weapon equippedWeapon = weaponManager.equippedWeapon;
            animator.SetFloat("drawSpeed", equippedWeapon.drawSpeed);

            // Turn off hand IKs
            leftHandTarget.GetComponent<FollowTarget>().target = leftHandIK.data.tip;
            rightHandTarget.GetComponent<FollowTarget>().target = rightHandIK.data.tip;
            leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 0;
            rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 0;
            rightFingerRig.weightTarget = 0;
            leftFingerRig.weightTarget = 0;

            animator.SetBool("stow" + equippedWeapon.animationClass, true);
            yield return null;
            animator.SetBool("stow" + equippedWeapon.animationClass, false);

            // Parent weapon to move with right hand
            equippedWeapon.transform.SetParent(GetTransitionPoint(equippedWeapon), true);
            equippedWeapon.ChangeOffset("transition");
            GetGripPoint(equippedWeapon).GetComponentInParent<RigWeightTarget>().weightTarget = 0;

            if (equippedWeapon.GetComponent<Rifle>())
            {
                spineAimRig.GetComponent<RigWeightTarget>().weightTarget = 0;
                playerController.SetLean(0);
            }
            else if (equippedWeapon.GetComponent<GreatSword>())
            {
                playerController.lookAngleUI.gameObject.SetActive(false);
            }
            else if (equippedWeapon.GetComponent<Pistol>())
            {
                spineAimRig.GetComponent<RigWeightTarget>().weightTarget = 0;
                playerController.SetLean(0);
            }
            else
            {
                Debug.LogError("This weapon doesn't have a valid class when trying to stow it " + equippedWeapon + " " + equippedWeapon.animationClass);
            }

            // Wait until stow animation has finished playing
            int animLayerIndex = animator.GetLayerIndex("Draw/Stow Weapon");
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("StowWeapon"));
            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));

            // Change to stowed mode
            weightManager.SetLayerWeight(equippedWeapon.animationClass, 0);
            equippedWeapon.transform.SetParent(GetStowPoint(equippedWeapon.stowPoint), true);
            equippedWeapon.ChangeOffset("stowed");
            weaponManager.StowWeapon();
            playerController.rotateBodyWithCamera = false;
        }

        private IEnumerator DrawWeapon(int slotIndex)
        {
            Weapon chosenWeapon = weaponManager.GetWeapon(slotIndex);
            animator.SetFloat("drawSpeed", chosenWeapon.drawSpeed);
            animator.SetBool("draw" + chosenWeapon.animationClass, true);
            yield return null;
            animator.SetBool("draw" + chosenWeapon.animationClass, false);

            int animLayerIndex = animator.GetLayerIndex("Draw/Stow Weapon");
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("DrawWeapon"));
            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));

            // Parent weapon to move with right hand
            GetGripPoint(chosenWeapon).GetComponentInParent<RigWeightTarget>().weightTarget = 1;
            chosenWeapon.transform.SetParent(GetTransitionPoint(chosenWeapon), true);
            chosenWeapon.ChangeOffset("transition");

            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("ToCombat"));
            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));

            // Change to player mode
            weightManager.SetLayerWeight(chosenWeapon.animationClass, 1);
            chosenWeapon.transform.SetParent(GetGripPoint(chosenWeapon), true);
            chosenWeapon.ChangeOffset("player");
            weaponManager.DrawWeapon(slotIndex);
            playerController.rotateBodyWithCamera = true;

            // Turn on hand IKs
            if (chosenWeapon.GetComponent<Rifle>())
            {
                leftHandTarget.GetComponent<FollowTarget>().target = chosenWeapon.leftHandGrip;
                rightHandTarget.GetComponent<FollowTarget>().target = chosenWeapon.rightHandGrip;
                leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
                rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
                spineAimRig.GetComponent<RigWeightTarget>().weightTarget = 1;

                Rifle rifleComponent = chosenWeapon.GetComponent<Rifle>();
                Transform rightFingers = rifleComponent.rightFingersGrips;
                Transform leftFingers = rifleComponent.leftFingersGrips;
                for (int i = 0; i < rightFingerIKs.Length; i++)
                {
                    rightFingerIKs[i].target = rightFingers.GetChild(i);
                    leftFingerIKs[i].target = leftFingers.GetChild(i);
                }
                rightFingerRig.weightTarget = 1;
                leftFingerRig.weightTarget = 1;
            }
            else if (chosenWeapon.GetComponent<GreatSword>())
            {
                leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
                leftHandTarget.GetComponent<FollowTarget>().target = chosenWeapon.leftHandGrip;
                playerController.lookAngleUI.gameObject.SetActive(true);
                Transform leftFingers = chosenWeapon.GetComponent<GreatSword>().leftFingersGrips;
                for (int i = 0; i < rightFingerIKs.Length; i++)
                {
                    leftFingerIKs[i].target = leftFingers.GetChild(i);
                }
                leftFingerRig.weightTarget = 1;
            }
            else if (chosenWeapon.GetComponent<Pistol>())
            {
                leftHandTarget.GetComponent<FollowTarget>().target = chosenWeapon.leftHandGrip;
                rightHandTarget.GetComponent<FollowTarget>().target = chosenWeapon.rightHandGrip;
                leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
                rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
                spineAimRig.GetComponent<RigWeightTarget>().weightTarget = 1;
                rightFingerRig.weightTarget = 1;
                leftFingerRig.weightTarget = 1;
            }
            else
            {
                Debug.LogWarning("This weapon doesn't have a valid class when trying to draw it " + chosenWeapon + " " + chosenWeapon.animationClass);
            }
        }

        private IEnumerator SwitchWeapon(int slotIndex)
        {
            // Stow equipped weapon
            Weapon equippedWeapon = weaponManager.equippedWeapon;
            animator.SetFloat("drawSpeed", equippedWeapon.drawSpeed);

            // Turn off hand IKs
            leftHandTarget.GetComponent<FollowTarget>().target = leftHandIK.data.tip;
            rightHandTarget.GetComponent<FollowTarget>().target = rightHandIK.data.tip;
            leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 0;
            rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 0;
            rightFingerRig.weightTarget = 0;
            leftFingerRig.weightTarget = 0;

            animator.SetBool("stow" + equippedWeapon.animationClass, true);
            yield return null;
            animator.SetBool("stow" + equippedWeapon.animationClass, false);

            // Parent weapon to move with right hand
            equippedWeapon.transform.SetParent(GetTransitionPoint(equippedWeapon), true);
            equippedWeapon.ChangeOffset("transition");
            GetGripPoint(equippedWeapon).GetComponentInParent<RigWeightTarget>().weightTarget = 0;

            // Wait until stow animation has started playing
            int animLayerIndex = animator.GetLayerIndex("Draw/Stow Weapon");
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("StowWeapon"));

            // Start drawing next weapon once stow animation has finished playing
            Weapon chosenWeapon = weaponManager.GetWeapon(slotIndex);

            if (equippedWeapon.GetComponent<Rifle>())
            {
                if (!(chosenWeapon.GetComponent<Pistol>() | chosenWeapon.GetComponent<Rifle>()))
                {
                    spineAimRig.GetComponent<RigWeightTarget>().weightTarget = 0;
                    playerController.SetLean(0);
                }
            }
            else if (equippedWeapon.GetComponent<GreatSword>())
            {
                playerController.lookAngleUI.gameObject.SetActive(false);
            }
            else if (equippedWeapon.GetComponent<Pistol>())
            {
                if (!(chosenWeapon.GetComponent<Pistol>() | chosenWeapon.GetComponent<Rifle>()))
                {
                    spineAimRig.GetComponent<RigWeightTarget>().weightTarget = 0;
                    playerController.SetLean(0);
                }
            }
            else
            {
                Debug.LogError("You are trying to equip a weapon class that hasn't been implemented yet" + equippedWeapon + " " + equippedWeapon.animationClass);
            }

            animator.SetBool("draw" + chosenWeapon.animationClass, true);
            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));
            animator.SetFloat("drawSpeed", chosenWeapon.drawSpeed);
            animator.SetBool("draw" + chosenWeapon.animationClass, false);

            // Change to stowed mode
            weightManager.SetLayerWeight(equippedWeapon.animationClass, 0);
            equippedWeapon.transform.SetParent(GetStowPoint(equippedWeapon.stowPoint), true);
            equippedWeapon.ChangeOffset("stowed");
            weaponManager.StowWeapon();

            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("ToCombat"));

            // Parent weapon to move with right hand
            GetGripPoint(chosenWeapon).GetComponentInParent<RigWeightTarget>().weightTarget = 1;
            chosenWeapon.transform.SetParent(GetTransitionPoint(chosenWeapon), true);
            chosenWeapon.ChangeOffset("transition");

            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));

            // Change to player mode
            weightManager.SetLayerWeight(chosenWeapon.animationClass, 1);
            Transform gripPoint = GetGripPoint(chosenWeapon);
            chosenWeapon.transform.SetParent(gripPoint, true);
            chosenWeapon.ChangeOffset("player");
            weaponManager.DrawWeapon(slotIndex);

            // Turn on hand IKs
            if (chosenWeapon.GetComponent<Rifle>())
            {
                leftHandTarget.GetComponent<FollowTarget>().target = chosenWeapon.leftHandGrip;
                rightHandTarget.GetComponent<FollowTarget>().target = chosenWeapon.rightHandGrip;
                leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
                rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
                spineAimRig.GetComponent<RigWeightTarget>().weightTarget = 1;

                Rifle rifleComponent = chosenWeapon.GetComponent<Rifle>();
                Transform rightFingers = rifleComponent.rightFingersGrips;
                Transform leftFingers = rifleComponent.leftFingersGrips;
                for (int i = 0; i < rightFingerIKs.Length; i++)
                {
                    rightFingerIKs[i].target = rightFingers.GetChild(i);
                    leftFingerIKs[i].target = leftFingers.GetChild(i);
                }
                rightFingerRig.weightTarget = 1;
                leftFingerRig.weightTarget = 1;
            }
            else if (chosenWeapon.GetComponent<GreatSword>())
            {
                leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
                leftHandTarget.GetComponent<FollowTarget>().target = chosenWeapon.leftHandGrip;
                playerController.lookAngleUI.gameObject.SetActive(true);
            }
            else if (chosenWeapon.GetComponent<Pistol>())
            {
                leftHandTarget.GetComponent<FollowTarget>().target = chosenWeapon.leftHandGrip;
                rightHandTarget.GetComponent<FollowTarget>().target = chosenWeapon.rightHandGrip;
                leftArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
                rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
                spineAimRig.GetComponent<RigWeightTarget>().weightTarget = 1;

                Pistol pistolComponent = chosenWeapon.GetComponent<Pistol>();
                gripPoint.parent.GetComponentInChildren<PistolPositionSolver>().UpdateMultipliers(pistolComponent.forwardMult, pistolComponent.rightMult, pistolComponent.upMult);

                Transform rightFingers = pistolComponent.rightFingersGrips;
                Transform leftFingers = pistolComponent.leftFingersGrips;
                for (int i = 0; i < rightFingerIKs.Length; i++)
                {
                    rightFingerIKs[i].target = rightFingers.GetChild(i);
                    leftFingerIKs[i].target = leftFingers.GetChild(i);
                }
                rightFingerRig.weightTarget = 1;
                leftFingerRig.weightTarget = 1;
            }
            else
            {
                Debug.LogError("This weapon doesn't have a valid class when trying to draw it " + chosenWeapon + " " + chosenWeapon.animationClass);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (weaponManager.equippedWeapon == null) { return; }
            if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex(weaponManager.equippedWeapon.animationClass)).IsTag("CollisionAttack"))
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
            else if (stowType == "Right Hip")
            {
                return rightHipStow;
            }
            else
            {
                Debug.LogWarning("The weapon you are trying to stow has an invalid stow type" + stowType);
                return null;
            }
        }

        private Transform GetGripPoint(Weapon weapon)
        {
            if (weapon.GetComponent<GreatSword>())
            {
                return greatSwordGrip;
            }
            else if (weapon.GetComponent<Rifle>())
            {
                return rifleGrip;
            }
            else if (weapon.GetComponent<Pistol>())
            {
                return pistolGrip;
            }
            else
            {
                Debug.LogError("Invalid weapon grip class " + weapon);
                return null;
            }
        }

        private Transform GetTransitionPoint(Weapon weapon)
        {
            if (weapon.GetComponent<GreatSword>())
            {
                return greatSwordStowTransition;
            }
            else if (weapon.GetComponent<Rifle>())
            {
                return rifleStowTransition;
            }
            else if (weapon.GetComponent<Pistol>())
            {
                return pistolStowTransition;
            }
            else
            {
                Debug.LogWarning("The weapon you are trying to reparent has an invalid weapon class" + weapon);
                return null;
            }
        }
    }
}
