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
        [Header("Weapon Grip Points")]
        public Transform greatSwordGrip;
        public Transform greatSwordBlocking;
        public Transform rifleGrip;
        [Header("Transition Points")]
        public Transform rifleStowTransition;
        public Transform greatSwordStowTransition;
        [Header("Stow Points")]
        public Transform spineStow;
        public Transform leftHipStow;

        AnimatorLayerWeightManager weightManager;
        Animator animator;
        WeaponManager weaponManager;
        PlayerController playerController;

        private void Start()
        {
            weightManager = GetComponentInChildren<AnimatorLayerWeightManager>();
            animator = GetComponentInChildren<Animator>();
            weaponManager = GetComponent<WeaponManager>();
            playerController = GetComponent<PlayerController>();
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

                        if (weapon.weaponClass == "Rifle")
                        {
                            // Do nothing
                        }
                        else if (weapon.weaponClass == "Great Sword")
                        {
                            Sheath sheath = weapon.GetComponentInChildren<Sheath>(true);
                            if (sheath)
                            {
                                sheath.gameObject.SetActive(true);
                                sheath.transform.SetParent(GetStowPoint(weapon.stowPoint), true);
                                sheath.hasPlayer = true;
                            }
                        }
                        else
                        {
                            Debug.LogError("You are trying to equip a weapon class that hasn't been implemented yet" + weapon + " " + weapon.weaponClass);
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
            string weaponClass = weaponManager.equippedWeapon.weaponClass;

            weaponManager.equippedWeapon.GetComponent<Weapon>().Attack1();

            if (weaponClass == "Great Sword")
            {
                
            }
            else if (weaponClass == "Rifle")
            {
                //RaycastHit[] allHits = Physics.RaycastAll(Camera.main.transform.position, Camera.main.transform.forward);
                //System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

                //foreach (RaycastHit hit in allHits)
                //{
                //    if (hit.transform == transform)
                //    {
                //        continue;
                //    }

                //    if (hit.transform.GetComponent<Attributes>())
                //    {
                //        hit.transform.GetComponent<Attributes>().InflictDamage(weaponManager.equippedWeapon.baseDamage, gameObject);
                //    }
                //    break;
                //}
            }
            else
            {
                Debug.LogWarning("Invalid weapon class attacking " + weaponClass);
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
                //animator.SetBool("attack2", value.isPressed);

                if (weaponManager.equippedWeapon.GetComponent<GreatSword>())
                {
                    blocking = value.isPressed;
                    //animator.SetBool("attack2", blocking);
                    // Procedural Block
                    playerController.disableLookInput = blocking;
                    weaponManager.equippedWeapon.stop = blocking;

                    if (blocking)
                    {
                        weaponManager.equippedWeapon.transform.SetParent(greatSwordBlocking, true);
                        rightHandTarget.GetComponent<FollowTarget>().target = weaponManager.equippedWeapon.rightHandGrip;
                        rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 1;
                        Transform weapon = weaponManager.equippedWeapon.transform;
                        weapon.localPosition = weapon.GetComponent<GreatSword>().blockingPosition;
                        weapon.localEulerAngles = weapon.GetComponent<GreatSword>().blockingRotation;

                        weapon.position = new Vector3(weapon.position.x, transform.position.y + initialblockingYOffset, weapon.transform.position.z);

                        startingLocPos = weaponManager.equippedWeapon.transform.localPosition;
                        
                        verticalOffset = 0;
                        horizontalOffset = 0;
                    }
                    else
                    {
                        weaponManager.equippedWeapon.transform.SetParent(greatSwordGrip, true);
                        rightHandTarget.GetComponent<FollowTarget>().target = rightHandIK.data.tip;
                        rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 0;
                    }
                }
                else if (weaponManager.equippedWeapon.GetComponent<Rifle>())
                {
                    // Aim down sights
                }
            }
        }

        [Header("Blocking Testing")]
        public float blockingSensitivity;
        public float initialblockingYOffset;
        public float downLimit;
        public float upLimit;
        public float horizontalLimit;
        bool blocking;
        Vector2 lookInput;
        Vector3 startingLocPos;
        float verticalOffset;
        float horizontalOffset;
        void OnLook(InputValue value)
        {
            if (!blocking) { return; }

            lookInput = value.Get<Vector2>();

            // Rotate camera only up and down
            //playerController.rotationX -= lookInput.y * playerController.sensitivity * Time.timeScale;
            //playerController.rotationX = Mathf.Clamp(playerController.rotationX, playerController.mouseUpXRotLimit, playerController.mouseDownXRotLimit);
            //Camera.main.transform.eulerAngles = new Vector3(playerController.rotationX, playerController.rotationY, Camera.main.transform.eulerAngles.z);

            Transform t = weaponManager.equippedWeapon.transform;
            float attempt = verticalOffset + lookInput.y * blockingSensitivity * Time.timeScale * playerController.sensitivity;
            if (attempt <= upLimit & attempt > downLimit)
            {
                verticalOffset += lookInput.y * blockingSensitivity * Time.timeScale * playerController.sensitivity;
            }

            attempt = horizontalOffset + lookInput.x * blockingSensitivity * Time.timeScale * playerController.sensitivity;
            if (Mathf.Abs(attempt) <= horizontalLimit)
            {
                horizontalOffset += lookInput.x * blockingSensitivity * Time.timeScale * playerController.sensitivity;
            }
            Vector3 horizontal = Vector3.right * horizontalOffset;

            t.localPosition = new Vector3(startingLocPos.x + horizontal.x, startingLocPos.y + verticalOffset, startingLocPos.z + horizontal.z);
        }

        public float pivotForward;
        public Vector3 lookOffset;
        private void Update()
        {
            if (!blocking) { return; }

            Vector3 pivot = Camera.main.transform.position + Camera.main.transform.forward * pivotForward;
            gizmoPoint = pivot;

            // Make Y axis look at pivot point
            Transform weapon = weaponManager.equippedWeapon.transform;
            var point = pivot - weapon.position;
            var rot = Quaternion.LookRotation(point, Vector3.up);
            weapon.rotation = rot;
            weapon.eulerAngles += lookOffset;
        }

        Vector3 gizmoPoint;
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(gizmoPoint, 0.1f);
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
                spineAimRig.GetComponent<RigWeightTarget>().weightTarget = 1;
            }
            else if (weapon.weaponClass == "Great Sword")
            {
                Sheath sheath = weapon.GetComponentInChildren<Sheath>(true);
                if (sheath)
                {
                    sheath.gameObject.SetActive(true);
                    sheath.transform.SetParent(GetStowPoint(weapon.stowPoint), true);
                    sheath.hasPlayer = true;
                }
                rightArmRig.GetComponent<RigWeightTarget>().weightTarget = 0;
                yield return new WaitUntil(() => rightArmRig.weight == 0);
                rightHandTarget.GetComponent<FollowTarget>().target = rightHandIK.data.tip;
            }
            else
            {
                Debug.LogError("You are trying to equip a weapon class that hasn't been implemented yet" + weapon + " " + weapon.weaponClass);
            }

            int slot = weaponManager.AddWeapon(weapon.GetComponent<Weapon>());
            weaponManager.DrawWeapon(slot); // Draw most recently added weapon
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

            animator.SetBool("stow" + equippedWeapon.weaponClass, true);
            yield return null;
            animator.SetBool("stow" + equippedWeapon.weaponClass, false);

            // Parent weapon to move with right hand
            equippedWeapon.transform.SetParent(GetTransitionPoint(equippedWeapon.weaponClass), true);
            equippedWeapon.ChangeOffset("transition");
            GetGripPoint(equippedWeapon.weaponClass).GetComponentInParent<RigWeightTarget>().weightTarget = 0;

            if (equippedWeapon.weaponClass == "Rifle")
            {
                spineAimRig.GetComponent<RigWeightTarget>().weightTarget = 0;
            }
            else if (equippedWeapon.weaponClass == "Great Sword")
            {
                // Do nothing
            }
            else
            {
                Debug.LogError("You are trying to equip a weapon class that hasn't been implemented yet" + equippedWeapon + " " + equippedWeapon.weaponClass);
            }

            // Wait until stow animation has finished playing
            int animLayerIndex = animator.GetLayerIndex("Draw/Stow Weapon");
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("StowWeapon"));
            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));

            // Change to stowed mode
            weightManager.SetLayerWeight(equippedWeapon.weaponClass, 0);
            equippedWeapon.transform.SetParent(GetStowPoint(equippedWeapon.stowPoint), true);
            equippedWeapon.ChangeOffset("stowed");
            weaponManager.StowWeapon();
        }

        private IEnumerator DrawWeapon(int slotIndex)
        {
            Weapon chosenWeapon = weaponManager.GetWeapon(slotIndex);
            animator.SetFloat("drawSpeed", chosenWeapon.drawSpeed);
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
                spineAimRig.GetComponent<RigWeightTarget>().weightTarget = 1;
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

            animator.SetBool("stow" + equippedWeapon.weaponClass, true);
            yield return null;
            animator.SetBool("stow" + equippedWeapon.weaponClass, false);

            // Parent weapon to move with right hand
            equippedWeapon.transform.SetParent(GetTransitionPoint(equippedWeapon.weaponClass), true);
            equippedWeapon.ChangeOffset("transition");
            GetGripPoint(equippedWeapon.weaponClass).GetComponentInParent<RigWeightTarget>().weightTarget = 0;

            // Wait until stow animation has started playing
            int animLayerIndex = animator.GetLayerIndex("Draw/Stow Weapon");
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("StowWeapon"));

            if (equippedWeapon.weaponClass == "Rifle")
            {
                spineAimRig.GetComponent<RigWeightTarget>().weightTarget = 0;
            }
            else if (equippedWeapon.weaponClass == "Great Sword")
            {
                // Do nothing
            }
            else
            {
                Debug.LogError("You are trying to equip a weapon class that hasn't been implemented yet" + equippedWeapon + " " + equippedWeapon.weaponClass);
            }

            // Start drawing next weapon once stow animation has finished playing
            Weapon chosenWeapon = weaponManager.GetWeapon(slotIndex);
            animator.SetBool("draw" + chosenWeapon.weaponClass, true);
            yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));
            animator.SetFloat("drawSpeed", chosenWeapon.drawSpeed);
            animator.SetBool("draw" + chosenWeapon.weaponClass, false);

            // Change to stowed mode
            weightManager.SetLayerWeight(equippedWeapon.weaponClass, 0);
            equippedWeapon.transform.SetParent(GetStowPoint(equippedWeapon.stowPoint), true);
            equippedWeapon.ChangeOffset("stowed");
            weaponManager.StowWeapon();

            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("ToCombat"));

            // Parent weapon to move with right hand
            GetGripPoint(chosenWeapon.weaponClass).GetComponentInParent<RigWeightTarget>().weightTarget = 1;
            chosenWeapon.transform.SetParent(GetTransitionPoint(chosenWeapon.weaponClass), true);
            chosenWeapon.ChangeOffset("transition");

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
                spineAimRig.GetComponent<RigWeightTarget>().weightTarget = 1;
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
