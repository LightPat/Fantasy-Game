using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using LightPat.ProceduralAnimations;
using Unity.Netcode;

namespace LightPat.Core.Player
{
    public class AirborneAnimationHandler : NetworkBehaviour
    {
        [Header("Jump Settings")]
        public float jumpHeight;
        public float airborneMoveSpeed;
        public float jumpForceDelay;
        public float runningJumpHeight;
        [Header("Miscellaneous")]
        public float isGroundedDistance;
        public float breakfallRollThreshold;

        Animator animator;
        Rigidbody rb;
        RootMotionManager rootMotionManager;
        WeaponLoadout weaponLoadout;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            rb = GetComponent<Rigidbody>();
            rootMotionManager = GetComponentInChildren<RootMotionManager>();
            weaponLoadout = GetComponent<WeaponLoadout>();
        }

        public override void OnNetworkSpawn()
        {
            jumping.OnValueChanged += OnJumpChange;
            breakfallRoll.OnValueChanged += OnBreakfallRollChange;
            wallRunning.OnValueChanged += OnWallRunChange;
            rightLeftMultiplier.OnValueChanged += OnRightLeftMultiplierChange;
            falling.OnValueChanged += OnFallingChange;
        }

        public override void OnNetworkDespawn()
        {
            jumping.OnValueChanged -= OnJumpChange;
            breakfallRoll.OnValueChanged -= OnBreakfallRollChange;
            wallRunning.OnValueChanged -= OnWallRunChange;
            rightLeftMultiplier.OnValueChanged -= OnRightLeftMultiplierChange;
            falling.OnValueChanged -= OnFallingChange;
        }

        void OnBreakfallRollChange(bool previous, bool current) { animator.SetBool("breakfallRoll", current); }

        void OnFallingChange(bool previous, bool current) { animator.SetBool("falling", current); }

