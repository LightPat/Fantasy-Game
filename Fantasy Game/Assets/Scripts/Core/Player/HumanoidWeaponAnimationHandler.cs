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
            attack1.OnValueChanged += OnAttack1Change;
            attack2.OnValueChanged += OnAttack2Change;
            reloading.OnValueChanged += OnReloadChange;
            targetWeaponSlot.OnValueChanged += OnTargetWeaponSlotChange;

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

        public override void OnNetworkDespawn()
        {
            attack1.OnValueChanged -= OnAttack1Change;
            attack2.OnValueChanged -= OnAttack2Change;
            reloading.OnValueChanged -= OnReloadChange;
            targetWeaponSlot.OnValueChanged -= OnTargetWeaponSlotChange;
        }

        bool equipInitialWeaponsRunning = true;
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
                    Destroy(g.GetComponent<CustomNetworkTransform>());
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
                    Destroy(g.GetComponent<CustomNetworkTransform>());
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
            equipInitialWeaponsRunning = false;
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

        void OnTargetWeaponSlotChange(int previous, int current) { waitForWeaponSlotChange = false; }

        bool waitForWeaponSlotChange;
        IEnumerator EquipAfter1Frame(Weapon weapon)
        {
            yield return null;
            // If we already have a weapon equipped just put the weapon in our reserves
            if (weaponLoadout.equippedWeapon == null)
            {
                waitForWeaponSlotChange = true;
                animatorLayerWeightManager.SetLayerWeight(weapon.animationClass, 1);
                Destroy(weapon.GetComponent<Rigidbody>());
                ReparentWeapon(weapon, "player");
                int slot = weaponLoadout.AddWeapon(weapon);
                if (IsServer)
                    targetWeaponSlot.Value = slot;
                weaponLoadout.DrawWeapon(slot);
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
            targetWeaponSlot.Value = -1;
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

        private NetworkVariable<bool> attack1 = new NetworkVariable<bool>();

        void OnAttack1Change(bool previous, bool current) { animator.SetBool("attack1", current); }

        public void Attack1(bool pressed)
        {
            if (weaponLoadout.equippedWeapon == null) { return; }
            NetworkObject netObj = weaponLoadout.equippedWeapon.Attack1(pressed);
            if (netObj)
                netObj.SpawnWithOwnership(OwnerClientId, true);
        }

        void OnAttack1(InputValue value)
        {
            Attack1ServerRpc(value.isPressed);
        }

        [ServerRpc]
        void Attack1ServerRpc(bool pressed)
        {
            attack1.Value = pressed;
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

        private NetworkVariable<bool> attack2 = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        void OnAttack2Change(bool previous, bool current) { Attack2(current); }

        void OnAttack2(InputValue value)
        {
            attack2.Value = value.isPressed;
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

        public NetworkVariable<bool> reloading { get; private set; } = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        void OnReloadChange(bool previous, bool current)
        {
            if (!current) { return; }
            if (weaponLoadout.equippedWeapon == null) { return; }
            if (weaponLoadout.equippedWeapon.GetComponent<GreatSword>())
            {
                blockConstraints.GetComponent<SwordBlockingIKSolver>().ResetRotation();
                if (IsOwner)
                    reloading.Value = false;
            }
            else
            {
                StartCoroutine(weaponLoadout.equippedWeapon.Reload(IsClient));
            }
        }

        void OnReload()
        {
            reloading.Value = true;
        }

        void OnQueryWeaponSlot(InputValue value)
        {
            if (reloading.Value) { return; }
            if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Draw/Stow Weapon")).IsName("Empty")) { return; }
            if (animator.IsInTransition(animator.GetLayerIndex("Draw/Stow Weapon"))) { return; }
            int slot = Convert.ToInt32(value.Get()) - 1;
            Weapon chosenWeapon = weaponLoadout.GetWeapon(slot);
            if (chosenWeapon == null) { return; }

            if (weaponLoadout.equippedWeapon == chosenWeapon)
                OnQueryWeaponSlotServerRpc(slot, "stow");
            else if (weaponLoadout.equippedWeapon != null)
                OnQueryWeaponSlotServerRpc(slot, "switch");
            else
                OnQueryWeaponSlotServerRpc(slot, "draw");
        }

        [ServerRpc]
        void OnQueryWeaponSlotServerRpc(int slot, string actionType)
        {
            if (actionType == "stow")
            {
                targetWeaponSlot.Value = -1;
            }
            else if (actionType == "switch")
            {
                targetWeaponSlot.Value = slot;
            }
            else if (actionType == "draw")
            {
                targetWeaponSlot.Value = slot;
            }
        }

        private void Update()
        {
            Attack1(attack1.Value);

            if (waitForWeaponSlotChange) { return; }
            if (equipInitialWeaponsRunning) { return; }
            if (weaponChangeRunning) { return; }
            if (reloading.Value) { return; }
            if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Draw/Stow Weapon")).IsName("Empty")) { return; }
            if (animator.IsInTransition(animator.GetLayerIndex("Draw/Stow Weapon"))) { return; }

            if (weaponLoadout.GetEquippedWeaponIndex() != targetWeaponSlot.Value)
            {
                if (weaponLoadout.equippedWeapon & targetWeaponSlot.Value == -1)
                {
                    weaponChangeRunning = true;
                    StartCoroutine(StowWeapon(IsClient));
                }
                else if (!weaponLoadout.equippedWeapon & targetWeaponSlot.Value != -1)
                {
                    weaponChangeRunning = true;
                    StartCoroutine(DrawWeapon(targetWeaponSlot.Value, IsClient));
                }
                else if (weaponLoadout.equippedWeapon & targetWeaponSlot.Value != -1)
                {
                    weaponChangeRunning = true;
                    StartCoroutine(SwitchWeapon(IsClient));
                }
            }
        }

        private NetworkVariable<int> targetWeaponSlot = new NetworkVariable<int>();
        bool weaponChangeRunning;
        private IEnumerator StowWeapon(bool animate)
        {
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
            weaponLoadout.equippedWeapon.Attack1(attack1.Value);
            EnableCombatIKs();

            if (animate)
                yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Draw/Stow Weapon")).IsName("Empty") & !animator.IsInTransition(animator.GetLayerIndex("Draw/Stow Weapon")));

            leftHandTarget.lerp = false;
            weaponChangeRunning = false;
        }

        private IEnumerator SwitchWeapon(bool animate)
        {
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
            int slotIndex = targetWeaponSlot.Value;
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
            weaponLoadout.equippedWeapon.Attack1(attack1.Value);
            EnableCombatIKs();

            if (animate)
                yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Draw/Stow Weapon")).IsName("Empty") & !animator.IsInTransition(animator.GetLayerIndex("Draw/Stow Weapon")));

            leftHandTarget.lerp = false;
            weaponChangeRunning = false;
        }

        private List<Attributes> swingHits = new List<Attributes>();
        public void ClearSwingHits()
        {
            swingHits.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            // For inflicting damage using collision weapons (swords)
            if (weaponLoadout.equippedWeapon == null) { return; }
            GreatSword sword = weaponLoadout.equippedWeapon.GetComponent<GreatSword>();
            if (!sword) { return; }
            if (!sword.swinging) { return; }

            Attributes hit = other.GetComponentInParent<Attributes>();

            if (other.GetComponentInParent<Sliceable>())
                sword.SliceStart();
            else if (hit)
            {
                if (!swingHits.Contains(hit))
                {
                    swingHits.Add(other.GetComponentInParent<Attributes>());
                    InflictDamageServerRpc(other.GetComponentInParent<Attributes>().NetworkObjectId, weaponLoadout.equippedWeapon.baseDamage);
                }
            }
            else
                sword.StopSwing();
        }

        [ServerRpc]
        private void InflictDamageServerRpc(ulong inflictedNetworkObjectId, float damage)
        {
            bool damageSuccess = NetworkManager.SpawnManager.SpawnedObjects[inflictedNetworkObjectId].GetComponent<Attributes>().InflictDamage(damage, gameObject);

            //if (inflicter.TryGetComponent(out NetworkObject playerNetObj) & damageSuccess)
            //{
            //    if (playerNetObj.IsPlayerObject)
            //        inflicter.SendMessage("PlayHitmarker", new HitmarkerData(Array.IndexOf(AudioManager.Singleton.networkAudioClips, hitmarkerSound), hitmarkerVolume, hitmarkerTime, playerNetObj.OwnerClientId));
            //}
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<Sliceable>())
                weaponLoadout.equippedWeapon.GetComponent<GreatSword>().SliceEnd(other);
        }

        public float maxMantleHeight = 1;
        public float minTranslateDistance = 0.13f;
        private void OnCollisionStay(Collision collision)
        {
            if (collision.collider.isTrigger) { return; }
            if (collision.collider.bounds.size.magnitude < 0.2f) { return; } // If the collider we are hitting is too small, this is used to filter out shells from guns

            if (Mathf.Abs(animator.GetFloat("moveInputX")) > 0.5f | Mathf.Abs(animator.GetFloat("moveInputY")) > 0.5f)
            {
                float[] yPos = new float[collision.contactCount];
                for (int i = 0; i < collision.contactCount; i++)
                {
                    yPos[i] = collision.GetContact(i).point.y;
                }
                float translateDistance = yPos.Max() - transform.position.y;
                if (translateDistance < minTranslateDistance) { return; } // If the wall is beneath us
                if (collision.collider.bounds.max.y - transform.position.y > maxMantleHeight) { return; } // If the wall is too high to mantle
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
            targetWeaponSlot.Value = -1;
            //OnDrop();

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
            OnAttackedClientRpc(data);
        }

        [ClientRpc]
        void OnAttackedClientRpc(OnAttackedData data)
        {
            animator.SetFloat("damageAngle", data.damageAngle);
            StartCoroutine(Utilities.ResetAnimatorBoolAfter1Frame(animator, "reactDamage"));
        }
    }
}
