using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using LightPat.ProceduralAnimations;
using Unity.Netcode;
using System.Linq;

namespace LightPat.Core.Player
{
    public class PlayerController : NetworkBehaviour
    {
        public GameObject worldSpaceLabel;
        public PlayerCameraFollow playerCamera { get; private set; }
        [Header("Animation Settings")]
        public float moveTransitionSpeed;

        [HideInInspector] public PlayerHUD playerHUD;

        Animator animator;
        Rigidbody rb;

        public void TurnOnDisplayModelMode()
        {
            GetComponent<PlayerInput>().enabled = false;
            Destroy(playerCamera.GetComponent<AudioListener>());
            playerCamera.neckAimRig.instantWeight = true;
            playerCamera.neckAimRig.weightTarget = 0;
            playerCamera.gameObject.SetActive(false);
            playerHUD.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            animator.SetFloat("idleTime", 50001);
        }

        public override void OnNetworkSpawn()
        {
            lookAngle.OnValueChanged += OnLookAngleChange;
            running.OnValueChanged += OnRunningChange;
            sprinting.OnValueChanged += OnSprintingChange;
            crouching.OnValueChanged += OnCrouchingChange;
            targetLean.OnValueChanged += OnTargetLeanChange;
            sitting.OnValueChanged += OnSittingChange;

            name = ClientManager.Singleton.GetClient(OwnerClientId).clientName;

            if (IsOwner)
            {
                GetComponent<PlayerInput>().enabled = true;
                GetComponent<ActionMapHandler>().enabled = true;
                playerCamera.GetComponent<AudioListener>().enabled = true;
                playerCamera.tag = "MainCamera";
                Destroy(worldSpaceLabel);
            }
            else
            {
                GetComponent<PlayerInput>().enabled = false;
                GetComponent<ActionMapHandler>().enabled = false;
                Destroy(playerCamera.GetComponent<AudioListener>());
                playerCamera.GetComponent<Camera>().depth = -1;
                playerCamera.GetComponent<Camera>().enabled = false;
                playerHUD.gameObject.SetActive(false);
            }

            MaterialColorChange colors = gameObject.AddComponent<MaterialColorChange>();
            colors.materialColors = ClientManager.Singleton.GetClient(OwnerClientId).colors;
            colors.Apply();
        }

        public override void OnNetworkDespawn()
        {
            lookAngle.OnValueChanged -= OnLookAngleChange;
            running.OnValueChanged -= OnRunningChange;
            sprinting.OnValueChanged -= OnSprintingChange;
            crouching.OnValueChanged -= OnCrouchingChange;
            targetLean.OnValueChanged -= OnTargetLeanChange;
            sitting.OnValueChanged -= OnSittingChange;
        }

        Vehicle vehicle;
        void OnDriverEnter(Vehicle newVehicle) { vehicle = newVehicle; }

        void OnDriverExit() { vehicle = null; }

        private NetworkVariable<bool> sitting = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        void OnSittingChange(bool previous, bool current) { animator.SetBool("sitting", current); }

        [Header("Sitting In Chairs")]
        public TwoBoneIKConstraint leftLegIK;
        public TwoBoneIKConstraint rightLegIK;
        bool disableNoPhysics;
        Vector4 originalLookLimit;
        void OnChairEnter(Chair newChair)
        {
            disableNoPhysics = true;
            StartCoroutine(WaitForParentOnChairEnter());
            GetComponent<CustomNetworkTransform>().SetParent(newChair.NetworkObject);
            chair = newChair;

            if (rb)
                Destroy(rb);
            
            sitting.Value = true;

            if (chair.leftFootGrip)
            {
                leftLegIK.GetComponentInChildren<FollowTarget>().target = chair.leftFootGrip;
                leftLegIK.GetComponentInParent<Rig>().weight = 1;
            }

            if (chair.rightFootGrip)
            {
                rightLegIK.GetComponentInChildren<FollowTarget>().target = chair.rightFootGrip;
                rightLegIK.GetComponentInParent<Rig>().weight = 1;
            }

            originalLookLimit = rotateWithBoneLookLimit;
            rotateWithBoneLookLimit = chair.occupantLookLimits;
        }

