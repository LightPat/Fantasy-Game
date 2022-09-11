using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using LightPat.Util;

namespace LightPat.Core.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Used in player camera follow")]
        public RigWeightTarget neckAimRig;
        public RigWeightTarget spineAimRig;
        [Header("Animation Settings")]
        public float moveTransitionSpeed;
        public float animatorSpeed = 1;

        Animator animator;
        Rigidbody rb;

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            rb = GetComponent<Rigidbody>();
        }

        // Teleportation stair walking
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

        Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
            if (moveInput.y <= 0 & running) { runTarget = 2; }
        }

        [Header("Mouse Look Settings")]
        public float sensitivity;
        public float bodyRotationSpeed;
        public float mouseUpXRotLimit;
        public float mouseDownXRotLimit;
        public bool disableLookInput;
        public bool disableCameraLookInput;
        public bool rotateBodyWithCamera;
        [HideInInspector] public float rotationX;
        [HideInInspector] public float rotationY;
        Vector2 lookInput;
        Vector3 bodyRotation;

        void OnLook(InputValue value)
        {
            if (disableLookInput) { return; }
            lookInput = value.Get<Vector2>();

            rotationY += lookInput.x * sensitivity * Time.timeScale;
            if (!disableCameraLookInput)
            {
                rotationX -= lookInput.y * sensitivity * Time.timeScale;
                rotationX = Mathf.Clamp(rotationX, mouseUpXRotLimit, mouseDownXRotLimit);
                Camera.main.transform.eulerAngles = new Vector3(rotationX, rotationY, Camera.main.transform.eulerAngles.z);
            }

            bodyRotation = new Vector3(transform.eulerAngles.x, rotationY + lookInput.x * sensitivity, transform.eulerAngles.z);

            if (rotateBodyWithCamera)
                rb.MoveRotation(Quaternion.Euler(bodyRotation));
        }

        private void Update()
        {
            float xTarget = moveInput.x;
            if (running) { xTarget *= runTarget; }
            float x = Mathf.Lerp(animator.GetFloat("x"), xTarget, Time.deltaTime * moveTransitionSpeed);
            animator.SetFloat("x", x);

            float yTarget = moveInput.y;
            if (running) { yTarget *= runTarget; }
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
            }
            else // If moveInput is not Vector2.zero
            {
                animator.SetFloat("idleTime", 0);
            }

            // Don't want to enter idle loop while crouching
            if (crouching)
            {
                animator.SetFloat("idleTime", 0);
            }

            // If we jump set idleTime to 0
            if (animator.GetBool("jumping")) { animator.SetFloat("idleTime", 0); }

            animator.speed = animatorSpeed;
        }

        private void LateUpdate()
        {
            if (!rotateBodyWithCamera)
                rb.MoveRotation(Quaternion.Slerp(transform.rotation, Quaternion.Euler(bodyRotation), Time.deltaTime * bodyRotationSpeed));
        }

        void OnAbility()
        {
            if (Time.timeScale == 1)
            {
                Time.timeScale = 0.5f;
            }
            else
            {
                Time.timeScale = 1;
            }

            //rb.AddForce(transform.forward * 50f, ForceMode.VelocityChange);
            //disableLookInput = !disableLookInput;
        }

        [Header("Misc Settings")]
        public bool toggleSprint;
        bool running;
        float runTarget;
        void OnSprint(InputValue value)
        {
            if (toggleSprint)
            {
                if (value.isPressed)
                {
                    running = !running;
                    animator.SetBool("running", running);
                    runTarget = 2;
                    ascending = true;

                    if (!running)
                    {
                        animator.SetBool("sprinting", false);
                    }
                }
            }
            else
            {
                running = value.isPressed;
                animator.SetBool("running", running);
                runTarget = 2;
                ascending = true;

                if (!value.isPressed)
                {
                    animator.SetBool("sprinting", false);
                }
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
                    {
                        animator.SetBool("sprinting", true);
                    }
                    else
                    {
                        animator.SetBool("sprinting", false);
                    }

                    if (runTarget == 4)
                    {
                        ascending = false;
                    }
                }
                else
                {
                    runTarget -= 1;
                    if (runTarget == 2)
                    {
                        ascending = true;
                    }

                    if (runTarget == 4)
                    {
                        animator.SetBool("sprinting", true);
                    }
                    else
                    {
                        animator.SetBool("sprinting", false);
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
                if (running)
                {
                    animator.SetBool("crouching", true);
                    StartCoroutine(ResetSlide());
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

        private IEnumerator ResetSlide()
        {
            yield return null;
            animator.SetBool("crouching", false);
        }

        [Header("Interact Settings")]
        public float reach;
        void OnInteract()
        {
            RaycastHit[] allHits = Physics.RaycastAll(Camera.main.transform.position, Camera.main.transform.forward);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform == transform)
                {
                    continue;
                }

                if (hit.transform.GetComponent<Interactable>())
                {
                    hit.transform.GetComponent<Interactable>().Invoke();
                }
                break;
            }
        }
    }
}
