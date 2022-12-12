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
        public Transform cameraParent;
        public Camera thirdPersonCamera;
        public PlayerCameraFollow playerCamera { get; private set; }
        [Header("Animation Settings")]
        public float moveTransitionSpeed;
        public float animatorSpeed = 1;

        [HideInInspector] public PlayerHUD playerHUD;

        Animator animator;
        Rigidbody rb;
        public Vehicle vehicle;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                GetComponent<PlayerInput>().enabled = true;
                GetComponent<ActionMapHandler>().enabled = true;
                playerCamera.tag = "MainCamera";
            }
            else
            {
                GetComponent<PlayerInput>().enabled = false;
                GetComponent<ActionMapHandler>().enabled = false;
                playerCamera.updateRotationWithTarget = true;
                playerCamera.gameObject.SetActive(false);
                playerHUD.gameObject.SetActive(false);
            }
        }

        //public override void OnDestroy()
        //{
        //    Destroy(playerCamera.gameObject);
        //}

        void OnDriverEnter(Vehicle newVehicle)
        {
            vehicle = newVehicle;
        }

        void OnDriverExit()
        {
            vehicle = null;
        }

        void OnChairEnter(VehicleChair newChair)
        {
            chair = newChair;

            if (rb)
                Destroy(rb);
            transform.localPosition = chair.occupantPosition;
            transform.rotation = chair.transform.rotation * Quaternion.Euler(chair.occupantRotation);
            bodyRotation = new Vector3(0, transform.eulerAngles.y, 0);
            animator.SetBool("sitting", true);

            if (!IsOwner)
                GetComponent<OwnerNetworkTransform>().Interpolate = false;
        }

        void OnChairExit()
        {
            transform.Translate(transform.rotation * chair.exitPosOffset, Space.World);

            bodyRotation = new Vector3(0, transform.eulerAngles.y, 0);
            animator.SetBool("sitting", false);

            rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            chair = null;

            if (!IsOwner)
                GetComponent<OwnerNetworkTransform>().Interpolate = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsOwner) { return; }

            if (collision.collider.CompareTag("NoPhysics") & transform.position.y > collision.GetContact(0).point.y)
            {
                if (rb)
                {
                    Destroy(rb);
                    TrySetParentServerRpc(collision.gameObject.GetComponent<NetworkObject>().NetworkObjectId);
                }
            }
        }

        [ServerRpc]
        private void RemoveParentServerRpc()
        {
            NetworkObject.TryRemoveParent();
        }

        [ServerRpc]
        private void TrySetParentServerRpc(ulong networkObjectId)
        {
            NetworkObject.TrySetParent(NetworkManager.SpawnManager.SpawnedObjects[networkObjectId], true);
        }

        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            base.OnNetworkObjectParentChanged(parentNetworkObject);
            GetComponent<OwnerNetworkTransform>().InLocalSpace = parentNetworkObject != null;
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

        Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
            if (vehicle) { vehicle.SendMessage("OnVehicleMove", moveInput); }
            if (moveInput.y <= 0 & running) { runTarget = 2; }
        }

        [Header("Mouse Look Settings")]
        public Vector4 rotateWithBoneLookLimit;
        public float sensitivity;
        public float bodyRotationSpeed;
        public float mouseUpXRotLimit;
        public float mouseDownXRotLimit;
        public bool disableLookInput;
        public bool rotateBodyWithCamera;
        public float attemptedXAngle { get; private set; }
        public Vector3 bodyRotation { get; private set; }
        Vector2 lookInput;
        float lookAngle;
        float prevLookAngle;
        float currentBodyRotSpeed;
        Vector3 rotateWithBoneRotOffset;
        MultiRotationConstraint camConstraint;
        void OnLook(InputValue value)
        {
            Look(value.Get<Vector2>(), sensitivity, Time.timeScale);
        }

        public void Look(Vector2 lookValue, float sensitivity = 1, float timeScale = 1, bool instantBoneRot = false)
        {
            lookInput = lookValue;

            // Look angle animator logic for sword swings
            if (lookInput != Vector2.zero)
            {
                //if (!animator.GetBool("attack1") & !animator.GetBool("attack2"))
                if (!animator.GetBool("attack2"))
                {
                    lookAngle = Mathf.Atan2(lookInput.x, lookInput.y) * Mathf.Rad2Deg;
                }

                if (lookAngle == 0)
                {
                    if (prevLookAngle > 0)
                    {
                        lookAngle = 1;
                    }
                    else if (prevLookAngle < 0)
                    {
                        lookAngle = -1;
                    }
                }

                if (prevLookAngle < 0 & lookAngle == 180)
                {
                    lookAngle *= -1;
                }

                animator.SetFloat("lookAngle", lookAngle);
                prevLookAngle = lookAngle;
            }

            lookInput *= sensitivity * timeScale;
            
            if (vehicle)
            {
                vehicle.SendMessage("OnVehicleLook", lookInput);
                return;
            }

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

            if (disableLookInput) { return; }

            // Body Rotation Logic (Rotation Around Y Axis)
            bodyRotation = new Vector3(0, bodyRotation.y + lookInput.x, 0);
            if (rotateBodyWithCamera)
            {
                if (rb)
                    rb.MoveRotation(Quaternion.Euler(bodyRotation));
                else
                    transform.rotation = Quaternion.Euler(bodyRotation);
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
                attemptedXAngle = Vector3.Angle(Quaternion.Euler(bodyRotation) * Vector3.forward, camTransform.forward);

                if (camTransform.forward.y > 0)
                    attemptedXAngle *= -1;

                if (attemptedXAngle > mouseDownXRotLimit)
                    camTransform.localEulerAngles = new Vector3(mouseDownXRotLimit, bodyRotation.y, 0);
                else if (attemptedXAngle < mouseUpXRotLimit)
                    camTransform.localEulerAngles = new Vector3(mouseUpXRotLimit, bodyRotation.y, 0);
            }
            else // When leaning
            {
                attemptedXAngle = Vector3.Angle(transform.forward, camTransform.forward);

                if (camTransform.forward.y > 0)
                    attemptedXAngle *= -1;

                if (attemptedXAngle > mouseDownXRotLimit)
                    camTransform.localEulerAngles = new Vector3(mouseDownXRotLimit, 0, 0);
                else if (attemptedXAngle < mouseUpXRotLimit)
                    camTransform.localEulerAngles = new Vector3(mouseUpXRotLimit, 0, 0);
            }
        }

        bool prevCamRotState;
        private void Update()
        {
            if (!IsOwner) { return; }

            if (chair)
            {
                transform.localPosition = chair.occupantPosition;
                transform.rotation = chair.transform.rotation * Quaternion.Euler(chair.occupantRotation);
                bodyRotation = new Vector3(0, transform.eulerAngles.y, 0);
            }

            playerHUD.lookAngleDisplay.rotation = Quaternion.Slerp(playerHUD.lookAngleDisplay.rotation, Quaternion.Euler(new Vector3(0, 0, -lookAngle)), playerHUD.lookAngleRotSpeed * Time.deltaTime);

            float xTarget = moveInput.x;
            if (animator.GetBool("mirrorIdle"))
                xTarget *= -1;
            if (running) { xTarget *= runTarget; }
            float x = Mathf.Lerp(animator.GetFloat("moveInputX"), xTarget, Time.deltaTime * moveTransitionSpeed);
            animator.SetFloat("moveInputX", x);

            float yTarget = moveInput.y;
            if (running) { yTarget *= runTarget; }
            float y = Mathf.Lerp(animator.GetFloat("moveInputY"), yTarget, Time.deltaTime * moveTransitionSpeed);
            animator.SetFloat("moveInputY", y);

            if (moveInput == Vector2.zero)
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) // Only change idle time if we are at rest
                    // This is used so that some states that don't have exit transitions can "remember" that the user moved during their playtime, also so that crouching and jumping is not considered "idle"
                    if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Idle Loop")).IsTag("PauseIdleTime"))
                        animator.SetFloat("idleTime", animator.GetFloat("idleTime") + Time.deltaTime);
            else // If moveInput is not Vector2.zero
                animator.SetFloat("idleTime", 0);
            // Don't want to enter idle loop while crouching
            if (crouching)
                animator.SetFloat("idleTime", 0);
            // If we jump set idleTime to 0
            if (animator.GetBool("jumping"))
                animator.SetFloat("idleTime", 0);

            animator.speed = animatorSpeed;

            if (rotateBodyWithCamera != prevCamRotState)
            {
                playerCamera.RefreshCameraParent();
            }

            if (!rotateBodyWithCamera)
            {
                if (rb)
                    rb.MoveRotation(Quaternion.Slerp(transform.rotation, Quaternion.Euler(bodyRotation), Time.deltaTime * currentBodyRotSpeed));
                else
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(bodyRotation), Time.deltaTime * currentBodyRotSpeed);
            }
            
            spineAim.data.offset = Vector3.Lerp(spineAim.data.offset, new Vector3(0, 0, targetLean / spineAim.weight), leanSpeed * Time.deltaTime);
            foreach (MultiAimConstraint aimConstraint in aimConstraints)
            {
                aimConstraint.data.offset = Vector3.Lerp(aimConstraint.data.offset, new Vector3(0, 0, targetLean), leanSpeed * Time.deltaTime);
            }

            if (playerCamera.updateRotationWithTarget)
                camConstraint.data.offset = Vector3.Lerp(camConstraint.data.offset, rotateWithBoneRotOffset, 5 * Time.deltaTime);
            else
                rotateWithBoneRotOffset = Vector3.zero; camConstraint.data.offset = Vector3.Lerp(camConstraint.data.offset, rotateWithBoneRotOffset, 5 * Time.deltaTime);

            prevCamRotState = rotateBodyWithCamera;
        }

        private void FixedUpdate()
        {
            if (!IsOwner) { return; }

            if (!rb & !chair)
            {
                Vector3 startPos = new Vector3(transform.position.x, transform.position.y + 0.7f, transform.position.z);
                RaycastHit[] allHits = Physics.RaycastAll(startPos, transform.up * -1, 2);
                System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
                foreach (RaycastHit hit in allHits)
                {
                    if (GetComponentsInChildren<Collider>().Contains(hit.collider)) { continue; }

                    if (hit.collider.CompareTag("NoPhysics"))
                    {
                        transform.position = new Vector3(hit.point.x, hit.point.y, hit.point.z);
                    }
                    else
                    {
                        rb = gameObject.AddComponent<Rigidbody>();
                        rb.constraints = RigidbodyConstraints.FreezeRotation;
                        rb.interpolation = RigidbodyInterpolation.Interpolate;
                        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                        RemoveParentServerRpc();
                    }
                    break;
                }

                // If we have no hits
                if (allHits.Length == 0)
                {
                    rb = gameObject.AddComponent<Rigidbody>();
                    rb.constraints = RigidbodyConstraints.FreezeRotation;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    RemoveParentServerRpc();
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

            //if (Time.timeScale == 1)
            //{
            //    StartCoroutine(TimeScaleAbility());
            //}
        }

        private IEnumerator TimeScaleAbility()
        {
            for (float i = 0.9f; i >= 0.1; i -= 0.05f)
            {
                yield return new WaitForSeconds(0.2f * Time.timeScale);
                Time.timeScale = Mathf.Round(i * 100) / 100;
            }

            for (float i = 0.2f; i <= 1.05; i+=0.05f)
            {
                yield return new WaitForSeconds(0.3f * Time.timeScale);
                Time.timeScale = Mathf.Round(i * 100) / 100;
            }
        }

        [Header("Misc Settings")]
        public bool toggleSprint;
        bool running;
        float runTarget;
        void OnSprint(InputValue value)
        {
            if (vehicle) { vehicle.SendMessage("OnVehicleSprint", value.isPressed); }

            if (toggleSprint)
            {
                if (value.isPressed)
                {
                    running = !running;
                    animator.SetBool("running", running);
                    runTarget = 2;
                    ascending = true;

                    if (!running)
                        animator.SetBool("sprinting", false);
                }
            }
            else
            {
                running = value.isPressed;
                animator.SetBool("running", running);
                runTarget = 2;
                ascending = true;

                if (!value.isPressed)
                    animator.SetBool("sprinting", false);
            }
        }

        bool ascending = true;
        void OnScaleSprint()
        {
            if (running & !crouching & moveInput.y > 0)
            {
                if (ascending)
                {
                    runTarget += 1;

                    if (runTarget == 4)
                        animator.SetBool("sprinting", true);
                    else
                        animator.SetBool("sprinting", false);

                    if (runTarget == 4)
                        ascending = false;
                }
                else
                {
                    runTarget -= 1;
                    if (runTarget == 2)
                        ascending = true;

                    if (runTarget == 4)
                        animator.SetBool("sprinting", true);
                    else
                        animator.SetBool("sprinting", false);
                }
            }
        }

        public bool toggleCrouch;
        bool crouching;
        void OnCrouch(InputValue value)
        {
            if (vehicle) { vehicle.SendMessage("OnVehicleCrouch", value.isPressed); }

            if (value.isPressed)
            {
                if (running)
                {
                    StartCoroutine(Utilities.ResetAnimatorBoolAfter1Frame(animator, "crouching"));
                    return;
                }
            }

            if (toggleCrouch)
            {
                if (value.isPressed)
                {
                    crouching = !crouching;
                    animator.SetBool("crouching", crouching);
                }
            }
            else
            {
                crouching = value.isPressed;
                animator.SetBool("crouching", crouching);
            }
        }

        [Header("Interact Settings")]
        public float reach;
        VehicleChair chair;
        void OnInteract()
        {
            if (animator.GetBool("sitting"))
            {
                chair.ExitSittingServerRpc();
                return;
            }

            RaycastHit[] allHits = Physics.RaycastAll(playerCamera.transform.position, playerCamera.transform.forward, reach);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform == transform) { continue; }

                if (hit.transform.GetComponent<Interactable>())
                {
                    hit.transform.GetComponent<Interactable>().Invoke(gameObject);
                }
                else if (hit.collider.GetComponent<Door>())
                {
                    hit.collider.GetComponent<Door>().ToggleDoor();
                }
                else if (hit.collider.GetComponent<VehicleChair>())
                {
                    if (animator.GetBool("falling")) { return; }
                    hit.collider.GetComponent<VehicleChair>().TrySittingServerRpc(NetworkObjectId);
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
        float targetLean;
        public void SetLean(float newTilt)
        {
            targetLean = newTilt;
            playerCamera.targetZRot = targetLean * spineAim.weight;
        }

        void OnLeanRight()
        {
            if (disableLeanInput) { return; }
            if (spineRig.weightTarget != 1) { return; }
            else if (playerCamera.updateRotationWithTarget) { return; }

            if (targetLean != rightLean)
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
                targetLean = rightLean;
            }
            else
            {
                targetLean = 0;
                playerCamera.targetZRot = 0;
            }
        }

        void OnLeanLeft()
        {
            if (disableLeanInput) { return; }
            if (spineRig.weightTarget != 1) { return; }
            else if (playerCamera.updateRotationWithTarget) { return; }

            if (targetLean != leftLean)
            {
                // Check if we are aiming too low or too high to lean
                float xAngle = Vector3.Angle(transform.forward, playerCamera.transform.forward) + Mathf.Abs(leftLean);
                if (playerCamera.transform.forward.y > 0)
                    xAngle *= -1;
                if (xAngle > mouseDownXRotLimit)
                    return;
                else if (xAngle < mouseUpXRotLimit)
                    return;

                targetLean = leftLean;
                playerCamera.targetZRot = leftLean;
            }
            else
            {
                targetLean = 0;
                playerCamera.targetZRot = 0;
            }
        }

        void OnProjectileHit(HitmarkerData hitmarkerData)
        {
            AudioManager.Singleton.PlayClipAtPoint(hitmarkerData.hitmarkerSound, transform.position, hitmarkerData.hitmarkerVolume);
            StartCoroutine(playerHUD.ToggleHitMarker(hitmarkerData.hitmarkerTime));
        }
    }
}