        private IEnumerator WaitForParentOnChairEnter()
        {
            yield return new WaitUntil(() => chair != null);
            yield return new WaitUntil(() => transform.parent == chair.transform);
            disableNoPhysics = false;
        }

        void OnChairExit()
        {
            disableNoPhysics = true;
            StartCoroutine(WaitForParentOnChairExit());
            transform.Translate(transform.rotation * chair.exitPosOffset, Space.World);

            transform.rotation.ToAngleAxis(out float angle, out Vector3 axis);
            bodyRotation = new Vector3(0, angle * axis.y, 0);

            GetComponent<CustomNetworkTransform>().SetParent(null);

            rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            sitting.Value = false;

            leftLegIK.GetComponentInParent<Rig>().weight = 0;
            rightLegIK.GetComponentInParent<Rig>().weight = 0;

            leftLegIK.GetComponentInChildren<FollowTarget>().target = leftLegIK.data.tip;
            rightLegIK.GetComponentInChildren<FollowTarget>().target = rightLegIK.data.tip;

            rotateWithBoneLookLimit = originalLookLimit;
            chair = null;
        }

        private IEnumerator WaitForParentOnChairExit()
        {
            yield return new WaitUntil(() => !transform.parent);
            yield return new WaitForSeconds(0.1f);
            disableNoPhysics = false;
        }

        private void OnTransformParentChanged()
        {
            playerCamera.RefreshCameraParent();
        }

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            rb = GetComponent<Rigidbody>();
            playerCamera = GetComponentInChildren<PlayerCameraFollow>();
            prevCamRotState = !rotateBodyWithCamera;
            playerHUD = GetComponentInChildren<PlayerHUD>();
            // Change bodyRotation to be the spawn rotation
            bodyRotation = new Vector3(0, transform.eulerAngles.y, 0);
            currentBodyRotSpeed = bodyRotationSpeed;
            camConstraint = playerCamera.neckAimRig.GetComponentInChildren<MultiRotationConstraint>();
        }

        private NetworkVariable<Vector2> moveInput = new NetworkVariable<Vector2>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        void OnMove(InputValue value)
        {
            moveInput.Value = value.Get<Vector2>();
            if (vehicle) { vehicle.SendMessage("OnVehicleMove", moveInput.Value); }
            if (moveInput.Value.y <= 0 & running.Value) { runTarget.Value = 2; }
        }

        [Header("Mouse Look Settings")]
        public Vector4 rotateWithBoneLookLimit;
        public float sensitivity;
        public float bodyRotationSpeed;
        public float mouseUpXRotLimit;
        public float mouseDownXRotLimit;
        public bool disableLookInput;
        public bool rotateBodyWithCamera { get; private set; } = true;
        public float attemptedXAngle { get; private set; }
        public Vector3 bodyRotation { get; private set; }
        Vector2 lookInput;
        private NetworkVariable<float> lookAngle = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        float prevLookAngle;
        float currentBodyRotSpeed;
        Vector3 rotateWithBoneRotOffset;
        MultiRotationConstraint camConstraint;
        void OnLook(InputValue value)
        {
            Look(value.Get<Vector2>(), sensitivity, Time.timeScale);
        }

        void OnLookAngleChange(float previous, float current) { animator.SetFloat("lookAngle", lookAngle.Value); }

