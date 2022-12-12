using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using LightPat.ProceduralAnimations;

namespace LightPat.Core.Player
{
    public class AirborneAnimationHandler : MonoBehaviour
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

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            rb = GetComponent<Rigidbody>();
            rootMotionManager = GetComponentInChildren<RootMotionManager>();
            weaponLoadout = GetComponent<WeaponLoadout>();
        }

        bool prevGrounded;
        private void Update()
        {
            if (!rb)
            {
                rb = GetComponent<Rigidbody>();
                return;
            }

            bool isGrounded = IsGrounded();

            animator.SetFloat("yVelocity", rb.velocity.y);

            // Wall running logic
            if (animator.GetBool("wallRun"))
            {
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                handTarget.GetComponentInParent<RigWeightTarget>().weightTarget = 1;
                //animator.SetFloat("moveInputX", 0);
                //animator.SetFloat("moveInputY", 0);

                RaycastHit[] allHits = Physics.RaycastAll(transform.position, transform.right * rightLeftMultiplier, 3);
                if (allHits.Length == 0) { EndWallRun(); return; }
                System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
                foreach (RaycastHit hit in allHits)
                {
                    if (hit.transform == transform) { continue; }

                    //transform.rotation = Quaternion.LookRotation(hit.normal, Vector3.right * rightLeftMultiplier * -1);
                    transform.rotation = Quaternion.LookRotation(Vector3.Cross(hit.normal, Vector3.down * rightLeftMultiplier));
                    rootRotationConstraint.localRotation = Quaternion.Euler(0, 0, zRot * rightLeftMultiplier);

                    // Left leg
                    foreach (Collider c in legTarget.GetComponentInParent<TwoBoneIKConstraint>().data.tip.GetComponentsInChildren<Collider>())
                    {
                        Physics.IgnoreCollision(c, hit.collider, true);
                    }
                    legTarget.position = hit.point + transform.rotation * new Vector3(wallRunFootPositionOffset.x * rightLeftMultiplier, wallRunFootPositionOffset.y, wallRunFootPositionOffset.z);
                    legTarget.rotation = Quaternion.LookRotation(Vector3.Cross(hit.normal, Vector3.down * rightLeftMultiplier)) * Quaternion.Euler(wallRunFootRotationOffset * rightLeftMultiplier);
                    break;
                }

                Transform shoulder = handTarget.GetComponentInParent<TwoBoneIKConstraint>().data.root.parent;
                allHits = Physics.RaycastAll(shoulder.position, transform.right * rightLeftMultiplier, 3);
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
                    handTarget.position = hit.point + transform.rotation * new Vector3(wallRunHandPositionOffset.x * rightLeftMultiplier, wallRunHandPositionOffset.y, wallRunHandPositionOffset.z);
                    handTarget.rotation = Quaternion.LookRotation(Vector3.Cross(hit.normal, Vector3.down * rightLeftMultiplier)) * Quaternion.Euler(wallRunHandRotationOffset * rightLeftMultiplier);
                    break;
                }
            }
            else if (!transform.parent)
            {
                animator.SetBool("falling", !isGrounded);
            }

            if (IsAirborne() | IsJumping())
                rootMotionManager.drag = 0;
            else
                rootMotionManager.drag = 1;

            // If we were falling on the last frame and we are not on this one
            if (!prevGrounded & isGrounded)
            {
                if (rb.velocity.magnitude > breakfallRollThreshold)
                {
                    animator.SetBool("breakfallRoll", true);
                    animator.SetFloat("landingAngle", Vector2.SignedAngle(new Vector2(rb.velocity.x, rb.velocity.z), new Vector2(transform.forward.x, transform.forward.z)));
                }
            }

            prevGrounded = isGrounded;
        }

        private void FixedUpdate()
        {
            if (IsAirborne() & rb)
            {
                Vector3 moveForce = rb.rotation * new Vector3(moveInput.x, 0, moveInput.y) * airborneMoveSpeed;

                // If rigidbody's velocity magnitude is greater than moveForce's magnitude
                if (new Vector2(rb.velocity.x, rb.velocity.z).magnitude > new Vector2(moveForce.x, moveForce.z).magnitude + airborneMoveSpeed) { return; }

                moveForce.x -= rb.velocity.x;
                moveForce.z -= rb.velocity.z;

                rb.AddForce(moveForce, ForceMode.VelocityChange);
            }
        }

        void OnJump(InputValue value)
        {
            if (!value.isPressed) { return; }
            if (animator.GetBool("sitting")) { return; }
            if (IsAirborne() | IsJumping() | IsLanding() | rb.velocity.y > 1 | animator.IsInTransition(animator.GetLayerIndex("Airborne"))) { return; }
            StartCoroutine(Jump());
            EndWallRun();
        }

        private IEnumerator Jump()
        {
            animator.SetBool("jumping", true);

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

            yield return null;
            animator.SetBool("jumping", false);
        }
        
        Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        bool landingCollisionRunning;
        private void OnCollisionEnter(Collision collision)
        {
            if (animator.GetBool("falling") & collision.gameObject.tag == "WallRun")
                StartWallRun(collision);

            if (landingCollisionRunning) { return; }

            if ((IsAirborne() | IsJumping()) & !IsLanding())
            {
                animator.SetFloat("landingMagnitude", collision.relativeVelocity.magnitude);

                landingCollisionRunning = true;
                StartCoroutine(ResetLandingBool());
            }
        }

        [Header("Wall Run Settings")]
        public RigWeightTarget wallRunRig;
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
        float rightLeftMultiplier;
        float lastWallRunExitTime;

        void StartWallRun(Collision collision)
        {
            if (Time.time - lastWallRunExitTime < wallRunDelay) { return; }

            animator.SetBool("wallRun", true);

            // Determine if wall is to left or right
            if (Vector3.SignedAngle(transform.forward, collision.GetContact(0).point - transform.position, Vector3.up) > 0) // Wall is on our right
            {
                // Variable assignments to use in Update()
                legTarget = rightLegTarget;
                handTarget = rightHandTarget;
                rightLeftMultiplier = 1;
                
                // Activate IK Rigs
                legTarget.GetComponentInParent<Rig>().weight = 1;
                handTarget.GetComponentInParent<RigWeightTarget>().weightTarget = 1;
                handTarget.GetComponent<FollowTarget>().move = false;
                handTarget.GetComponent<FollowTarget>().rotate = false;
                GetComponentInChildren<RootMotionManager>().disableRightHand = true;
            }
            else // Wall is on our left
            {
                // Variable assignments to use in Update()
                legTarget = leftLegTarget;
                handTarget = leftHandTarget;
                rightLeftMultiplier = -1;

                // Activate IK Rigs
                legTarget.GetComponentInParent<Rig>().weight = 1;
                handTarget.GetComponentInParent<RigWeightTarget>().weightTarget = 1;
                handTarget.GetComponent<FollowTarget>().move = false;
                handTarget.GetComponent<FollowTarget>().rotate = false;
                GetComponentInChildren<RootMotionManager>().disableLeftHand = true;
            }

            rb.useGravity = false;
            animator.SetBool("falling", false);
            ConstantForce wallForce = gameObject.AddComponent<ConstantForce>();
            wallForce.relativeForce = new Vector3(50 * rightLeftMultiplier, 0, 0);
            wallRunRig.weightTarget = 1;
            PlayerController playerController;
            if (TryGetComponent(out playerController))
            {
                playerController.disableLeanInput = true;
                playerController.SetLean(0);
            }
        }

        void EndWallRun()
        {
            if (!animator.GetBool("wallRun")) { return; }

            lastWallRunExitTime = Time.time;
            animator.SetBool("wallRun", false);

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

            legTarget = null;
            handTarget = null;
            rootRotationConstraint.localRotation = Quaternion.Euler(0, 0, 0);
            wallRunRig.weightTarget = 0;
            PlayerController playerController;
            if (TryGetComponent(out playerController))
            {
                playerController.disableLeanInput = false;
            }
            rightLeftMultiplier = 0;
        }

        private IEnumerator ResetLandingBool()
        {
            yield return new WaitUntil(() => !(IsAirborne() | IsJumping()) & !IsLanding());
            landingCollisionRunning = false;
            animator.SetBool("breakfallRoll", false);
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
