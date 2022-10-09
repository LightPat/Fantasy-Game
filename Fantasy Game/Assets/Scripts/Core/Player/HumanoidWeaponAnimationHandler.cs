using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using LightPat.Util;
using System;

namespace LightPat.Core.Player
{
    [RequireComponent(typeof(WeaponLoadout))]
    public class HumanoidWeaponAnimationHandler : MonoBehaviour
    {
        [Header("Rigging Assignments")]
        public RigWeightTarget rightArmRig;
        public RigWeightTarget leftArmRig;
        public TwoBoneIKConstraint rightHandIK;
        public TwoBoneIKConstraint leftHandIK;
        public FollowTarget rightHandTarget;
        public FollowTarget leftHandTarget;
        public RigWeightTarget spineAimRig;
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
        AnimatorLayerWeightManager animatorLayerWeightManager;
        Animator animator;
        WeaponLoadout weaponLoadout;
        FollowTarget[] rightFingerIKs;
        FollowTarget[] leftFingerIKs;

        private void Start()
        {
            playerController = GetComponent<PlayerController>();
            animatorLayerWeightManager = GetComponentInChildren<AnimatorLayerWeightManager>();
            animator = GetComponentInChildren<Animator>();
            weaponLoadout = GetComponent<WeaponLoadout>();
            rightFingerIKs = rightFingerRig.GetComponentsInChildren<FollowTarget>();
            leftFingerIKs = leftFingerRig.GetComponentsInChildren<FollowTarget>();

            StartCoroutine(EquipInitialWeapons());
        }

        private IEnumerator EquipInitialWeapons()
        {
            Weapon equippedWeapon = weaponLoadout.equippedWeapon;
            // Do this to prevent warning from invalid animation layers in other scripts' Update() methods
            weaponLoadout.equippedWeapon = null;
            // Wait 1 frame to prevent burst job errors from occuring from the rigbuilder
            yield return null;

            foreach (Weapon startingWeapon in weaponLoadout.startingWeapons)
            {
                Weapon weapon = startingWeapon;
                // If this is a prefab that hasn't been instantiated
                if (startingWeapon.gameObject.scene.name == null)
                {
                    GameObject g = Instantiate(startingWeapon.gameObject);
                    weapon = g.GetComponent<Weapon>();
                    ReparentWeapon(weapon, "stowed");
                    g.transform.localPosition = weapon.stowedPositionOffset;
                    g.transform.localRotation = Quaternion.Euler(weapon.stowedRotationOffset);
                    g.name = startingWeapon.name;
                    // Wait 1 frame so that the change offset call will not interfere with the weapon's Start() method
                    yield return null;
                }

                Destroy(weapon.GetComponent<Rigidbody>());
                ReparentWeapon(weapon, "stowed");

                Sheath sheath = weapon.GetComponentInChildren<Sheath>(true);
                if (sheath)
                {
                    sheath.gameObject.SetActive(true);
                    sheath.transform.SetParent(GetStowPoint(weapon), true);
                    sheath.hasPlayer = true;
                }

                weaponLoadout.AddWeapon(weapon.GetComponent<Weapon>());
            }

            if (equippedWeapon)
            {
                Weapon weapon = equippedWeapon;
                Weapon startingWeapon = equippedWeapon;
                // If this is a prefab that hasn't been spawned in yet
                if (startingWeapon.gameObject.scene.name == null)
                {
                    GameObject g = Instantiate(startingWeapon.gameObject);
                    weapon = g.GetComponent<Weapon>();
                    ReparentWeapon(weapon, "player");
                    g.transform.localPosition = weapon.stowedPositionOffset;
                    g.transform.localRotation = Quaternion.Euler(weapon.stowedRotationOffset);
                    g.name = startingWeapon.name;
                    weaponLoadout.equippedWeapon = weapon;
                    // Wait 1 frame so that the change offset call will not interfere with the weapon's Start() method
                    yield return null;
                }

                Destroy(weapon.GetComponent<Rigidbody>());
                ReparentWeapon(weapon, "stowed");

                Sheath sheath = weapon.GetComponentInChildren<Sheath>(true);
                if (sheath)
                {
                    sheath.gameObject.SetActive(true);
                    sheath.transform.SetParent(GetStowPoint(weapon), true);
                    sheath.hasPlayer = true;
                }

                yield return DrawWeapon(weaponLoadout.AddWeapon(weapon.GetComponent<Weapon>()));
                weaponLoadout.ChangeLoadoutPositions(0, weaponLoadout.GetEquippedWeaponIndex());
            }
        }

