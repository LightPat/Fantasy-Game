using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using LightPat.ProceduralAnimations;

namespace LightPat.Core
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(Attributes))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Object Assignments")]
        public Transform verticalRotate;
        public Transform playerModel;
        public GameObject crosshair;
        public GameObject escapeMenu;
        public GameObject inventoryPrefab;
        [HideInInspector] public GameObject equippedWeapon;

        [Header("Switch Cameras")]
        public GameObject firstPersonCamera;
        public GameObject thirdPersonCamera;

        private Rigidbody rb;
        private float currentSpeedTarget;
        private float? lockSpeedTarget = null;
        private AudioSource audioSrc;
        private Animator animator;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            audioSrc = GetComponent<AudioSource>();
            playerInput = GetComponent<PlayerInput>();
            currentSpeedTarget = walkingSpeed;
            Cursor.lockState = CursorLockMode.Locked;
            lookEulers = new Vector3(transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.z);
            animator = GetComponent<Animator>();
        }

        private void Update()
        {
            // Crosshair Color Change Hover Logic
            // Red is for enemy
            // Blue is for interactable
            // Green is for friendly
            RaycastHit hit;
            bool bHit = Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, reach);

            if (bHit)
            {
                if (hit.transform.GetComponent<Interactable>())
                {
                    crosshair.GetComponent<Image>().color = new Color32(0, 0, 255, 255);
                }
                else if (hit.transform.GetComponent<Enemy>())
                {
                    crosshair.GetComponent<Image>().color = new Color32(255, 0, 0, 255);
                }
                else if (hit.transform.GetComponent<Friendly>())
                {
                    crosshair.GetComponent<Image>().color = new Color32(0, 255, 0, 255);
                }
                else if (hit.transform.GetComponent<Weapon>())
                {
                    crosshair.GetComponent<Image>().color = new Color32(255, 127, 80, 255);
                }
                else
                {
                    crosshair.GetComponent<Image>().color = new Color32(0, 0, 0, 255);
                }
            }
            else
            {
                crosshair.GetComponent<Image>().color = new Color32(0, 0, 0, 255);
            }

            // Look logic
            if (!disableLookInput)
            {
                lookInput *= (sensitivity);
                lookEulers.x += lookInput.x;

                // This prevents the rotation from increasing or decreasing infinitely if the player does a bunch of spins horizontally
                if (lookEulers.x >= 360)
                {
                    lookEulers.x = lookEulers.x - 360;
                }
                else if (lookEulers.x <= -360)
                {
                    lookEulers.x = lookEulers.x + 360;
                }

                /* First Person Camera Rotation Logic
                Remember that the camera is a child of the player, so we don't need to worry about horizontal rotation, that has already been calculated
                Calculate vertical rotation for the first person camera if you're not looking straight up or down already
                If we reach the top or bottom of our vertical look bound, set the total rotation amount to 1 over or 1 under the bound
                Otherwise, just change the rotation by the lookInput */
                if (lookEulers.y < -verticalLookBound)
                {
                    lookEulers.y = -verticalLookBound - 1;

                    if (lookInput.y > 0)
                    {
                        lookEulers.y += lookInput.y;
                    }
                }
                else if (lookEulers.y > verticalLookBound)
                {
                    lookEulers.y = verticalLookBound + 1;

                    if (lookInput.y < 0)
                    {
                        lookEulers.y += lookInput.y;
                    }
                }
                else
                {
                    lookEulers.y += lookInput.y;
                }

                Quaternion newRotation = Quaternion.Euler(0, lookEulers.x, 0);
                rb.MoveRotation(newRotation);
                verticalRotate.rotation = Quaternion.Euler(-lookEulers.y, lookEulers.x, 0);
            }

            animator.SetFloat("Speed", new Vector2(rb.velocity.x, rb.velocity.z).magnitude);

            // If we are not landing, update falling animation parameter
            if (!landing)
            {
                animator.SetBool("Falling", !IsGrounded());
            }
            else // If we are landing, once we are fully on the ground, exit the landing state
            {
                if (IsGrounded())
                {
                    landing = false;
                    // lockMoveInput and lockSpeedTarget are reset using animation events
                }
            }

            // If we are falling but not landing, we send a raycast to see when we are about to enter the landing state
            if (animator.GetBool("Falling") & rb.velocity.y < 0 & !landing)
            {
                if (IsGrounded(landingCheckDistance))
                {
                    // Enter landing state
                    landing = true;
                    animator.SetBool("Falling", false);
                    // Update vertical speed
                    animator.SetFloat("LandingSpeed", rb.velocity.y);
                }
            }

            // Decode move input into a direction for animator
            // Forwards == 0
            // Right == 1
            // Backwards == 2
            // Left == 3
            if (moveInput == new Vector2(1, 0))
            {
                animator.SetInteger("Direction", 1); // right
            }
            else if (moveInput == new Vector2(-1, 0))
            {
                animator.SetInteger("Direction", 3); // left
            }
            else if (moveInput.y > 0)
            {
                animator.SetInteger("Direction", 0); // forwards
            }
            else if (moveInput.y < 0)
            {
                animator.SetInteger("Direction", 2); // backwards
            }

            float interpSpeed = 7f;
            // Rotate the player model based on the direction of the moveInput vector when pressing two keys at the same time
            if (Vector2.Distance(moveInput, new Vector2(0.7f, 0.7f)) < 0.1)
            {
                // forward - right
                playerModel.localRotation = Quaternion.Slerp(playerModel.localRotation, Quaternion.Euler(0, 45, 0), interpSpeed * Time.deltaTime);
            }
            else if (Vector2.Distance(moveInput, new Vector2(-0.7f, 0.7f)) < 0.1)
            {
                // forward - left
                playerModel.localRotation = Quaternion.Slerp(playerModel.localRotation, Quaternion.Euler(0, -45, 0), interpSpeed * Time.deltaTime);
            }
            else if (Vector2.Distance(moveInput, new Vector2(0.7f, -0.7f)) < 0.1)
            {
                // backward - right
                playerModel.localRotation = Quaternion.Slerp(playerModel.localRotation, Quaternion.Euler(0, -45, 0), interpSpeed * Time.deltaTime);
            }
            else if (Vector2.Distance(moveInput, new Vector2(-0.7f, -0.7f)) < 0.1)
            {
                // backward - left
                playerModel.localRotation = Quaternion.Slerp(playerModel.localRotation, Quaternion.Euler(0, 45, 0), interpSpeed * Time.deltaTime);
            }
            else
            {
                playerModel.localRotation = Quaternion.Slerp(playerModel.localRotation, Quaternion.identity, interpSpeed * Time.deltaTime);
            }
        }

        void FixedUpdate()
        {
            if (!disableMoveInput)
            {
                Vector3 moveForce;
                if (lockMoveInput == null)
                {
                    moveForce = rb.rotation * new Vector3(moveInput.x, 0, moveInput.y);
                }
                else
                {
                    Vector3 casted = (Vector3)lockMoveInput;
                    moveForce = rb.rotation * new Vector3(casted.x, 0, casted.y);
                }

                if (lockSpeedTarget == null)
                {
                    moveForce *= currentSpeedTarget;
                }
                else
                {
                    moveForce *= (float)lockSpeedTarget;
                }

                moveForce.x -= rb.velocity.x;
                moveForce.z -= rb.velocity.z;
                rb.AddForce(moveForce, ForceMode.VelocityChange);
            }

            if (!audioSrc.isPlaying & rb.velocity.magnitude > walkingSpeed - 1 & moveInput != Vector2.zero & IsGrounded())
            {
                //StartCoroutine(PlayFootstep());
            }

            // Falling Gravity velocity increase
            if (rb.velocity.y < 0)
            {
                rb.AddForce(new Vector3(0, (fallingGravityScale * -1), 0), ForceMode.VelocityChange);
            }
        }

        [Header("Footstep Detection Settings")]
        public float footstepDetectionRadius = 10f;
        private IEnumerator PlayFootstep()
        {
            audioSrc.Play();
            Collider[] colliders = Physics.OverlapSphere(transform.position, footstepDetectionRadius);
            foreach (Collider c in colliders)
            {
                c.SendMessageUpwards("OnFootstep", transform.position, SendMessageOptions.DontRequireReceiver);
            }
            yield return new WaitForSeconds(.3f);
            audioSrc.Pause();
        }

        // The formatting is: speedTarget (null, 0, Walk, or Sprint) lockMoveInput (null or two values to represent a vector2 input)
        public void LockMovementEvent(string parameters)
        {
            string[] subs = parameters.Split(' ');

            if (subs[0] == "Sprint")
            {
                lockSpeedTarget = sprintSpeed;
            }
            else if (subs[0] == "Walk")
            {
                lockSpeedTarget = walkingSpeed;
            }
            else if (subs[0] == "0")
            {
                lockSpeedTarget = 0;
            }
            else if (subs[0] == "null")
            {
                lockSpeedTarget = null;
            }

            if (subs[1] == "null")
            {
                lockMoveInput = null;
            }
            else
            {
                lockMoveInput = new Vector2(float.Parse(subs[1]), float.Parse(subs[2]));
            }
        }

        // 1 means rotate with bone, 0 means rotate with mouse input
        public void RotateCameraWithBoneEvent(int value)
        {
            if (value == 1) // Start updating rotation, disable look input
            {
                firstPersonCamera.GetComponent<PlayerCameraFollow>().UpdateRotation = true;
                disableLookInput = true;
            }
            else if (value == 0) // Stop updating rotation, re-enable look input, reset the camera's rotation
            {
                firstPersonCamera.GetComponent<PlayerCameraFollow>().UpdateRotation = false;
                disableLookInput = false;

                // Set verticalRotate's rotation to the rotation of the fps cam at the end of the animation
                verticalRotate.rotation = firstPersonCamera.transform.rotation;
                // Set fps cam to the same rotation as vertical rotate
                firstPersonCamera.transform.localRotation = Quaternion.identity;
                // Adjust lookEulers to new verticalRotate rotation
                lookEulers = new Vector3(verticalRotate.rotation.eulerAngles.y, -verticalRotate.rotation.eulerAngles.x, verticalRotate.rotation.eulerAngles.z);
            }
        }

        [Header("Move Settings")]
        public float walkingSpeed = 5f;
        [HideInInspector] public Vector2 moveInput;
        // lockMoveInput is a vector2 that has a higher priority than moveInput. This should only be assigned to make the player keep moving in a certain direction for a period of time.
        private Vector2? lockMoveInput;
        // stopMoveInput is a boolean to stop the player from moving
        private bool disableMoveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        [Header("Look Settings")]
        public float sensitivity = 1f;
        public float verticalLookBound = 90f;
        private Vector3 lookEulers;
        private Vector2 lookInput;
        private bool disableLookInput;
        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>();
        }

        [Header("Interact Settings")]
        public float reach = 4f;
        public float reachSpeed = 0.05f;
        public Transform weaponParent;
        public Rig armsRig;
        public TwoBoneIKConstraint rightHand;
        public TwoBoneIKConstraint leftHand;
        void OnInteract()
        {
            RaycastHit hit;
            // Raycast gameObject that we are looking at if it is in the range of our reach
            bool bHit = Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, reach);

            if (bHit)
            {
                if (hit.transform.GetComponent<Interactable>())
                {
                    hit.transform.GetComponent<Interactable>().Invoke();
                }
                else if (hit.transform.GetComponent<Weapon>())
                {
                    if (equippedWeapon != null) { equippedWeapon.SetActive(false); }

                    StartCoroutine(PickUpItem(hit.transform.Find("ref_right_hand_grip")));
                    //equippedWeapon = hit.transform.gameObject;
                    //equippedWeapon.transform.SetParent(transform, true);
                    //equippedWeapon.transform.GetComponentInChildren<Collider>().enabled = false;
                    //Destroy(equippedWeapon.GetComponent<Rigidbody>());
                    //equippedWeapon.transform.SetParent(weaponParent);
                    //equippedWeapon.transform.localRotation = Quaternion.identity;
                    //equippedWeapon.transform.localPosition = equippedWeapon.GetComponent<Weapon>().offset;

                    //armsRig.weight = 1;

                    //rightHand.transform.GetChild(0).GetComponent<MirrorTarget>().target = equippedWeapon.transform.Find("ref_right_hand_grip");
                    //leftHand.transform.GetChild(0).GetComponent<MirrorTarget>().target = equippedWeapon.transform.Find("ref_left_hand_grip");
                }
            }
        }

        private IEnumerator PickUpItem(Transform target)
        {
            disableLookInput = true;
            disableMoveInput = true;
            // Set IK weight to 1
            armsRig.weight = 1;

            DestroyImmediate(target.GetComponentInParent<Rigidbody>());
            target.parent.GetComponentInChildren<Collider>().enabled = false;

            Transform rightTarget = rightHand.transform.GetChild(0);
            //if (target.position.y - transform.position.y > 0.3f)
            //{
            //    animator.SetBool("Crouching", true);
            //}

            float lerpProgress = 0;
            Vector3 oldPosition = transform.position;
            Vector3 newPosition = target.position - transform.forward * 0.5f;
            oldPosition.y = 0;
            newPosition.y = 0;
            Vector3 previous = transform.position;
            // Move player body close to target so that arm can reach it
            while (lerpProgress < 1)
            {
                // Interpolate towards target point
                Vector3 interpolatedPosition = Vector3.Lerp(oldPosition, newPosition, lerpProgress);
                interpolatedPosition.y = transform.position.y;
                transform.position = interpolatedPosition;
                lerpProgress += Time.deltaTime * reachSpeed;

                // Have to calculate velocity for animator since we aren't using rigidbody
                float velocity = (transform.position - previous).magnitude / Time.deltaTime;
                previous = transform.position;
                animator.SetFloat("Speed", velocity);
                yield return null;
            }

            lerpProgress = 0;
            // Lerp IK target position
            oldPosition = rightTarget.position;
            rightTarget.GetComponent<MirrorTarget>().move = false;
            rightTarget.GetComponent<MirrorTarget>().rotate = false;
            while (lerpProgress < 1)
            {
                Vector3 interpolatedPosition = Vector3.Lerp(oldPosition, target.position, lerpProgress);
                rightTarget.rotation = Quaternion.Slerp(rightTarget.rotation, target.rotation, lerpProgress);
                //interpolatedPosition.z += Mathf.Sin(lerpProgress * Mathf.PI);
                rightTarget.position = interpolatedPosition;
                lerpProgress += Time.deltaTime * reachSpeed;
                yield return null;
            }

            // Set parent to bone tip
            target.parent.SetParent(rightHand.data.tip, true);
            //target.GetComponentInParent<Weapon>().positionOffset = target.parent.localPosition;
            //target.GetComponentInParent<Weapon>().rotationOffset = target.parent.localEulerAngles;
            //target.GetComponentInParent<Weapon>().fakeParent = rightHand.data.tip;
            //target.parent.SetParent(null, true);
            
            // Deactivate IK
            float weightProgress = 1;
            while (weightProgress > 0)
            {
                weightProgress -= Time.deltaTime * reachSpeed;
                armsRig.weight = weightProgress;
                animator.SetLayerWeight(1, 1 - weightProgress);
                yield return null;
            }

            rightTarget.GetComponent<MirrorTarget>().move = true;
            rightTarget.GetComponent<MirrorTarget>().rotate = true;

            disableMoveInput = false;
            disableLookInput = false;
        }

        [Header("Jump Settings")]
        public float jumpHeight = 3f;
        public float fallingGravityScale = 0.5f;
        public float jumpDelay = 1f;
        public float landingCheckDistance = 5f;
        public float airborneXZSpeed = 1f;
        private float lastLandingTime = 0;
        private bool jumpRunning;
        private bool landing;
        void OnJump()
        {
            // If the difference between the time we finished our last jump and the current time is greater than our jump delay, do not execute anymore code
            if (Time.time - lastLandingTime < jumpDelay)
            {
                return;
            }

            // If we are on the ground, jump
            if (IsGrounded())
            {
                // This fixes spacebar spamming adding too much force on jump
                if (jumpRunning) { return; }

                if (animator.GetCurrentAnimatorStateInfo(0).IsTag("Walking") | animator.GetCurrentAnimatorStateInfo(0).IsName("Standing Idle")) // If we are walking or standing still
                {
                    StartCoroutine(IdleJump());
                }
                else if (animator.GetCurrentAnimatorStateInfo(0).IsTag("Running") & !animator.GetBool("Crouching") & animator.GetInteger("Direction") == 0) // If we are running forwards, but not sliding
                {
                    StartCoroutine(RunningJump());
                }
                else if (animator.GetCurrentAnimatorStateInfo(0).IsTag("Running") & !animator.GetBool("Crouching"))
                {
                    StartCoroutine(IdleJump());
                }
            }
        }

        private IEnumerator IdleJump()
        {
            animator.SetBool("Jumping", true);
            jumpRunning = true;
            lockSpeedTarget = airborneXZSpeed;

            // Stop moving and looking for half a second while animation completes
            disableLookInput = true;
            disableMoveInput = true;
            yield return new WaitForSeconds(0.5f);
            disableMoveInput = false;
            disableLookInput = false;

            float jumpForce = Mathf.Sqrt(jumpHeight * -2 * Physics.gravity.y);
            rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.VelocityChange);

            // Wait so that isGrounded() doesn't return true while taking off
            yield return new WaitForSeconds(0.1f);

            float startTime = Time.time;

            // While we are stil airborne, we wait until we are on the ground again
            while (!IsGrounded())
            {
                yield return new WaitForEndOfFrame();
                // If the jump animation has reached the midpoint, switch to falling
                if (Time.time - startTime >= 0.6) { break; }
            }

            animator.SetBool("Jumping", false);
            lastLandingTime = Time.time;
            jumpRunning = false;
        }

        private IEnumerator RunningJump()
        {
            animator.SetBool("Jumping", true);
            jumpRunning = true;
            // Make the player continue sprinting through the whole jump and roll sequence
            lockMoveInput = moveInput;
            lockSpeedTarget = sprintSpeed;

            float jumpForce = Mathf.Sqrt(jumpHeight * -2 * Physics.gravity.y);
            rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.VelocityChange);

            // Wait so that isGrounded() doesn't return true while taking off
            yield return new WaitForSeconds(0.1f);

            float startTime = Time.time;

            // While we are stil airborne, we wait until we are on the ground again
            while (!IsGrounded(0.1f))
            {
                yield return new WaitForEndOfFrame();
                // If the jump animation has reached the midpoint, switch to falling
                if (Time.time - startTime >= 0.4) { break; }
            }

            animator.SetBool("Jumping", false);
            lastLandingTime = Time.time;
            jumpRunning = false;
        }

        [Header("IsGrounded Settings")]
        public float checkDistance = 1;
        private bool IsGrounded()
        {
            // TODO this isn't really an elegant solution, if you stand on the edge of something it doesn't realize that you are still grouded
            // If you check for velocity = 0 then you can double jump since the apex of your jump's velocity is 0
            // Check if the player is touching a gameObject under them
            // May need to change 1.5f to be a different number if you switch the asset of the player model

            RaycastHit hit;
            bool bHit = Physics.Raycast(new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z), Vector3.up * -1, out hit, checkDistance);
            return bHit;
        }

        private bool IsGrounded(float checkDistance)
        {
            // TODO this isn't really an elegant solution, if you stand on the edge of something it doesn't realize that you are still grouded
            // If you check for velocity = 0 then you can double jump since the apex of your jump's velocity is 0
            // Check if the player is touching a gameObject under them
            // May need to change 1.5f to be a different number if you switch the asset of the player model

            RaycastHit hit;
            bool bHit = Physics.Raycast(new Vector3(transform.position.x, transform.position.y + 0.1f, transform.position.z), transform.up * -1, out hit, checkDistance);
            return bHit;
        }

        [Header("Sprint Settings")]
        public float sprintSpeed = 100f;
        public bool toggleSprint;
        void OnSprint(InputValue value)
        {
            if (toggleSprint)
            {
                if (value.isPressed)
                {
                    if (currentSpeedTarget == sprintSpeed)
                    {
                        currentSpeedTarget = walkingSpeed;
                    }
                    else if (currentSpeedTarget == walkingSpeed)
                    {
                        currentSpeedTarget = sprintSpeed;
                    }
                }
            }
            else // If toggle sprint is off
            {
                if (value.isPressed)
                {
                    currentSpeedTarget = sprintSpeed;
                }
                else
                {
                    currentSpeedTarget = walkingSpeed;
                }
            }
        }

        [Header("Crouch Settings")]
        public float crouchSpeed = 2f;
        public bool toggleCrouch;
        void OnCrouch(InputValue value)
        {
            if (toggleCrouch)
            {
                if (value.isPressed)
                {
                    if (currentSpeedTarget == crouchSpeed)
                    {
                        animator.SetBool("Crouching", false);
                        currentSpeedTarget = walkingSpeed;
                    }
                    else
                    {
                        animator.SetBool("Crouching", true);
                        // If sprint is active, and we are only holding W
                        if (currentSpeedTarget == sprintSpeed & moveInput.y == 1)
                        {
                            StartCoroutine(Slide());
                            return;
                        }
                        else
                        {
                            currentSpeedTarget = crouchSpeed;
                        }
                    }
                }
            }
            else // If toggle crouch is off
            {
                animator.SetBool("Crouching", value.isPressed);
                if (value.isPressed)
                {
                    // If we are running
                    if (currentSpeedTarget == sprintSpeed)
                    {
                        StartCoroutine(Slide());
                        return;
                    }

                    currentSpeedTarget = crouchSpeed;
                }
                else
                {
                    // If we are sliding
                    if (currentSpeedTarget == sprintSpeed)
                    {
                        return;
                    }

                    currentSpeedTarget = walkingSpeed;
                }
            }
        }

        private IEnumerator Slide()
        {
            // This prevents the player from holding down crouch and endlessly sliding
            yield return new WaitForSeconds(1f);
            animator.SetBool("Crouching", false);
        }

        [Header("Attack Settings")]
        public float attackDamage = 10f;
        void OnAttack()
        {
            RaycastHit hit;
            bool bHit = Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, reach);

            if (bHit)
            {
                if (hit.transform.GetComponent<Attributes>())
                {
                    if (equippedWeapon)
                    {
                        hit.transform.GetComponent<Attributes>().InflictDamage(equippedWeapon.GetComponent<Weapon>().baseDamage, gameObject);
                    }
                    else
                    {
                        hit.transform.GetComponent<Attributes>().InflictDamage(attackDamage, gameObject);
                    }
                }
            }
        }

        void OnTestAnim()
        {
            animator.SetBool("New", !animator.GetBool("New"));
        }

        void OnSwitchCameras()
        {
            if (firstPersonCamera.activeInHierarchy)
            {
                firstPersonCamera.SetActive(false);
                thirdPersonCamera.SetActive(true);
            }
            else
            {
                firstPersonCamera.SetActive(true);
                thirdPersonCamera.SetActive(false);
            }
        }

        void OnAttacked(GameObject value)
        {
            Debug.Log("I'm being attacked! " + value);
        }

        private PlayerInput playerInput;
        private string lastActionMapName;
        private GameObject menu;
        void OnEscape()
        {
            if (playerInput.currentActionMap.name != "Menu")
            {
                lastActionMapName = playerInput.currentActionMap.name;
                playerInput.SwitchCurrentActionMap("Menu");

                transform.Find("HUD").gameObject.SetActive(false);
                menu = Instantiate(escapeMenu);
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                if (menu.GetComponent<Menu>().childMenu)
                {
                    Destroy(menu.GetComponent<Menu>().childMenu);
                }
                Destroy(menu);
                transform.Find("HUD").gameObject.SetActive(true);
                playerInput.SwitchCurrentActionMap(lastActionMapName);
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        private GameObject inventoryMenu;
        void OnInventoryToggle()
        {
            if (playerInput.currentActionMap.name != "Inventory")
            {
                lastActionMapName = playerInput.currentActionMap.name;
                playerInput.SwitchCurrentActionMap("Inventory");

                transform.Find("HUD").gameObject.SetActive(false);
                inventoryMenu = Instantiate(inventoryPrefab);
                inventoryMenu.GetComponent<PlayerInventory>().UpdateAttributes(GetComponent<Attributes>());
                inventoryMenu.GetComponent<PlayerInventory>().UpdateWeapon(equippedWeapon);
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Destroy(inventoryMenu);
                transform.Find("HUD").gameObject.SetActive(true);
                playerInput.SwitchCurrentActionMap("First Person");
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        void OnMouseMove(InputValue value)
        {
            inventoryMenu.GetComponent<PlayerInventory>().UpdateInspectInput(value.Get<Vector2>(), 0);
        }

        void OnRotateInspectWeapon(InputValue value)
        {
            inventoryMenu.GetComponent<PlayerInventory>().UpdateInspectInput(value.isPressed, 0);
        }

        void OnResetInspectForces(InputValue value)
        {
            inventoryMenu.GetComponent<PlayerInventory>().UpdateInspectInput(value.isPressed, 1);
        }

        void OnZoomInspectCamera(InputValue value)
        {
            inventoryMenu.GetComponent<PlayerInventory>().UpdateInspectInput(value.Get<Vector2>(), 1);
        }

        void OnRotateInspectCamera(InputValue value)
        {
            inventoryMenu.GetComponent<PlayerInventory>().UpdateInspectInput(value.isPressed, 2);
        }
    }
}