        public void Look(Vector2 lookValue, float sensitivity = 1, float timeScale = 1, bool instantBoneRot = false)
        {
            lookInput = lookValue;

            // Look angle animator logic for sword swings
            if (lookInput != Vector2.zero)
            {
                if (!animator.GetBool("attack2"))
                {
                    lookAngle.Value = Mathf.Atan2(lookInput.x, lookInput.y) * Mathf.Rad2Deg;
                }

                if (lookAngle.Value == 0)
                {
                    if (prevLookAngle > 0)
                    {
                        lookAngle.Value = 1;
                    }
                    else if (prevLookAngle < 0)
                    {
                        lookAngle.Value = -1;
                    }
                }

                if (prevLookAngle < 0 & lookAngle.Value == 180)
                {
                    lookAngle.Value *= -1;
                }

                prevLookAngle = lookAngle.Value;
            }

            lookInput *= sensitivity * timeScale;

            if (playerCamera.updateRotationWithTarget)
            {
                if (!instantBoneRot)
                {
                    rotateWithBoneRotOffset += new Vector3(-lookInput.y / 2, lookInput.x / 2, 0);
                    rotateWithBoneRotOffset.x = Mathf.Clamp(rotateWithBoneRotOffset.x, rotateWithBoneLookLimit.y, rotateWithBoneLookLimit.x);
                    rotateWithBoneRotOffset.y = Mathf.Clamp(rotateWithBoneRotOffset.y, rotateWithBoneLookLimit.w, rotateWithBoneLookLimit.z);
                }
                else
                {
                    rotateWithBoneRotOffset += new Vector3(-lookInput.y * 2, lookInput.x * 2, 0);
                    rotateWithBoneRotOffset.x = Mathf.Clamp(rotateWithBoneRotOffset.x, rotateWithBoneLookLimit.y, rotateWithBoneLookLimit.x);
                    rotateWithBoneRotOffset.y = Mathf.Clamp(rotateWithBoneRotOffset.y, rotateWithBoneLookLimit.w, rotateWithBoneLookLimit.z);
                    camConstraint.data.offset = rotateWithBoneRotOffset;
                }
            }

            if (vehicle)
            {
                vehicle.SendMessage("OnVehicleLook", lookInput);
                return;
            }

            if (disableLookInput) { return; }

            // Body Rotation Logic (Rotation Around Y Axis)
            bodyRotation = new Vector3(0, bodyRotation.y + lookInput.x, 0);
            if (rotateBodyWithCamera)
            {
                if (rb)
                    rb.MoveRotation(Quaternion.Euler(bodyRotation));
                else if (playerCamera.updateRotationWithTarget)
                    transform.rotation = Quaternion.Euler(bodyRotation);
                else if (transform.parent)
                    transform.localRotation = Quaternion.Euler(bodyRotation);
            }

            // Camera Rotation Logic (Rotation Around X Axis)
            Transform camTransform = playerCamera.transform;

            // When leaning
            if (playerCamera.targetZRot != 0)
            {
                float xAngle = Vector3.Angle(transform.forward, camTransform.forward);
                if (camTransform.forward.y > 0)
                    xAngle *= -1;
                float zRot = Mathf.Abs(playerCamera.targetZRot);
                if (xAngle > mouseDownXRotLimit - zRot & lookInput.y < 0)
                    return;
                else if (xAngle < mouseUpXRotLimit + zRot & lookInput.y > 0)
                    return;
            }

            // When not leaning
            camTransform.Rotate(new Vector3(-lookInput.y, 0, 0), Space.Self);
            if (!rotateBodyWithCamera)
            {
                camTransform.localEulerAngles = new Vector3(camTransform.localEulerAngles.x, camTransform.localEulerAngles.y + lookInput.x, camTransform.localEulerAngles.z);
                attemptedXAngle = Vector3.SignedAngle(Quaternion.Euler(bodyRotation) * Vector3.forward, camTransform.forward, transform.right);

                if (attemptedXAngle > mouseDownXRotLimit)
                {
                    camTransform.localEulerAngles = new Vector3(mouseDownXRotLimit, bodyRotation.y, 0);
                    attemptedXAngle = mouseDownXRotLimit;
                }
                else if (attemptedXAngle < mouseUpXRotLimit)
                {
                    camTransform.localEulerAngles = new Vector3(mouseUpXRotLimit, bodyRotation.y, 0);
                    attemptedXAngle = mouseUpXRotLimit;
                }
            }
            else // When you have a weapon out
            {
                attemptedXAngle = Vector3.SignedAngle(transform.forward, camTransform.forward, transform.right);

                if (attemptedXAngle > mouseDownXRotLimit)
                {
                    camTransform.localEulerAngles = new Vector3(mouseDownXRotLimit, 0, 0);
                    attemptedXAngle = mouseDownXRotLimit;
                }
                else if (attemptedXAngle < mouseUpXRotLimit)
                {
                    camTransform.localEulerAngles = new Vector3(mouseUpXRotLimit, 0, 0);
                    attemptedXAngle = mouseUpXRotLimit;
                }
            }
        }