        [Header("Interact Setings")]
        public float weaponReachDistance;
        void OnInteract()
        {
            RaycastHit[] allHits = Physics.RaycastAll(Camera.main.transform.position, Camera.main.transform.forward, weaponReachDistance);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform == transform) { continue; }
                if (hit.transform.GetComponent<Weapon>())
                {
                    // If we already have a weapon equipped just put the weapon we click in our reserves
                    if (weaponLoadout.equippedWeapon == null)
                    {
                        Weapon weapon = hit.transform.GetComponent<Weapon>();
                        animatorLayerWeightManager.SetLayerWeight(weapon.GetComponent<Weapon>().animationClass, 1);
                        Destroy(weapon.GetComponent<Rigidbody>());
                        ReparentWeapon(weapon, "player");
                        weaponLoadout.DrawWeapon(weaponLoadout.AddWeapon(weapon));
                        EnableCombatIKs();
                    }
                    else
                    {
                        Weapon weapon = hit.transform.GetComponent<Weapon>();
                        Destroy(weapon.GetComponent<Rigidbody>());
                        ReparentWeapon(weapon, "stowed");

                        Sheath sheath = weapon.GetComponentInChildren<Sheath>(true);
                        if (sheath)
                        {
                            sheath.gameObject.SetActive(true);
                            sheath.transform.SetParent(GetStowPoint(weapon), true);
                            sheath.hasPlayer = true;
                        }
                        weaponLoadout.AddWeapon(weapon.GetComponent<Weapon>());
                    }
                }
                break;
            }
        }

        [Header("Weapon Drop Settings")]
        public Vector3 dropForce;
        void OnDrop()
        {
            if (weaponLoadout.equippedWeapon)
            {
                animatorLayerWeightManager.SetLayerWeight(weaponLoadout.equippedWeapon.animationClass, 0);
                weaponLoadout.equippedWeapon.transform.position += transform.forward;
                weaponLoadout.equippedWeapon.transform.SetParent(null, true);
                Rigidbody rb = weaponLoadout.equippedWeapon.gameObject.AddComponent<Rigidbody>();
                rb.AddForce(rb.transform.rotation * dropForce, ForceMode.VelocityChange);
                weaponLoadout.RemoveEquippedWeapon();
                DisableCombatIKs();
            }
        }

        void OnAttack1(InputValue value)
        {
            animator.SetBool("attack1", value.isPressed);
            if (weaponLoadout.equippedWeapon == null) { return; }
            weaponLoadout.equippedWeapon.Attack1(value.isPressed);
        }

        [Header("Sword blocking")]
        public Transform blockConstraints;
        void OnAttack2(InputValue value)
        {
            if (weaponLoadout.equippedWeapon == null) // If we have no weapon active in our hands, activate fist combat
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
                if (!value.isPressed) { return; }
                bool attack2 = !animator.GetBool("attack2");
                animator.SetBool("attack2", attack2);
                if (weaponLoadout.equippedWeapon.GetComponent<GreatSword>())
                {
                    GetComponent<Attributes>().blocking = attack2;
                    if (attack2)
                    {
                        rightHandTarget.target = blockConstraints;
                        rightArmRig.weightTarget = 1;
                    }
                    else
                    {
                        rightArmRig.weightTarget = 0;
                        rightHandTarget.target = rightHandIK.data.tip;
                    }
                }
            }
        }

        void OnMelee()
        {
            StartCoroutine(Utilities.ResetAnimatorBoolAfter1Frame(animator, "melee"));
        }

        void OnReload()
        {
            if (weaponLoadout.equippedWeapon == null) { return; }
            StartCoroutine(weaponLoadout.equippedWeapon.Reload());
        }

        void OnQueryWeaponSlot(InputValue value)
        {
            int slot = Convert.ToInt32(value.Get()) - 1;
            if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Draw/Stow Weapon")).IsName("Empty")) { return; }
            Weapon chosenWeapon = weaponLoadout.GetWeapon(slot);
            if (chosenWeapon == null) { return; }
            if (weaponLoadout.equippedWeapon == chosenWeapon)
                StartCoroutine(StowWeapon());
            else if (weaponLoadout.equippedWeapon != null)
                StartCoroutine(SwitchWeapon(slot));
            else
                StartCoroutine(DrawWeapon(slot));
        }

        private IEnumerator StowWeapon()
        {
            leftHandTarget.lerp = true;
            Weapon equippedWeapon = weaponLoadout.equippedWeapon;
            equippedWeapon.disableAttack = true;
            animator.SetFloat("drawSpeed", equippedWeapon.drawSpeed);

            DisableCombatIKs();

            animator.SetBool("stow" + equippedWeapon.animationClass, true);
            yield return null;
            animator.SetBool("stow" + equippedWeapon.animationClass, false);

            // Parent weapon to move with right hand
            ReparentWeapon(equippedWeapon, "transition");

            // Wait until stow animation has finished playing
            int animLayerIndex = animator.GetLayerIndex("Draw/Stow Weapon");
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("StowWeapon"));
            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));

            // Change to stowed mode
            animatorLayerWeightManager.SetLayerWeight(equippedWeapon.animationClass, 0);
            ReparentWeapon(equippedWeapon, "stowed");
            weaponLoadout.StowWeapon();
            equippedWeapon.disableAttack = false;

            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Draw/Stow Weapon")).IsName("Empty"));
            leftHandTarget.lerp = false;
        }

        private IEnumerator DrawWeapon(int slotIndex)
        {
            leftHandTarget.lerp = true;
            Weapon chosenWeapon = weaponLoadout.GetWeapon(slotIndex);
            animator.SetFloat("drawSpeed", chosenWeapon.drawSpeed);
            animator.SetBool("draw" + chosenWeapon.animationClass, true);
            yield return null;
            animator.SetBool("draw" + chosenWeapon.animationClass, false);

            int animLayerIndex = animator.GetLayerIndex("Draw/Stow Weapon");
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("DrawWeapon"));
            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));

            // Parent weapon to move with right hand
            ReparentWeapon(chosenWeapon, "transition");

            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("ToCombat"));
            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));

            // Change to player mode
            animatorLayerWeightManager.SetLayerWeight(chosenWeapon.animationClass, 1);
            ReparentWeapon(chosenWeapon, "player");
            weaponLoadout.DrawWeapon(slotIndex);
            EnableCombatIKs();

            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Draw/Stow Weapon")).IsName("Empty"));
            leftHandTarget.lerp = false;
        }

        private IEnumerator SwitchWeapon(int slotIndex)
        {
            leftHandTarget.lerp = true;
            // Stow equipped weapon
            Weapon equippedWeapon = weaponLoadout.equippedWeapon;
            equippedWeapon.disableAttack = true;
            animator.SetFloat("drawSpeed", equippedWeapon.drawSpeed);

            DisableCombatIKs();

            animator.SetBool("stow" + equippedWeapon.animationClass, true);
            yield return null;
            animator.SetBool("stow" + equippedWeapon.animationClass, false);

            // Parent weapon to move with right hand
            ReparentWeapon(equippedWeapon, "transition");

            // Wait until stow animation has started playing
            int animLayerIndex = animator.GetLayerIndex("Draw/Stow Weapon");
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("StowWeapon"));

            // Start drawing next weapon once stow animation has finished playing
            Weapon chosenWeapon = weaponLoadout.GetWeapon(slotIndex);

            animator.SetBool("draw" + chosenWeapon.animationClass, true);
            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));
            animator.SetFloat("drawSpeed", chosenWeapon.drawSpeed);
            animator.SetBool("draw" + chosenWeapon.animationClass, false);

            // Change to stowed mode
            animatorLayerWeightManager.SetLayerWeight(equippedWeapon.animationClass, 0);
            ReparentWeapon(equippedWeapon, "stowed");
            weaponLoadout.StowWeapon();
            equippedWeapon.disableAttack = false;

            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("ToCombat"));

            // Parent weapon to move with right hand
            ReparentWeapon(chosenWeapon, "transition");

            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));

            // Change to player mode
            animatorLayerWeightManager.SetLayerWeight(chosenWeapon.animationClass, 1);
            Transform gripPoint = GetGripPoint(chosenWeapon);
            ReparentWeapon(chosenWeapon, "player");
            weaponLoadout.DrawWeapon(slotIndex);
            EnableCombatIKs();

            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Draw/Stow Weapon")).IsName("Empty"));
            leftHandTarget.lerp = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (weaponLoadout.equippedWeapon == null) { return; }
            if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex(weaponLoadout.equippedWeapon.animationClass)).IsTag("CollisionAttack"))
            {
                return;
            }

            for (int i = 0; i < collision.contactCount; i++)
            {
                // If the collision is detected on one of our equippedWeapon's colliders
                if (collision.GetContact(i).thisCollider.GetComponentInParent<Weapon>() == weaponLoadout.equippedWeapon)
                {
                    if (collision.transform.GetComponent<Attributes>())
                    {
                        collision.transform.GetComponent<Attributes>().InflictDamage(weaponLoadout.equippedWeapon.baseDamage, gameObject);
                    }
                }
            }
        }

        private void ReparentWeapon(Weapon weapon, string action)
        {
            if (action == "player")
            {
                weapon.transform.SetParent(GetGripPoint(weapon), true);
            }
            else if (action == "transition")
            {
                weapon.transform.SetParent(GetTransitionPoint(weapon), true);
                foreach (Collider c in weapon.GetComponentsInChildren<Collider>())
                {
                    c.enabled = true;
                }
            }
            else if (action == "stowed")
            {
                weapon.transform.SetParent(GetStowPoint(weapon), true);
                foreach (Collider c in weapon.GetComponentsInChildren<Collider>())
                {
                    c.enabled = false;
                }
            }
            else
            {
                Debug.LogError("The weapon action you are trying to reparent is invalid" + weapon + " " + action);
            }

            weapon.ChangeOffset(action);
        }

        private Transform GetStowPoint(Weapon weapon)
        {
            if (weapon.stowPoint == "Spine")
            {
                return spineStow;
            }
            else if (weapon.stowPoint == "Left Hip")
            {
                return leftHipStow;
            }
            else if (weapon.stowPoint == "Right Hip")
            {
                return rightHipStow;
            }
            else
            {
                Debug.LogWarning("The weapon you are trying to stow has an invalid stow type" + weapon.stowPoint);
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

        private void EnableCombatIKs()
        {
            if (playerController)
                playerController.rotateBodyWithCamera = true;

            if (weaponLoadout.equippedWeapon.GetComponent<Rifle>())
            {
                leftHandTarget.target = weaponLoadout.equippedWeapon.leftHandGrip;
                rightHandTarget.target = weaponLoadout.equippedWeapon.rightHandGrip;
                leftArmRig.weightTarget = 1;
                rightArmRig.weightTarget = 1;
                spineAimRig.weightTarget = 1;

                Rifle rifleComponent = weaponLoadout.equippedWeapon.GetComponent<Rifle>();
                Transform rightFingers = rifleComponent.rightFingersGrips;
                Transform leftFingers = rifleComponent.leftFingersGrips;
                for (int i = 0; i < rightFingerIKs.Length; i++)
                {
                    rightFingerIKs[i].target = rightFingers.GetChild(i);
                    leftFingerIKs[i].target = leftFingers.GetChild(i);
                }
                rightFingerRig.weightTarget = 1;
                leftFingerRig.weightTarget = 1;

                if (playerController)
                {
                    playerController.playerHUD.ammoDisplay.gameObject.SetActive(true);
                    playerController.playerHUD.SetAmmoText(rifleComponent.currentBullets + " / " + rifleComponent.magazineSize);
                }
            }
            else if (weaponLoadout.equippedWeapon.GetComponent<GreatSword>())
            {
                leftArmRig.weightTarget = 1;
                leftHandTarget.target = weaponLoadout.equippedWeapon.leftHandGrip;
                Transform leftFingers = weaponLoadout.equippedWeapon.GetComponent<GreatSword>().leftFingersGrips;
                for (int i = 0; i < rightFingerIKs.Length; i++)
                {
                    leftFingerIKs[i].target = leftFingers.GetChild(i);
                }
                leftFingerRig.weightTarget = 1;

                if (playerController)
                    playerController.playerHUD.lookAngleDisplay.gameObject.SetActive(true);
            }
            else if (weaponLoadout.equippedWeapon.GetComponent<Pistol>())
            {
                leftHandTarget.target = weaponLoadout.equippedWeapon.leftHandGrip;
                rightHandTarget.target = weaponLoadout.equippedWeapon.rightHandGrip;
                leftArmRig.weightTarget = 1;
                rightArmRig.weightTarget = 1;
                spineAimRig.weightTarget = 1;

                Pistol pistolComponent = weaponLoadout.equippedWeapon.GetComponent<Pistol>();
                pistolGrip.parent.GetComponentInChildren<PistolPositionSolver>().UpdateMultipliers(pistolComponent.constraintPositionMultipliers);

                Transform rightFingers = pistolComponent.rightFingersGrips;
                Transform leftFingers = pistolComponent.leftFingersGrips;
                for (int i = 0; i < rightFingerIKs.Length; i++)
                {
                    rightFingerIKs[i].target = rightFingers.GetChild(i);
                    leftFingerIKs[i].target = leftFingers.GetChild(i);
                }
                rightFingerRig.weightTarget = 1;
                leftFingerRig.weightTarget = 1;

                if (playerController)
                {
                    playerController.playerHUD.ammoDisplay.gameObject.SetActive(true);
                    playerController.playerHUD.SetAmmoText(pistolComponent.currentBullets + " / " + pistolComponent.magazineSize);
                }
            }
            else
            {
                Debug.LogWarning("This weapon doesn't have a valid class when trying to draw it " + weaponLoadout.equippedWeapon + " " + weaponLoadout.equippedWeapon.animationClass);
            }
        }

        private void DisableCombatIKs()
        {
            // Turn off all IKs
            leftHandTarget.target = leftHandIK.data.tip;
            rightHandTarget.target = rightHandIK.data.tip;
            leftArmRig.weightTarget = 0;
            rightArmRig.weightTarget = 0;
            rightFingerRig.weightTarget = 0;
            leftFingerRig.weightTarget = 0;
            spineAimRig.weightTarget = 0;

            if (playerController)
            {
                playerController.SetLean(0);
                playerController.playerHUD.ammoDisplay.gameObject.SetActive(false);
                playerController.playerHUD.lookAngleDisplay.gameObject.SetActive(false);
                playerController.rotateBodyWithCamera = false;
            }            
        }
    }
}