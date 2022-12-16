using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using System;
using System.Linq;
using LightPat.ProceduralAnimations;
using Unity.Netcode;

namespace LightPat.Core.Player
{
    public class HumanoidWeaponAnimationHandler : NetworkBehaviour
    {
        public int maxWeapons = 3;
        public Transform mainCamera;
        [Header("Rigging Assignments")]
        public RigWeightTarget rightArmRig;
        public RigWeightTarget leftArmRig;
        public TwoBoneIKConstraint rightHandIK;
        public TwoBoneIKConstraint leftHandIK;
        public FollowTarget rightHandTarget;
        public HintPositionSolver rightHandHint;
        public FollowTarget leftHandTarget;
        public HintPositionSolver leftHandHint;
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

        private void Awake()
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
            if (weaponLoadout.GetWeaponListLength() >= maxWeapons) { return; }

            RaycastHit[] allHits = Physics.RaycastAll(playerController.playerCamera.transform.position, playerController.playerCamera.transform.forward, weaponReachDistance);
            Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform == transform) { continue; }
                if (hit.transform.GetComponent<NetworkedWeapon>())
                {
                    EquipWeapon(hit.transform.GetComponent<NetworkedWeapon>());
                }
                break;
            }
        }

        public void EquipWeapon(NetworkedWeapon networkedWeapon)
        {
            Weapon weapon = networkedWeapon.GetComponent<NetworkedWeapon>().GenerateLocalInstance(true);
            if (weapon == null) { return; }
            StartCoroutine(EquipAfter1Frame(weapon));
            for (int i = 0; i < ClientManager.Singleton.weaponPrefabOptions.Length; i++)
            {
                if (ClientManager.Singleton.weaponPrefabOptions[i].weaponName == weapon.weaponName)
                {
                    EquipWeaponServerRpc(i);
                    break;
                }
            }
        }

        IEnumerator EquipAfter1Frame(Weapon weapon)
        {
            yield return null;
            // If we already have a weapon equipped just put the weapon we click in our reserves
            if (weaponLoadout.equippedWeapon == null)
            {
                animatorLayerWeightManager.SetLayerWeight(weapon.GetComponent<Weapon>().animationClass, 1);
                Destroy(weapon.GetComponent<Rigidbody>());
                ReparentWeapon(weapon, "player");
                weaponLoadout.DrawWeapon(weaponLoadout.AddWeapon(weapon));
                EnableCombatIKs();
            }
            else
            {
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

        [ServerRpc]
        void EquipWeaponServerRpc(int weaponIndex)
        {
            GameObject weaponGO = Instantiate(ClientManager.Singleton.weaponPrefabOptions[weaponIndex].gameObject);
            Weapon weapon = weaponGO.GetComponent<NetworkedWeapon>().GenerateLocalInstance(false);
            weapon.transform.position = transform.position + transform.forward + transform.up;
            StartCoroutine(EquipAfter1Frame(weapon));

            List<ulong> clientIdList = NetworkManager.ConnectedClientsIds.ToList();
            clientIdList.Remove(OwnerClientId);
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = clientIdList.ToArray()
                }
            };

            EquipWeaponClientRpc(weaponIndex, clientRpcParams);
        }

        [ClientRpc]
        void EquipWeaponClientRpc(int weaponIndex, ClientRpcParams clientRpcParams = default)
        {
            GameObject weaponGO = Instantiate(ClientManager.Singleton.weaponPrefabOptions[weaponIndex].gameObject);
            Weapon weapon = weaponGO.GetComponent<NetworkedWeapon>().GenerateLocalInstance(false);
            weapon.transform.position = transform.position + transform.forward + transform.up;
            StartCoroutine(EquipAfter1Frame(weapon));
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
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.AddForce(rb.transform.rotation * dropForce, ForceMode.VelocityChange);
                weaponLoadout.RemoveEquippedWeapon();
                DisableCombatIKs();
            }
        }

        public void Attack1(bool pressed)
        {
            animator.SetBool("attack1", pressed);
            if (weaponLoadout.equippedWeapon == null) { return; }
            weaponLoadout.equippedWeapon.Attack1(pressed);
        }

        void OnAttack1(InputValue value)
        {
            Attack1(value.isPressed);
            OnAttack1ServerRpc(value.isPressed);
        }

        [ServerRpc]
        void OnAttack1ServerRpc(bool pressed)
        {
            Attack1(pressed);

            List<ulong> clientIdList = NetworkManager.ConnectedClientsIds.ToList();
            clientIdList.Remove(OwnerClientId);
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = clientIdList.ToArray()
                }
            };

            OnAttack1ClientRpc(pressed, clientRpcParams);
        }

        [ClientRpc]
        void OnAttack1ClientRpc(bool pressed, ClientRpcParams clientRpcParams = default)
        {
            Attack1(pressed);
        }

        [Header("Sword blocking")]
        public Transform blockConstraints;
        public float blockSpeed = 1;
        bool blocking;
        float oldRigSpeed;
        int oldCullingMask;
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
                blocking = value.isPressed;
                animator.SetBool("attack2", blocking);
                if (weaponLoadout.equippedWeapon.GetComponent<GreatSword>())
                {
                    GetComponent<Attributes>().blocking = blocking;
                    if (blocking)
                    {
                        rightHandTarget.target = blockConstraints;
                        rightArmRig.weightTarget = 1;
                        spineAimRig.weightTarget = 1;
                        oldRigSpeed = rightArmRig.weightSpeed;
                        rightArmRig.weightSpeed = blockSpeed;
                        spineAimRig.weightSpeed = blockSpeed;
                        blockConstraints.GetComponent<SwordBlockingIKSolver>().ResetRotation();

                        if (animator.GetFloat("lookAngle") < 0)
                            animator.SetBool("mirrorIdle", true);
                        else
                            animator.SetBool("mirrorIdle", false);

                        if (playerController)
                        {
                            Camera playerCamera = playerController.playerCamera.GetComponent<Camera>();
                            oldCullingMask = playerCamera.cullingMask;
                            playerCamera.cullingMask = -1;
                        }
                    }
                    else
                    {
                        spineAimRig.weightTarget = 0;
                        rightArmRig.weightTarget = 0;
                        spineAimRig.weightSpeed = oldRigSpeed;
                        StartCoroutine(ChangeFollowTargetAfterWeightTargetReached(rightHandTarget, rightHandIK.data.tip, rightArmRig, oldRigSpeed));
                        blockConstraints.GetComponent<SwordBlockingIKSolver>().ResetRotation();
                        if (playerController)
                            playerController.playerCamera.GetComponent<Camera>().cullingMask = oldCullingMask;
                    }
                }
            }
        }

        private IEnumerator ChangeFollowTargetAfterWeightTargetReached(FollowTarget followTarget, Transform newTarget, RigWeightTarget rig, float originalSpeed)
        {
            yield return new WaitUntil(() => rig.GetRig().weight == 0);
            followTarget.target = newTarget;
            rig.weightSpeed = originalSpeed;
        }

        void OnScroll(InputValue value)
        {
            if (blocking)
            {
                blockConstraints.GetComponent<SwordBlockingIKSolver>().ScrollInput(value.Get<Vector2>());
            }
        }

        void OnMelee()
        {
            StartCoroutine(Utilities.ResetAnimatorBoolAfter1Frame(animator, "melee"));
        }

        void OnReload()
        {
            if (weaponLoadout.equippedWeapon == null) { return; }
            if (weaponLoadout.equippedWeapon.GetComponent<GreatSword>())
                blockConstraints.GetComponent<SwordBlockingIKSolver>().ResetRotation();
            StartCoroutine(weaponLoadout.equippedWeapon.Reload());
            OnReloadServerRpc();
        }

        [ServerRpc]
        void OnReloadServerRpc()
        {
            StartCoroutine(weaponLoadout.equippedWeapon.Reload());

            List<ulong> clientIdList = NetworkManager.ConnectedClientsIds.ToList();
            clientIdList.Remove(OwnerClientId);
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = clientIdList.ToArray()
                }
            };

            OnReloadClientRpc(clientRpcParams);
        }

        [ClientRpc]
        void OnReloadClientRpc(ClientRpcParams clientRpcParams = default)
        {
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
            OnQueryWeaponSlotServerRpc(slot);
        }

        [ServerRpc]
        void OnQueryWeaponSlotServerRpc(int slot)
        {
            Weapon chosenWeapon = weaponLoadout.GetWeapon(slot);
            if (weaponLoadout.equippedWeapon == chosenWeapon)
                StartCoroutine(StowWeapon());
            else if (weaponLoadout.equippedWeapon != null)
                StartCoroutine(SwitchWeapon(slot));
            else
                StartCoroutine(DrawWeapon(slot));

            List<ulong> clientIdList = NetworkManager.ConnectedClientsIds.ToList();
            clientIdList.Remove(OwnerClientId);
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = clientIdList.ToArray()
                }
            };

            OnQueryWeaponSlotClientRpc(slot, clientRpcParams);
        }

        [ClientRpc]
        void OnQueryWeaponSlotClientRpc(int slot, ClientRpcParams clientRpcParams = default)
        {
            Weapon chosenWeapon = weaponLoadout.GetWeapon(slot);
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

        private void OnTriggerEnter(Collider other)
        {
            // For inflicting damage using collision weapons (swords)
            if (weaponLoadout.equippedWeapon == null) { return; }
            GreatSword sword = weaponLoadout.equippedWeapon.GetComponent<GreatSword>();
            if (!sword) { return; }
            if (!sword.swinging) { return; }

            if (other.GetComponentInParent<Sliceable>())
                sword.SliceStart();
            else if(other.gameObject.GetComponentInParent<Attributes>())
                other.gameObject.GetComponentInParent<Attributes>().InflictDamage(weaponLoadout.equippedWeapon.baseDamage, gameObject);
            else
                sword.StopSwing();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<Sliceable>())
                weaponLoadout.equippedWeapon.GetComponent<GreatSword>().SliceEnd(other);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (collision.collider.CompareTag("Stairs") & (Mathf.Abs(animator.GetFloat("moveInputX")) > 0.5f | Mathf.Abs(animator.GetFloat("moveInputY")) > 0.5f))
            {
                float[] yPos = new float[collision.contactCount];
                for (int i = 0; i < collision.contactCount; i++)
                {
                    yPos[i] = collision.GetContact(i).point.y;
                }

                float translateDistance = yPos.Max() - transform.position.y;

                // TODO Change it so that we can't go up stairs that are too high for us
                //if (collision.collider.bounds.size.y - translateDistance > maxStairStepDistance) { return; }

                if (translateDistance < 0) { return; }
                transform.Translate(new Vector3(0, translateDistance, 0));
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
                leftHandHint.offset = new Vector3(-1, -1, 0);
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
            leftHandHint.offset = new Vector3(-0.5f, -0.5f, 0);

            if (playerController)
            {
                playerController.SetLean(0);
                playerController.playerHUD.ammoDisplay.gameObject.SetActive(false);
                playerController.playerHUD.lookAngleDisplay.gameObject.SetActive(false);
                playerController.rotateBodyWithCamera = false;
            }            
        }

        void OnDeath()
        {
            Attack1(false);
            OnDrop();
        }

        void OnAttacked(GameObject attacker)
        {
            //Debug.Log(name + " is being attacked by: " + attacker);
        }
    }
}