        private NetworkVariable<Vector3> rbVelocity = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<bool> breakfallRoll = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<bool> falling = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        bool prevGrounded;
        private void Update()
        {
            if (!rb)
            {
                rb = GetComponent<Rigidbody>();
                return;
            }

            bool isGrounded = IsGrounded();

            animator.SetFloat("yVelocity", rbVelocity.Value.y);

            // Wall running logic
            if (animator.GetBool("wallRun"))
            {
                //if (TryGetComponent(out PlayerController playerController))
                //    playerController.rotateBodyWithCamera = false;
                
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                handTarget.GetComponentInParent<RigWeightTarget>().weightTarget = 1;
                animator.SetFloat("moveInputX", 0);
                //animator.SetFloat("moveInputY", 0);

                RaycastHit[] allHits = Physics.RaycastAll(transform.position, transform.right * rightLeftMultiplier.Value, 3, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                if (allHits.Length == 0) { EndWallRun(); return; }
                System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
                foreach (RaycastHit hit in allHits)
                {
                    if (hit.transform == transform) { continue; }

                    //transform.rotation = Quaternion.LookRotation(hit.normal, Vector3.right * rightLeftMultiplier * -1);
                    transform.rotation = Quaternion.LookRotation(Vector3.Cross(hit.normal, Vector3.down * rightLeftMultiplier.Value));
                    rootRotationConstraint.localRotation = Quaternion.Euler(0, 0, zRot * rightLeftMultiplier.Value);

                    // Left leg
                    foreach (Collider c in legTarget.GetComponentInParent<TwoBoneIKConstraint>().data.tip.GetComponentsInChildren<Collider>())
                    {
                        Physics.IgnoreCollision(c, hit.collider, true);
                    }
                    legTarget.position = hit.point + transform.rotation * new Vector3(wallRunFootPositionOffset.x * rightLeftMultiplier.Value, wallRunFootPositionOffset.y, wallRunFootPositionOffset.z);
                    legTarget.rotation = Quaternion.LookRotation(Vector3.Cross(hit.normal, Vector3.down * rightLeftMultiplier.Value)) * Quaternion.Euler(wallRunFootRotationOffset * rightLeftMultiplier.Value);
                    break;
                }

                Transform shoulder = handTarget.GetComponentInParent<TwoBoneIKConstraint>().data.root.parent;
                allHits = Physics.RaycastAll(shoulder.position, transform.right * rightLeftMultiplier.Value, 3, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                if (allHits.Length == 0) { EndWallRun(); return; }
                System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
                foreach (RaycastHit hit in allHits)
                {
                    if (hit.transform == transform) { continue; }
                    // Left hand
                    foreach (Collider c in handTarget.GetComponentInParent<TwoBoneIKConstraint>().data.tip.GetComponentsInChildren<Collider>())
                    {
                        Physics.IgnoreCollision(c, hit.collider, true);
                    }
                    handTarget.position = hit.point + transform.rotation * new Vector3(wallRunHandPositionOffset.x * rightLeftMultiplier.Value, wallRunHandPositionOffset.y, wallRunHandPositionOffset.z);
                    handTarget.rotation = Quaternion.LookRotation(Vector3.Cross(hit.normal, Vector3.down * rightLeftMultiplier.Value)) * Quaternion.Euler(wallRunHandRotationOffset * rightLeftMultiplier.Value);
                    break;
                }
            }
            else if (!transform.parent)
            {
                if (IsOwner)
                    falling.Value = !isGrounded;
            }

            if (IsAirborne() | IsJumping())
                rootMotionManager.drag = 0;
            else
                rootMotionManager.drag = 1;

            // If we were falling on the last frame and we are not on this one
            if (!prevGrounded & isGrounded)
            {
                if (rbVelocity.Value.magnitude > breakfallRollThreshold)
                {
                    if (IsOwner)
                        breakfallRoll.Value = true;
                    animator.SetFloat("landingAngle", Vector2.SignedAngle(new Vector2(rb.velocity.x, rb.velocity.z), new Vector2(transform.forward.x, transform.forward.z)));
                }
            }

            animator.SetBool("fallingLoopReached", IsAirborne());

            prevGrounded = isGrounded;
        }

        private void FixedUpdate()
        {
            if (!rb)
            {
                rb = GetComponent<Rigidbody>();
                return;
            }

            if (IsAirborne())
            {
                Vector3 moveForce = rb.rotation * new Vector3(moveInput.x, 0, moveInput.y) * airborneMoveSpeed;

                // If rigidbody's velocity magnitude is greater than moveForce's magnitude
                if (new Vector2(rb.velocity.x, rb.velocity.z).magnitude > new Vector2(moveForce.x, moveForce.z).magnitude + airborneMoveSpeed) { return; }

                moveForce.x -= rb.velocity.x;
                moveForce.z -= rb.velocity.z;

                rb.AddForce(moveForce, ForceMode.VelocityChange);
            }

            if (IsOwner)
                rbVelocity.Value = rb.velocity;
        }

        void OnJump(InputValue value)
        {
            if (!rb) { return; }
            if (!value.isPressed) { return; }
            if (animator.GetBool("sitting")) { return; }
            if (IsAirborne() | IsJumping() | IsLanding() | rb.velocity.y > 1 | animator.IsInTransition(animator.GetLayerIndex("Airborne"))) { return; }
            StartCoroutine(Jump());
            EndWallRun();
        }

        private NetworkVariable<bool> jumping = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        void OnJumpChange(bool previous, bool current) { animator.SetBool("jumping", current); }

        private IEnumerator Jump()
        {
            jumping.Value = true;
            int startTick = NetworkManager.LocalTime.Tick;

            yield return new WaitForSeconds(jumpForceDelay);
            if (!animator.GetBool("running"))
            {
                float jumpForce = Mathf.Sqrt(jumpHeight * -2 * Physics.gravity.y);
                rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.VelocityChange);
            }
            else
            {
                // Jump in direction rigidbody is moving
                float jumpForce = Mathf.Sqrt(runningJumpHeight * -2 * Physics.gravity.y);
                rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.VelocityChange);
            }

            // Wait for 1 tick or more to pass before setting jump to false again
            yield return new WaitUntil(() => NetworkManager.LocalTime.Tick >= startTick + 1);
            jumping.Value = false;
        }
        
        Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        bool landingCollisionRunning;
        private void OnCollisionEnter(Collision collision)
        {
            if (animator.GetBool("falling") & collision.gameObject.CompareTag("WallRun"))
                StartWallRun(collision);

            if (landingCollisionRunning) { return; }

            if ((IsAirborne() | IsJumping()) & !IsLanding())
            {
                animator.SetFloat("landingMagnitude", collision.relativeVelocity.magnitude);

                landingCollisionRunning = true;
                StartCoroutine(ResetLandingBool());
            }
        }

        private IEnumerator ResetLandingBool()
        {
            yield return new WaitUntil(() => !(IsAirborne() | IsJumping()) & !IsLanding());
            landingCollisionRunning = false;
            breakfallRoll.Value = false;
        }

        [Header("Wall Run Settings")]
        public RigWeightTarget rootRotationRig;
        public float zRot;
        public float wallRunDelay;
        public Transform rootRotationConstraint;
        public Transform rightLegTarget;
        public Transform rightHandTarget;
        public Transform leftLegTarget;
        public Transform leftHandTarget;
        public Vector3 wallRunHandPositionOffset;
        public Vector3 wallRunHandRotationOffset;
        public Vector3 wallRunFootPositionOffset;
        public Vector3 wallRunFootRotationOffset;

        Transform legTarget;
        Transform handTarget;
        float lastWallRunExitTime;

        private NetworkVariable<bool> wallRunning = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<int> rightLeftMultiplier = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        void OnWallRunChange(bool previous, bool current) { animator.SetBool("wallRun", current); }

