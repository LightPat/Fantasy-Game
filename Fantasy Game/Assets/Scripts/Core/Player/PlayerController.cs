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
        [Header("Animation Settings")]
        public float moveTransitionSpeed;
        public float animatorSpeed = 1;

        Animator animator;
        Rigidbody rb;

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            rb = GetComponent<Rigidbody>();
            prevRotationState = !rotateBodyWithCamera;
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
        Vector2 lookInput;
        Vector3 bodyRotation;
        public Transform arrow;
        public float arrowSpeed;
        public float arrowAngle;
        float prevArrowAngle;
        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>();

            //if (!animator.GetBool("attack1") & !animator.GetBool("attack2"))
            //{
            //    if (lookInput.x != 0)
            //        animator.SetFloat("lookInputX", lookInput.x);
            //    if (lookInput.y != 0)
            //        animator.SetFloat("lookInputY", lookInput.y);
            //}

            if (lookInput != Vector2.zero)
            {
                if (!animator.GetBool("attack1") & !animator.GetBool("attack2"))
                {
                    arrowAngle = Mathf.Atan2(lookInput.x, lookInput.y) * Mathf.Rad2Deg;
                }

                if (prevArrowAngle < 0 & arrowAngle == 180)
                {
                    arrowAngle *= -1;
                }

                animator.SetFloat("arrowAngle", arrowAngle);
                prevArrowAngle = arrowAngle;
            }

            if (disableLookInput) { return; }
            lookInput *= sensitivity * Time.timeScale;

            // Body Rotation Logic (Rotation Around Y Axis)
            bodyRotation = new Vector3(transform.eulerAngles.x, bodyRotation.y + lookInput.x, transform.eulerAngles.z);
            if (rotateBodyWithCamera)
                rb.MoveRotation(Quaternion.Euler(bodyRotation));

            // Camera Rotation Logic (Rotation Around X Axis)
            if (disableCameraLookInput) { return; }
            Transform camTransform = Camera.main.transform;
            camTransform.Rotate(new Vector3(-lookInput.y, 0, 0), Space.Self);
            float attemptedXAngle;
            if (!rotateBodyWithCamera)
            {
                camTransform.localEulerAngles = new Vector3(camTransform.localEulerAngles.x, camTransform.localEulerAngles.y + lookInput.x, camTransform.localEulerAngles.z);
                attemptedXAngle = Vector3.Angle(Quaternion.Euler(bodyRotation) * Vector3.forward, camTransform.forward);

                if (camTransform.forward.y > 0)
                {
                    attemptedXAngle *= -1;
                }

                if (attemptedXAngle > mouseDownXRotLimit)
                {
                    camTransform.localEulerAngles = new Vector3(mouseDownXRotLimit, bodyRotation.y, 0);
                }
                else if (attemptedXAngle < mouseUpXRotLimit)
                {
                    camTransform.localEulerAngles = new Vector3(mouseUpXRotLimit, bodyRotation.y, 0);
                }
            }
            else
            {
                attemptedXAngle = Vector3.Angle(transform.forward, camTransform.forward);

                if (camTransform.forward.y > 0)
                {
                    attemptedXAngle *= -1;
                }

                if (attemptedXAngle > mouseDownXRotLimit)
                {
                    camTransform.localEulerAngles = new Vector3(mouseDownXRotLimit, 0, 0);
                }
                else if (attemptedXAngle < mouseUpXRotLimit)
                {
                    camTransform.localEulerAngles = new Vector3(mouseUpXRotLimit, 0, 0);
                }
            }
        }

        bool prevRotationState;
        private void Update()
        {
            arrow.rotation = Quaternion.Slerp(arrow.rotation, Quaternion.Euler(new Vector3(0,0,-arrowAngle)), arrowSpeed * Time.deltaTime);

            float xTarget = moveInput.x;
            if (running) { xTarget *= runTarget; }
            float x = Mathf.Lerp(animator.GetFloat("moveInputX"), xTarget, Time.deltaTime * moveTransitionSpeed);
            animator.SetFloat("moveInputX", x);

            float yTarget = moveInput.y;
            if (running) { yTarget *= runTarget; }
            float y = Mathf.Lerp(animator.GetFloat("moveInputY"), yTarget, Time.deltaTime * moveTransitionSpeed);
            animator.SetFloat("moveInputY", y);

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

            if (!rotateBodyWithCamera & prevRotationState)
            {
                Camera.main.transform.SetParent(null);
            }
            else if (rotateBodyWithCamera & !prevRotationState)
            {
                transform.rotation = Quaternion.Euler(bodyRotation);
                Camera.main.transform.SetParent(transform);
            }

            if (!rotateBodyWithCamera)
                rb.MoveRotation(Quaternion.Slerp(transform.rotation, Quaternion.Euler(bodyRotation), Time.deltaTime * bodyRotationSpeed));

            prevRotationState = rotateBodyWithCamera;
        }

        void OnAbility()
        {
            if (Time.timeScale == 1)
            {
                StartCoroutine(TimeScaleAbility());
            }
        }

        private IEnumerator TimeScaleAbility()
        {
            Time.timeScale = 0.1f;

            for (float i = 0.2f; i <= 1.1; i+=0.1f)
            {
                yield return new WaitForSeconds(0.3f);
                Time.timeScale = Mathf.Round(i * 100) / 100;
            }
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
                    hit.transform.GetComponent<Interactable>().Invoke(gameObject);
                }
                break;
            }
        }
    }
}