        bool prevCamRotState;
        private void Update()
        {
            if (chair)
            {
                if (IsOwner)
                {
                    transform.localPosition = chair.occupantPosition;
                    transform.rotation = chair.transform.rotation * Quaternion.Euler(chair.occupantRotation);
                }
            }

            playerHUD.lookAngleDisplay.rotation = Quaternion.Slerp(playerHUD.lookAngleDisplay.rotation, Quaternion.Euler(new Vector3(0, 0, -lookAngle.Value)), playerHUD.lookAngleRotSpeed * Time.deltaTime);

            float xTarget = moveInput.Value.x;
            if (animator.GetBool("mirrorIdle"))
                xTarget *= -1;
            if (running.Value) { xTarget *= runTarget.Value; }
            float x = Mathf.Lerp(animator.GetFloat("moveInputX"), xTarget, Time.deltaTime * moveTransitionSpeed);
            animator.SetFloat("moveInputX", x);

            float yTarget = moveInput.Value.y;
            if (running.Value) { yTarget *= runTarget.Value; }
            float y = Mathf.Lerp(animator.GetFloat("moveInputY"), yTarget, Time.deltaTime * moveTransitionSpeed);
            animator.SetFloat("moveInputY", y);

            if (moveInput.Value == Vector2.zero)
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) // Only change idle time if we are at rest
                    // This is used so that some states that don't have exit transitions can "remember" that the user moved during their playtime, also so that crouching and jumping is not considered "idle"
                    if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Idle Loop")).IsTag("PauseIdleTime"))
                        animator.SetFloat("idleTime", animator.GetFloat("idleTime") + Time.deltaTime);
                    else // If moveInput is not Vector2.zero
                        animator.SetFloat("idleTime", 0);
            // Don't want to enter idle loop while crouching
            if (crouching.Value)
                animator.SetFloat("idleTime", 0);
            // If we jump set idleTime to 0
            if (animator.GetBool("jumping"))
                animator.SetFloat("idleTime", 0);

            if (rotateBodyWithCamera != prevCamRotState)
                playerCamera.RefreshCameraParent();

            spineAim.data.offset = Vector3.Lerp(spineAim.data.offset, new Vector3(0, 0, targetLean.Value / spineAim.weight), leanSpeed * Time.deltaTime);
            foreach (MultiAimConstraint aimConstraint in aimConstraints)
            {
                aimConstraint.data.offset = Vector3.Lerp(aimConstraint.data.offset, new Vector3(0, 0, targetLean.Value), leanSpeed * Time.deltaTime);
            }

            if (playerCamera.updateRotationWithTarget)
            {
                if (chair)
                {
                    Vector3 targetRot = new Vector3(0, 0, rotateWithBoneRotOffset.z);
                    if (chair.rotateX)
                        targetRot.x = rotateWithBoneRotOffset.x;
                    if (chair.rotateY)
                        targetRot.y = rotateWithBoneRotOffset.y;

                    camConstraint.data.offset = Vector3.Lerp(camConstraint.data.offset, targetRot, 5 * Time.deltaTime);
                }
                else
                {
                    camConstraint.data.offset = Vector3.Lerp(camConstraint.data.offset, rotateWithBoneRotOffset, 5 * Time.deltaTime);
                }
            }
            else
            {
                rotateWithBoneRotOffset = Vector3.zero;
                camConstraint.data.offset = Vector3.Lerp(camConstraint.data.offset, rotateWithBoneRotOffset, 5 * Time.deltaTime);
            }

