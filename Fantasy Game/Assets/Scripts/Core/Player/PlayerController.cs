using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using UnityEngine.Animations.Rigging;
using LightPat.ProceduralAnimations;

namespace LightPat.Core.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Animation Settings")]
        public float moveTransitionSpeed;
        public float animatorSpeed = 1;
        public float idleLoopTransitionTime = 10;

        Animator animator;
        AnimationLayerWeightManager weightManager;

        void OnEscape()
        {
            disableLookInput = !disableLookInput;
            //GetComponent<Rigidbody>().AddForce(new Vector3(5, 0, 0), ForceMode.VelocityChange);
            //animator.SetTrigger("Test");
        }

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            weightManager = GetComponentInChildren<AnimationLayerWeightManager>();
        }

        // Simple stair walking
        private void OnCollisionStay(Collision collision)
        {
            if (collision.transform.CompareTag("Stairs") & moveInput != Vector2.zero)
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

        [HideInInspector] public Vector2 moveInput;
        bool disableMoveInput;
        void OnMove(InputValue value)
        {
            if (disableMoveInput) { return; }
            moveInput = value.Get<Vector2>();
            if (moveInput.y <= 0 & sprinting) { sprintTarget = 2; }
            if (moveInput == Vector2.zero & sprinting) { sprinting = false; }
        }

        [Header("Mouse Look Settings")]
        public float sensitivity;
        public float bodyRotationSpeed;
        public float mouseUpXRotLimit;
        public float mouseDownXRotLimit;
        public bool disableLookInput;
        public bool disableCameraLookInput;
        [HideInInspector] public float rotationX;
        [HideInInspector] public float rotationY;
        [HideInInspector] public Vector2 lookInput;
        Vector3 bodyRotation;

        void OnLook(InputValue value)
        {
            if (disableLookInput) { return; }
            lookInput = value.Get<Vector2>();

            rotationY += sensitivity * lookInput.x;
            if (!disableCameraLookInput)
            {
                rotationX -= sensitivity * lookInput.y;
                rotationX = Mathf.Clamp(rotationX, mouseUpXRotLimit, mouseDownXRotLimit);
                Camera.main.transform.eulerAngles = new Vector3(rotationX, rotationY, 0);
            }

            if (freeLooking) { return; }

            if (disableCameraLookInput)
            {
                bodyRotation = new Vector3(transform.eulerAngles.x, rotationY + lookInput.x * sensitivity, transform.eulerAngles.z);
            }
            else if (rotationX <= 90)
            {
                bodyRotation = new Vector3(transform.eulerAngles.x, Camera.main.transform.eulerAngles.y + lookInput.x * sensitivity, transform.eulerAngles.z);
            }
        }

        private void Update()
        {
            animator.speed = animatorSpeed;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(bodyRotation), Time.deltaTime * bodyRotationSpeed);

            float xTarget = moveInput.x;
            if (sprinting) { xTarget *= sprintTarget; }
            float x = Mathf.Lerp(animator.GetFloat("x"), xTarget, Time.deltaTime * moveTransitionSpeed);
            animator.SetFloat("x", x);

            float yTarget = moveInput.y;
            if (sprinting) { yTarget *= sprintTarget; }
            float y = Mathf.Lerp(animator.GetFloat("y"), yTarget, Time.deltaTime * moveTransitionSpeed);
            animator.SetFloat("y", y);

            if (moveInput == Vector2.zero)
            {
                // Only change idle time if we are at rest
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
                {
                    // This is used so that some states that don't have exit transitions can "remember" that the user moved during their playtime, also so that crouching and jumping is not considered "idle"
                    if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Idle Loop")).IsTag("PauseIdleTime"))
                    {
                        animator.SetFloat("idleTime", animator.GetFloat("idleTime") + Time.deltaTime);
                    }
                }

                // Only change Idle Loop layer weight if idleTime is greater than 10 and we have no moveInput
                if (animator.GetFloat("idleTime") > idleLoopTransitionTime)
                    weightManager.SetLayerWeight("Idle Loop", 1);
            }
            else // If moveInput is not Vector2.zero
            {
                animator.SetFloat("idleTime", 0);
            }

            // Change the weight of the idle Loop once we have exited whatever idle animation we were in
            if (animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Idle Loop")).IsName("Not Idle Looping"))
                weightManager.SetLayerWeight("Idle Loop", 0);

            // Don't want to enter idle loop while crouching
            if (crouching)
            {
                animator.SetFloat("idleTime", 0);
            }

            // If we jump set idleTime to 0
            if (animator.GetBool("jumping")) { animator.SetFloat("idleTime", 0); }
        }

        [Header("NOT FUNCTIONAL YET")]
        public bool toggleSprint;
        bool sprinting;
        float sprintTarget;
        void OnSprint(InputValue value)
        {
            if (value.isPressed & moveInput != Vector2.zero)
            {
                sprinting = !sprinting;
                sprintTarget = 2;
                ascending = true;
            }
        }

        bool ascending = true;
        void OnScaleSprint()
        {
            if (sprinting & !crouching & moveInput.y > 0)
            {
                if (ascending)
                {
                    sprintTarget += 1;
                    if (sprintTarget == 4)
                    {
                        ascending = false;
                    }
                }
                else
                {
                    sprintTarget -= 1;
                    if (sprintTarget == 2)
                    {
                        ascending = true;
                    }
                }
            }
        }

        public bool toggleCrouch;
        bool crouching;
        void OnCrouch(InputValue value)
        {
            if (value.isPressed)
            {
                if (sprinting & weightManager.GetLayerWeight(animator.GetLayerIndex("Crouching")) == 0)
                {
                    animator.SetBool("sliding", true);
                    StartCoroutine(ResetSlide());
                    return;
                }

                crouching = !crouching;
                if (crouching)
                {
                    weightManager.SetLayerWeight("Crouching", 1);
                }
                else
                {
                    weightManager.SetLayerWeight("Crouching", 0);
                }
            }
        }

        private IEnumerator ResetSlide()
        {
            yield return null;
            animator.SetBool("sliding", false);
        }

        bool freeLooking;
        void OnFreeLook(InputValue value)
        {
            freeLooking = value.isPressed;
        }

        [Header("Grab Weapon Settings")]
        public float reach;
        public float reachSpeed;
        public Rig armsRig;
        public Transform rightHandTarget;
        public Transform leftHandTarget;
        void OnInteract()
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, reach))
            {
                if (hit.transform.GetComponent<Interactable>())
                {
                    hit.transform.GetComponent<Interactable>().Invoke();
                }
                else if (hit.transform.GetComponent<Weapon>())
                {
                    //if (equippedWeapon != null) { equippedWeapon.SetActive(false); }

                    //StartCoroutine(PickUpWeapon(hit.transform.Find("ref_right_hand_grip")));
                    armsRig.GetComponent<RigWeightTarget>().weightSpeed = reachSpeed;
                    armsRig.GetComponent<RigWeightTarget>().weightTarget = 1;

                    rightHandTarget.GetComponent<FollowTarget>().target = hit.transform.Find("ref_right_hand_grip");
                }
            }
        }

        /*
        GameObject equippedWeapon = null;
        private IEnumerator PickUpWeapon(Transform target)
        {
            equippedWeapon = target.parent.gameObject;
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
            //rightTarget.GetComponent<MirrorTarget>().move = false;
            //rightTarget.GetComponent<MirrorTarget>().rotate = false;
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

            // Deactivate IK
            float weightProgress = 1;
            while (weightProgress > 0)
            {
                weightProgress -= Time.deltaTime * reachSpeed;
                armsRig.weight = weightProgress;
                //animator.SetLayerWeight(animator.GetLayerIndex(target.GetComponentInParent<Weapon>().weaponClass), 1 - weightProgress);
                yield return null;
            }

            //rightTarget.GetComponent<MirrorTarget>().move = true;
            //rightTarget.GetComponent<MirrorTarget>().rotate = true;

            disableMoveInput = false;
            disableLookInput = false;
        }*/
    }
}