        void OnRightLeftMultiplierChange(int previous, int current)
        {
            if (current == 1) // Wall is on right
            {
                // Variable assignments to use in Update()
                legTarget = rightLegTarget;
                legTarget.GetComponent<FollowTarget>().enabled = false;
                handTarget = rightHandTarget;

                // Activate IK Rigs
                legTarget.GetComponentInParent<Rig>().weight = 1;
                handTarget.GetComponentInParent<RigWeightTarget>().weightTarget = 1;
                handTarget.GetComponent<FollowTarget>().move = false;
                handTarget.GetComponent<FollowTarget>().rotate = false;
                GetComponentInChildren<RootMotionManager>().disableRightHand = true;

                rb.useGravity = false;
                if (IsOwner)
                    falling.Value = false;
                ConstantForce wallForce = gameObject.AddComponent<ConstantForce>();
                wallForce.relativeForce = new Vector3(50 * current, 0, 0);
                rootRotationRig.weightTarget = 1;
                if (TryGetComponent(out PlayerController playerController))
                {
                    playerController.disableLeanInput = true;
                    playerController.SetLean(0);
                }
            }
            else if (current == -1) // Wall is on left
            {
                // Variable assignments to use in Update()
                legTarget = leftLegTarget;
                legTarget.GetComponent<FollowTarget>().enabled = false;
                handTarget = leftHandTarget;

                // Activate IK Rigs
                legTarget.GetComponentInParent<Rig>().weight = 1;
                handTarget.GetComponentInParent<RigWeightTarget>().weightTarget = 1;
                handTarget.GetComponent<FollowTarget>().move = false;
                handTarget.GetComponent<FollowTarget>().rotate = false;
                GetComponentInChildren<RootMotionManager>().disableLeftHand = true;

                rb.useGravity = false;
                if (IsOwner)
                    falling.Value = false;
                ConstantForce wallForce = gameObject.AddComponent<ConstantForce>();
                wallForce.relativeForce = new Vector3(50 * current, 0, 0);
                rootRotationRig.weightTarget = 1;
                if (TryGetComponent(out PlayerController playerController))
                {
                    playerController.disableLeanInput = true;
                    playerController.SetLean(0);
                }
            }
            else if (current == 0)
            {
                rb.useGravity = true;
                GetComponentInChildren<RootMotionManager>().disableLeftHand = false;
                GetComponentInChildren<RootMotionManager>().disableRightHand = false;
                Destroy(GetComponent<ConstantForce>());

                legTarget.GetComponentInParent<Rig>().weight = 0;

                if (weaponLoadout.equippedWeapon)
                    handTarget.GetComponentInParent<RigWeightTarget>().weightTarget = 1;
                else
                    handTarget.GetComponentInParent<RigWeightTarget>().weightTarget = 0;

                handTarget.GetComponent<FollowTarget>().move = true;
                handTarget.GetComponent<FollowTarget>().rotate = true;

                legTarget.GetComponent<FollowTarget>().enabled = true;
                legTarget = null;
                handTarget = null;
                rootRotationConstraint.localRotation = Quaternion.Euler(0, 0, 0);
                rootRotationRig.weightTarget = 0;
                //if (TryGetComponent(out PlayerController playerController))
                //{
                //    playerController.disableLeanInput = false;
                //    playerController.rotateBodyWithCamera = weaponLoadout.equippedWeapon;
                //}
            }
        }

        void StartWallRun(Collision collision)
        {
            if (Time.time - lastWallRunExitTime < wallRunDelay) { return; }

            wallRunning.Value = true;

            // Determine if wall is to left or right
            if (Vector3.SignedAngle(transform.forward, collision.GetContact(0).point - transform.position, Vector3.up) > 0) // Wall is on our right
                rightLeftMultiplier.Value = 1;
            else // Wall is on our left
                rightLeftMultiplier.Value = -1;
        }

        void EndWallRun()
        {
            if (!animator.GetBool("wallRun")) { return; }

            lastWallRunExitTime = Time.time;
            wallRunning.Value = false;

            rightLeftMultiplier.Value = 0;
        }

        bool IsAirborne()
        {
            return animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Airborne")).IsTag("Airborne");
        }

        bool IsJumping()
        {
            if (weaponLoadout.equippedWeapon == null)
            {
                return animator.GetCurrentAnimatorStateInfo(0).IsTag("Jumping");
            }
            else
            {
                return animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex(weaponLoadout.equippedWeapon.animationClass)).IsTag("Jumping");
            }
        }

        bool IsLanding()
        {
            return animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Airborne")).IsTag("Landing");
        }

        bool IsGrounded()
        {
            RaycastHit hit;
            return rb.SweepTest(Vector3.down, out hit, isGroundedDistance);
        }
    }
}
