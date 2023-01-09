using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using System;
using System.Linq;
using LightPat.ProceduralAnimations;
using Unity.Netcode;
using Unity.Netcode.Components;

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
        }

        public override void OnNetworkSpawn()
        {
            weaponLoadout.startingWeapons.Clear();
            int i = 0;
            foreach (int weaponPrefabIndex in ClientManager.Singleton.GetClient(OwnerClientId).spawnWeapons)
            {
                if (i == 0)
                    weaponLoadout.equippedWeapon = ClientManager.Singleton.weaponPrefabOptions[weaponPrefabIndex];
                else
                    weaponLoadout.startingWeapons.Add(ClientManager.Singleton.weaponPrefabOptions[weaponPrefabIndex]);
                i += 1;
            }
            StartCoroutine(EquipInitialWeapons());
        }

        private IEnumerator EquipInitialWeapons()
        {
            Weapon equippedWeapon = weaponLoadout.equippedWeapon;
            // Do this to prevent warning from invalid animation layers in other scripts' Update() methods
            weaponLoadout.equippedWeapon = null;
            // Wait 1 frame to prevent burst job errors from occuring from the rigbuilder
            yield return null;

            if (equippedWeapon)
            {
                Weapon weapon = equippedWeapon;
                Weapon startingWeapon = equippedWeapon;
                // If this is a prefab that hasn't been spawned in yet
                if (startingWeapon.gameObject.scene.name == null)
                {
                    yield return new WaitForEndOfFrame();
                    GameObject g = Instantiate(startingWeapon.gameObject);
                    Destroy(g.GetComponent<NetworkedWeapon>());
                    Destroy(g.GetComponent<NetworkTransform>());
                    Destroy(g.GetComponent<NetworkObject>());
                    yield return new WaitUntil(() => !g.GetComponent<NetworkObject>());
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

                StartCoroutine(DrawWeapon(weaponLoadout.AddWeapon(weapon.GetComponent<Weapon>()), true));
            }

            foreach (Weapon startingWeapon in weaponLoadout.startingWeapons)
            {
                Weapon weapon = startingWeapon;
                // If this is a prefab that hasn't been instantiated
                if (startingWeapon.gameObject.scene.name == null)
                {
                    GameObject g = Instantiate(startingWeapon.gameObject);
                    Destroy(g.GetComponent<NetworkedWeapon>());
                    Destroy(g.GetComponent<NetworkTransform>());
                    Destroy(g.GetComponent<NetworkObject>());
                    yield return new WaitUntil(() => !g.GetComponent<NetworkObject>());
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
        }

        [Header("Interact Setings")]
        public float weaponReachDistance;
        void OnInteract()
        {
            if (weaponLoadout.GetWeaponListLength() >= maxWeapons) { return; }

            RaycastHit[] allHits = Physics.RaycastAll(playerController.playerCamera.transform.position, playerController.playerCamera.transform.forward, weaponReachDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore);
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
            Weapon weapon = networkedWeapon.GetComponent<Weapon>();
            for (int i = 0; i < ClientManager.Singleton.weaponPrefabOptions.Length; i++)
            {
                if (ClientManager.Singleton.weaponPrefabOptions[i].weaponName == weapon.weaponName)
                {
                    EquipWeaponServerRpc(i, networkedWeapon.NetworkObjectId);
                    break;
                }
            }
        }

        IEnumerator EquipAfter1Frame(Weapon weapon)
        {
            yield return null;
            // If we already have a weapon equipped just put the weapon in our reserves
            if (weaponLoadout.equippedWeapon == null)
            {
                animatorLayerWeightManager.SetLayerWeight(weapon.animationClass, 1);
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
                weaponLoadout.AddWeapon(weapon);
            }
        }

        [ServerRpc]
        void EquipWeaponServerRpc(int weaponIndex, ulong networkObjectId)
        {
            if (!IsHost)
            {
                GameObject weaponGO = Instantiate(ClientManager.Singleton.weaponPrefabOptions[weaponIndex].gameObject);
                Weapon weapon = weaponGO.GetComponent<NetworkedWeapon>().GenerateLocalInstance();
                weapon.transform.position = transform.position + transform.forward + transform.up;
                StartCoroutine(EquipAfter1Frame(weapon));
            }
            
            NetworkManager.SpawnManager.SpawnedObjects[networkObjectId].Despawn(true);
            EquipWeaponClientRpc(weaponIndex);
        }

        [ClientRpc]
        void EquipWeaponClientRpc(int weaponIndex)
        {
            GameObject weaponGO = Instantiate(ClientManager.Singleton.weaponPrefabOptions[weaponIndex].gameObject);
            Weapon weapon = weaponGO.GetComponent<NetworkedWeapon>().GenerateLocalInstance();
            weapon.transform.position = transform.position + transform.forward + transform.up;
            StartCoroutine(EquipAfter1Frame(weapon));
        }

        [Header("Weapon Drop Settings")]
        public Vector3 dropForce;
        void OnDrop()
        {
            if (weaponLoadout.equippedWeapon)
            {
                for (int i = 0; i < ClientManager.Singleton.weaponPrefabOptions.Length; i++)
                {
                    if (weaponLoadout.equippedWeapon.weaponName == ClientManager.Singleton.weaponPrefabOptions[i].weaponName)
                    {
                        DropWeaponServerRpc(i);
                        break;
                    }
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void DropWeaponServerRpc(int weaponIndex)
        {
            GameObject droppedWeapon = Instantiate(ClientManager.Singleton.weaponPrefabOptions[weaponIndex].gameObject, weaponLoadout.equippedWeapon.transform.position + transform.forward, weaponLoadout.equippedWeapon.transform.rotation);
            droppedWeapon.GetComponent<NetworkObject>().Spawn();
            droppedWeapon.GetComponent<Rigidbody>().AddForce(droppedWeapon.transform.rotation * dropForce, ForceMode.VelocityChange);

            animatorLayerWeightManager.SetLayerWeight(weaponLoadout.equippedWeapon.animationClass, 0);
            Destroy(weaponLoadout.equippedWeapon.gameObject);
            weaponLoadout.RemoveEquippedWeapon();
            DisableCombatIKs();

            DropWeaponClientRpc();
        }

        [ClientRpc]
        void DropWeaponClientRpc()
        {
            if (IsHost) { return; }

            animatorLayerWeightManager.SetLayerWeight(weaponLoadout.equippedWeapon.animationClass, 0);
            Destroy(weaponLoadout.equippedWeapon.gameObject);
            weaponLoadout.RemoveEquippedWeapon();
            DisableCombatIKs();
        }

        public NetworkVariable<bool> reloading = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        bool attack1;

        public void Attack1(bool pressed)
        {
            if (weaponLoadout.equippedWeapon == null) { return; }
            NetworkObject netObj = weaponLoadout.equippedWeapon.Attack1(pressed);
            if (netObj)
                netObj.Spawn(true);
        }

        void OnAttack1(InputValue value)
        {
            OnAttack1ServerRpc(value.isPressed);
        }

        [ServerRpc]
        void OnAttack1ServerRpc(bool pressed)
        {
            if (!IsHost)
            {
                if (IsOwner)
                    animator.SetBool("attack1", pressed);
                attack1 = pressed;
                Attack1(pressed);
            }
            
            OnAttack1ClientRpc(pressed);
        }

        [ClientRpc] void OnAttack1ClientRpc(bool pressed)
        {
            if (IsOwner)
                animator.SetBool("attack1", pressed);
            attack1 = pressed;
            Attack1(pressed);
        }

        [Header("Sword blocking")]
        public Transform blockConstraints;
        public float blockSpeed = 1;
        bool blocking;
        float oldRigSpeed;
        int oldCullingMask;
        public void Attack2(bool pressed)
        {
            if (weaponLoadout.equippedWeapon == null) // If we have no weapon active in our hands, activate fist combat
            {
                if (pressed) { return; }
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
                blocking = pressed;
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

        void OnAttack2(InputValue value)
        {
            Attack2(value.isPressed);
        }

        [ServerRpc]
        void OnAttack2ServerRpc(bool pressed)
        {
            if (!IsHost)
                Attack2(pressed);

            OnAttack2ClientRpc(pressed);
        }

        [ClientRpc] void OnAttack2ClientRpc(bool pressed) { Attack2(pressed); }

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
            OnReloadServerRpc();
        }

        [ServerRpc]
        void OnReloadServerRpc()
        {
            if (!IsHost)
                StartCoroutine(weaponLoadout.equippedWeapon.Reload(false));
            OnReloadClientRpc();
        }

        [ClientRpc]
        void OnReloadClientRpc()
        {
            StartCoroutine(weaponLoadout.equippedWeapon.Reload(true));
        }

        bool rpcSending;
        void OnQueryWeaponSlot(InputValue value)
        {
            if (reloading.Value) { return; }
            if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Draw/Stow Weapon")).IsName("Empty")) { return; }
            if (animator.IsInTransition(animator.GetLayerIndex("Draw/Stow Weapon"))) { return; }
            if (rpcSending) { return; }
            int slot = Convert.ToInt32(value.Get()) - 1;
            Weapon chosenWeapon = weaponLoadout.GetWeapon(slot);
            if (chosenWeapon == null) { return; }

            if (weaponLoadout.equippedWeapon == chosenWeapon)
                OnQueryWeaponSlotServerRpc(slot, "stow");
            else if (weaponLoadout.equippedWeapon != null)
                OnQueryWeaponSlotServerRpc(slot, "switch");
            else
                OnQueryWeaponSlotServerRpc(slot, "draw");

            if (!IsHost) // For some reason the host doesn't work with the rpc sending bool
                rpcSending = true;
        }

        [ServerRpc]
        void OnQueryWeaponSlotServerRpc(int slot, string actionType)
        {
            if (!IsHost)
            {
                if (actionType == "stow")
                    targetWeaponSlot = -1;
                else
                    targetWeaponSlot = slot;

                if (actionType == "stow")
                    StartCoroutine(StowWeapon(false));
                else if (actionType == "switch")
                    StartCoroutine(SwitchWeapon(slot, false));
                else if (actionType == "draw")
                    StartCoroutine(DrawWeapon(slot, false));
            }
            OnQueryWeaponSlotClientRpc(slot, actionType);
        }

        [ClientRpc]
        void OnQueryWeaponSlotClientRpc(int slot, string actionType)
        {
            if (actionType == "stow")
                targetWeaponSlot = -1;
            else
                targetWeaponSlot = slot;

            if (actionType == "stow")
                StartCoroutine(StowWeapon(true));
            else if (actionType == "switch")
                StartCoroutine(SwitchWeapon(slot, true));
            else if (actionType == "draw")
                StartCoroutine(DrawWeapon(slot, true));
            if (IsOwner)
                rpcSending = false;
        }

        int targetWeaponSlot;
        bool weaponChangeRunning;
        private IEnumerator StowWeapon(bool animate)
        {
            yield return new WaitUntil(() => !weaponChangeRunning);
            if (targetWeaponSlot != -1) { yield break; }

            weaponChangeRunning = true;
            Weapon equippedWeapon = weaponLoadout.equippedWeapon;
            equippedWeapon.disableAttack = true;
            if (animate)
            {
                leftHandTarget.lerp = true;
                animator.SetFloat("drawSpeed", equippedWeapon.drawSpeed);
            }

            DisableCombatIKs();

            if (animate)
            {
                animator.SetBool("stow" + equippedWeapon.animationClass, true);
                yield return null;
                animator.SetBool("stow" + equippedWeapon.animationClass, false);

                // Parent weapon to move with right hand
                ReparentWeapon(equippedWeapon, "transition");

                // Wait until stow animation has finished playing
                int animLayerIndex = animator.GetLayerIndex("Draw/Stow Weapon");
                yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("StowWeapon"));
                yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));
            }

            // Change to stowed mode
            animatorLayerWeightManager.SetLayerWeight(equippedWeapon.animationClass, 0);
            ReparentWeapon(equippedWeapon, "stowed");
            weaponLoadout.StowWeapon();
            equippedWeapon.disableAttack = false;

            if (animate)
                yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Draw/Stow Weapon")).IsName("Empty") & !animator.IsInTransition(animator.GetLayerIndex("Draw/Stow Weapon")));

            leftHandTarget.lerp = false;
            weaponChangeRunning = false;
        }

        private IEnumerator DrawWeapon(int slotIndex, bool animate)
        {
            yield return new WaitUntil(() => !weaponChangeRunning);
            if (targetWeaponSlot != slotIndex) { yield break; }

            weaponChangeRunning = true;

            Weapon chosenWeapon = weaponLoadout.GetWeapon(slotIndex);
            if (animate)
            {
                leftHandTarget.lerp = true;
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
            }

            // Change to player mode
            animatorLayerWeightManager.SetLayerWeight(chosenWeapon.animationClass, 1);
            ReparentWeapon(chosenWeapon, "player");
            weaponLoadout.DrawWeapon(slotIndex);
            weaponLoadout.equippedWeapon.Attack1(attack1);
            EnableCombatIKs();

            if (animate)
                yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Draw/Stow Weapon")).IsName("Empty") & !animator.IsInTransition(animator.GetLayerIndex("Draw/Stow Weapon")));

            leftHandTarget.lerp = false;
            weaponChangeRunning = false;
        }

        private IEnumerator SwitchWeapon(int slotIndex, bool animate)
        {
            yield return new WaitUntil(() => !weaponChangeRunning);
            if (targetWeaponSlot != slotIndex) { yield break; }

            weaponChangeRunning = true;
            // Stow equipped weapon
            Weapon equippedWeapon = weaponLoadout.equippedWeapon;
            equippedWeapon.disableAttack = true;

            if (animate)
            {
                leftHandTarget.lerp = true;
                animator.SetFloat("drawSpeed", equippedWeapon.drawSpeed);
            }
            
            DisableCombatIKs();

            int animLayerIndex = animator.GetLayerIndex("Draw/Stow Weapon");
            if (animate)
            {
                animator.SetBool("stow" + equippedWeapon.animationClass, true);
                yield return null;
                animator.SetBool("stow" + equippedWeapon.animationClass, false);

                // Parent weapon to move with right hand
                ReparentWeapon(equippedWeapon, "transition");

                // Wait until stow animation has started playing
                yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("StowWeapon"));
            }

            // Start drawing next weapon once stow animation has finished playing
            Weapon chosenWeapon = weaponLoadout.GetWeapon(slotIndex);

            if (animate)
            {
                animator.SetBool("draw" + chosenWeapon.animationClass, true);
                yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));
                animator.SetFloat("drawSpeed", chosenWeapon.drawSpeed);
                animator.SetBool("draw" + chosenWeapon.animationClass, false);
            }

            // Change to stowed mode
            animatorLayerWeightManager.SetLayerWeight(equippedWeapon.animationClass, 0);
            ReparentWeapon(equippedWeapon, "stowed");
            weaponLoadout.StowWeapon();
            equippedWeapon.disableAttack = false;

            if (animate)
            {
                yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animLayerIndex).IsTag("ToCombat"));

                // Parent weapon to move with right hand
                ReparentWeapon(chosenWeapon, "transition");

                yield return new WaitUntil(() => animator.IsInTransition(animLayerIndex));
            }

            // Change to player mode
            animatorLayerWeightManager.SetLayerWeight(chosenWeapon.animationClass, 1);
            Transform gripPoint = GetGripPoint(chosenWeapon);
            ReparentWeapon(chosenWeapon, "player");
            weaponLoadout.DrawWeapon(slotIndex);
            weaponLoadout.equippedWeapon.Attack1(attack1);
            EnableCombatIKs();

            if (animate)
                yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Draw/Stow Weapon")).IsName("Empty") & !animator.IsInTransition(animator.GetLayerIndex("Draw/Stow Weapon")));

            leftHandTarget.lerp = false;
            weaponChangeRunning = false;
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

            if (weaponLoadout.equippedWeapon.TryGetComponent(out Rifle rifleComponent))
            {
                leftHandTarget.target = weaponLoadout.equippedWeapon.leftHandGrip;
                rightHandTarget.target = weaponLoadout.equippedWeapon.rightHandGrip;
                leftArmRig.weightTarget = 1;
                rightArmRig.weightTarget = 1;
                spineAimRig.weightTarget = 1;

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
            else if (weaponLoadout.equippedWeapon.TryGetComponent(out GreatSword greatSword))
            {
                leftHandHint.offset = new Vector3(-1, -1, 0);
                leftArmRig.weightTarget = 1;
                leftHandTarget.target = weaponLoadout.equippedWeapon.leftHandGrip;
                Transform leftFingers = greatSword.leftFingersGrips;
                for (int i = 0; i < rightFingerIKs.Length; i++)
                {
                    leftFingerIKs[i].target = leftFingers.GetChild(i);
                }
                leftFingerRig.weightTarget = 1;

                if (playerController)
                    playerController.playerHUD.lookAngleDisplay.gameObject.SetActive(true);
            }
            else if (weaponLoadout.equippedWeapon.TryGetComponent(out Pistol pistolComponent))
            {
                leftHandTarget.target = weaponLoadout.equippedWeapon.leftHandGrip;
                rightHandTarget.target = weaponLoadout.equippedWeapon.rightHandGrip;
                leftArmRig.weightTarget = 1;
                rightArmRig.weightTarget = 1;
                spineAimRig.weightTarget = 1;

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
            if (animator.GetBool("dead")) { return; }
            animator.SetBool("dead", true);
            OnDrop();

            Flag flag = GetComponentInChildren<Flag>();
            Vector3 flagPosition = Vector3.zero;
            if (flag)
            {
                flagPosition = flag.transform.position;
                flag.transform.SetParent(null, true);
                Rigidbody rb = flag.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }

            DeathClientRpc(flagPosition);
        }

        [ClientRpc]
        void DeathClientRpc(Vector3 flagPosition)
        {
            animator.SetBool("dead", true);

            Flag flag = GetComponentInChildren<Flag>();
            if (flag)
            {
                flag.transform.position = flagPosition;
                flag.transform.SetParent(null, true);
                Rigidbody rb = flag.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }
        }

        void OnAttacked(OnAttackedData data)
        {
            animator.SetFloat("damageAngle", data.damageAngle);
            StartCoroutine(Utilities.ResetAnimatorBoolAfter1Frame(animator, "reactDamage"));
        }
    }
}
