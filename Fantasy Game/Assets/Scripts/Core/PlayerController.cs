using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace LightPat.Core
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(Attributes))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Object Assignments")]
        public Transform verticalRotate;
        public GameObject crosshair;
        public GameObject escapeMenu;
        public GameObject inventoryPrefab;

        private Rigidbody rb;
        private float currentSpeed;
        private AudioSource audioSrc;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            audioSrc = GetComponent<AudioSource>();
            playerInput = GetComponent<PlayerInput>();
            currentSpeed = walkingSpeed;
            Cursor.lockState = CursorLockMode.Locked;
            lookEulers = new Vector3(transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.z);
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

        void FixedUpdate()
        {
            Vector3 moveForce = rb.rotation * new Vector3(moveInput.x, 0, moveInput.y) * currentSpeed;
            moveForce.x -= rb.velocity.x;
            moveForce.z -= rb.velocity.z;
            rb.AddForce(moveForce, ForceMode.VelocityChange);

            if (!audioSrc.isPlaying & rb.velocity.magnitude > 3 & moveInput != Vector2.zero)
            {
                StartCoroutine(playFootstep());
            }

            // Falling Gravity velocity increase
            if (rb.velocity.y < 0)
            {
                rb.AddForce(new Vector3(0, (fallingGravityScale * -1), 0), ForceMode.VelocityChange);
            }
        }

        [Header("Footstep Detection Settings")]
        public float footstepDetectionRadius = 10f;
        private IEnumerator playFootstep()
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

        [Header("Move Settings")]
        public float walkingSpeed = 5f;
        private Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        [Header("Look Settings")]
        public float sensitivity = 1f;
        public float verticalLookBound = 90f;
        private Vector3 lookEulers;
        private Vector2 lookInput;
        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>();
        }

        [Header("Interact Settings")]
        public float reach = 4f;
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
            }
        }

        [Header("Jump Settings")]
        public float jumpHeight = 3f;
        public float fallingGravityScale = 0.5f;
        [Header("IsGrounded Settings")]
        public float checkDistance = 1;
        void OnJump(InputValue value)
        {
            // TODO this isn't really an elegant solution, if you stand on the edge of something it doesn't realize that you are still grouded
            // If you check for velocity = 0 then you can double jump since the apex of your jump's velocity is 0
            // Check if the player is touching a gameObject under them
            // May need to change 1.5f to be a different number if you switch the asset of the player model
            bool isGrounded()
            {
                RaycastHit hit;
                bool bHit = Physics.Raycast(new Vector3(transform.position.x, transform.position.y + 0.1f, transform.position.z), transform.up * -1, out hit, checkDistance);
                return bHit;
            }

            if (value.isPressed)
            {
                if (isGrounded())
                {
                    float jumpForce = Mathf.Sqrt(jumpHeight * -2 * Physics.gravity.y);
                    rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.VelocityChange);
                }
            }
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

        [Header("Sprint Settings")]
        public float sprintSpeed = 100f;
        void OnSprint(InputValue value)
        {
            if (value.isPressed)
            {
                currentSpeed = sprintSpeed;
            }
            else
            {
                currentSpeed = walkingSpeed;
            }
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
                    hit.transform.GetComponent<Attributes>().InflictDamage(attackDamage, gameObject);
                }
            }
        }

        void OnAttacked(GameObject value)
        {
            //Debug.Log("I'm being attacked! " + value);
        }
    }
}