            prevCamRotState = rotateBodyWithCamera;
        }

        private void FixedUpdate()
        {
            if (!IsOwner) { return; }

            if (!rb & !chair)
            {
                Vector3 startPos = transform.position;
                RaycastHit[] allHits = Physics.RaycastAll(startPos, transform.up * -1, 2, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                Debug.DrawRay(startPos, transform.up * -1, Color.red, Time.fixedDeltaTime);
                System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
                foreach (RaycastHit hit in allHits)
                {
                    if (GetComponentsInChildren<Collider>().Contains(hit.collider)) { continue; }

                    if (!hit.collider.CompareTag("NoPhysics"))
                    {
                        if (Time.time - lastNoPhysicsTime < noPhysicsTimeThreshold) { return; }
                        lastNoPhysicsTime = Time.time;

                        transform.parent.rotation.ToAngleAxis(out float angle, out Vector3 axis);
                        bodyRotation = new Vector3(0, bodyRotation.y + angle * axis.y, 0);

                        GetComponent<CustomNetworkTransform>().SetParent(null);
                        rb = gameObject.AddComponent<Rigidbody>();
                        rb.constraints = RigidbodyConstraints.FreezeRotation;
                        rb.interpolation = RigidbodyInterpolation.Interpolate;
                        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    }
                    break;
                }

                // If we have no hits
                if (allHits.Length == 0)
                {
                    if (Time.time - lastNoPhysicsTime < noPhysicsTimeThreshold) { return; }
                    lastNoPhysicsTime = Time.time;

                    transform.parent.rotation.ToAngleAxis(out float angle, out Vector3 axis);
                    bodyRotation = new Vector3(0, bodyRotation.y + angle * axis.y, 0);

                    GetComponent<CustomNetworkTransform>().SetParent(null);
                    rb = gameObject.AddComponent<Rigidbody>();
                    rb.constraints = RigidbodyConstraints.FreezeRotation;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                }
            }
        }

        float noPhysicsTimeThreshold = 0.2f;
        float lastNoPhysicsTime;
        private void OnCollisionEnter(Collision collision)
        {
            if (!IsOwner) { return; }
            if (disableNoPhysics) { return; }

            if (collision.collider.CompareTag("NoPhysics") & transform.position.y > collision.GetContact(0).point.y)
            {
                if (rb)
                {
                    if (Time.time - lastNoPhysicsTime < noPhysicsTimeThreshold) { return; }
                    lastNoPhysicsTime = Time.time;

                    GetComponent<CustomNetworkTransform>().SetParent(collision.transform.GetComponent<NetworkObject>());
                    Destroy(rb);
                    collision.transform.rotation.ToAngleAxis(out float angle, out Vector3 axis);
                    bodyRotation = new Vector3(0, bodyRotation.y - angle * axis.y, 0);
                }
            }
        }

        void OnAbility()
        {
            if (Time.timeScale == 1)
            {
                Time.timeScale = 0.1f;
            }
            else
            {
                Time.timeScale = 1;
            }
        }

        [Header("Misc Settings")]
        public bool toggleSprint;
        private NetworkVariable<bool> running = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<float> runTarget = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        void OnSprint(InputValue value)
        {
            if (vehicle) { vehicle.SendMessage("OnVehicleSprint", value.isPressed); }

            if (toggleSprint)
            {
                if (value.isPressed)
                {
                    running.Value = !running.Value;
                    runTarget.Value = 2;
                    ascending = true;

                    if (!running.Value)
                        animator.SetBool("sprinting", false);
                }
            }
            else
            {
                running.Value = value.isPressed;
                runTarget.Value = 2;
                ascending = true;

                if (!value.isPressed)
                    animator.SetBool("sprinting", false);
            }
        }

        void OnRunningChange(bool previous, bool current) { animator.SetBool("running", current); }
        void OnSprintingChange(bool previous, bool current) { animator.SetBool("sprinting", current); }

        private NetworkVariable<bool> sprinting = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        bool ascending = true;
        void OnScaleSprint()
        {
            if (running.Value & !crouching.Value & moveInput.Value.y > 0)
            {
                if (ascending)
                {
                    runTarget.Value += 1;

                    if (runTarget.Value == 4)
                        sprinting.Value = true;
                    else
                        sprinting.Value = false;

                    if (runTarget.Value == 4)
                        ascending = false;
                }
                else
                {
                    runTarget.Value -= 1;
                    if (runTarget.Value == 2)
                        ascending = true;

                    if (runTarget.Value == 4)
                        sprinting.Value = true;
                    else
                        sprinting.Value = false;
                }
            }
        }

        public bool toggleCrouch;
        private NetworkVariable<bool> crouching = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        void OnCrouch(InputValue value)
        {
            if (vehicle) { vehicle.SendMessage("OnVehicleCrouch", value.isPressed); }

            if (running.Value)
            {
                crouching.Value = value.isPressed;
                return;
            }

            if (toggleCrouch)
            {
                if (value.isPressed)
                {
                    crouching.Value = !crouching.Value;
                }
            }
            else
            {
                crouching.Value = value.isPressed;
            }
        }

        void OnCrouchingChange(bool previous, bool current) { animator.SetBool("crouching", current); }

        [Header("Interact Settings")]
        public float reach;
        Chair chair;
        void OnInteract()
        {
            if (animator.GetBool("sitting"))
            {
                chair.ExitSittingServerRpc();
                return;
            }

            RaycastHit[] allHits = Physics.RaycastAll(playerCamera.transform.position, playerCamera.transform.forward, reach, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform == transform) { continue; }

                if (hit.transform.TryGetComponent(out Interactable interactable))
                {
                    interactable.Invoke(gameObject);
                }
                else if (hit.collider.TryGetComponent(out Door door))
                {
                    door.ToggleDoorServerRpc();
                }
                else if (hit.collider.TryGetComponent(out Chair chair))
                {
                    if (animator.GetBool("falling")) { return; }
                    chair.TrySittingServerRpc(NetworkObjectId);
                }
                break;
            }
        }

        void OnJump(InputValue value)
        {
            if (vehicle) { vehicle.SendMessage("OnVehicleJump", value.isPressed); }
            if (!value.isPressed) { return; }
        }

        [Header("Lean Settings")]
        public RigWeightTarget spineRig;
        public MultiAimConstraint spineAim;
        public MultiAimConstraint[] aimConstraints;
        public float rightLean;
        public float leftLean;
        public float leanSpeed;
        public bool disableLeanInput;
        private NetworkVariable<float> targetLean = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public void SetLean(float newTilt)
        {
            if (IsOwner)
                targetLean.Value = newTilt;
        }

        void OnTargetLeanChange(float previous, float current) { playerCamera.targetZRot = targetLean.Value * spineAim.weight; }

        void OnLeanRight()
        {
            if (disableLeanInput) { return; }
            if (spineRig.weightTarget != 1) { return; }
            else if (playerCamera.updateRotationWithTarget) { return; }

            if (targetLean.Value != rightLean)
            {
                // Check if we are aiming too low or too high to lean
                float xAngle = Vector3.Angle(transform.forward, playerCamera.transform.forward) + Mathf.Abs(rightLean);
                if (playerCamera.transform.forward.y > 0)
                    xAngle *= -1;
                if (xAngle > mouseDownXRotLimit)
                    return;
                else if (xAngle < mouseUpXRotLimit)
                    return;

                playerCamera.targetZRot = rightLean;
                targetLean.Value = rightLean;
            }
            else
            {
                targetLean.Value = 0;
                playerCamera.targetZRot = 0;
            }
        }

        void OnLeanLeft()
        {
            if (disableLeanInput) { return; }
            if (spineRig.weightTarget != 1) { return; }
            else if (playerCamera.updateRotationWithTarget) { return; }

            if (targetLean.Value != leftLean)
            {
                // Check if we are aiming too low or too high to lean
                float xAngle = Vector3.Angle(transform.forward, playerCamera.transform.forward) + Mathf.Abs(leftLean);
                if (playerCamera.transform.forward.y > 0)
                    xAngle *= -1;
                if (xAngle > mouseDownXRotLimit)
                    return;
                else if (xAngle < mouseUpXRotLimit)
                    return;

                targetLean.Value = leftLean;
                playerCamera.targetZRot = leftLean;
            }
            else
            {
                targetLean.Value = 0;
                playerCamera.targetZRot = 0;
            }
        }

        [SerializeField] private GameObject scoreboardPrefab;
        GameObject scoreboardInstance;
        void OnScoreboard(InputValue value)
        {
            if (value.isPressed)
            {
                scoreboardInstance = Instantiate(scoreboardPrefab);
            }
            else
            {
                Destroy(scoreboardInstance);
            }
        }

        void PlayHitmarker(HitmarkerData hitmarkerData)
        {
            if (IsLocalPlayer)
            {
                AudioManager.Singleton.PlayClipAtPoint(AudioManager.Singleton.networkAudioClips[hitmarkerData.hitmarkerSoundIndex], transform.position, hitmarkerData.hitmarkerVolume);
                StartCoroutine(playerHUD.ToggleHitMarker(hitmarkerData.hitmarkerTime));
            }
            else
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { hitmarkerData.targetClient }
                    }
                };
                HitmarkerClientRpc(hitmarkerData.hitmarkerSoundIndex, hitmarkerData.hitmarkerVolume, hitmarkerData.hitmarkerTime, clientRpcParams);
            }
        }

        [ClientRpc]
        void HitmarkerClientRpc(int hitmarkerSoundIndex, float hitmarkerVolume, float hitmarkerTime, ClientRpcParams clientRpcParams = default)
        {
            AudioManager.Singleton.PlayClipAtPoint(AudioManager.Singleton.networkAudioClips[hitmarkerSoundIndex], transform.position, hitmarkerVolume);
            StartCoroutine(playerHUD.ToggleHitMarker(hitmarkerTime));
        }

        public GameObject deathScreenPrefab;
        GameObject deathScreenInstance;
        bool respawning;
        void OnDeath()
        {
            if (!respawning)
            {
                respawning = true;
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { OwnerClientId }
                    }
                };
                RespawnClientRpc(clientRpcParams);
            }
        }

        [ClientRpc]
        void RespawnClientRpc(ClientRpcParams clientRpcParams = default)
        {
            deathScreenInstance = Instantiate(deathScreenPrefab);
            StartCoroutine(WaitForRespawnCountdown());
        }

        private IEnumerator WaitForRespawnCountdown()
        {
            yield return new WaitUntil(() => deathScreenInstance == null);

            foreach (TeamSpawnPoint teamSpawnPoint in FindObjectOfType<GameLogicManager>().spawnPoints)
            {
                if (teamSpawnPoint.team == GetComponent<Attributes>().team)
                {
                    transform.position = teamSpawnPoint.spawnPosition;
                    transform.rotation = Quaternion.Euler(teamSpawnPoint.spawnRotation);
                    break;
                }
            }
            RespawnServerRpc();
        }

        [ServerRpc]
        void RespawnServerRpc()
        {
            animator.SetBool("dead", false);
            GetComponent<Attributes>().OnNetworkSpawn();
            respawning = false;
            RespawnClientRpc();
        }

        [ClientRpc]
        void RespawnClientRpc()
        {
            animator.SetBool("dead", false);
        }

        void OnProjectileNear(Projectile projectile)
        {
            playerHUD.OnProjectileNear(projectile);
        }
    }
}
