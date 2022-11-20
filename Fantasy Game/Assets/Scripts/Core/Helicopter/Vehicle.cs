using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Vehicle : MonoBehaviour
    {
        public Transform passengerSeat;
        public Transform mainRotor;
        public Transform tailRotor;
        public float mainRotorSpeed;
        public float tailRotorSpeed;
        public bool engineStarted;
        public Camera vehicleCamera;
        public bool grounded;

        float currentRotorSpeed;
        GameObject driver;
        Rigidbody rb;
        ConstantForce antiGravity;
        Vector3 prevPosition;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            antiGravity = gameObject.AddComponent<ConstantForce>();
            bodyRotation = transform.rotation;
            prevPosition = transform.position;
        }

        private void Update()
        {
            // Rotate rotors
            if (engineStarted)
                currentRotorSpeed = Mathf.MoveTowards(currentRotorSpeed, 1, Time.deltaTime / 4);
            else
                currentRotorSpeed = Mathf.Lerp(currentRotorSpeed, 0, Time.deltaTime / 8);
            mainRotor.Rotate(0, 0, mainRotorSpeed * Time.deltaTime * currentRotorSpeed, Space.Self);
            tailRotor.Rotate(tailRotorSpeed * Time.deltaTime * currentRotorSpeed, 0, 0, Space.Self);

            antiGravity.force = new Vector3(0, -Physics.gravity.y * currentRotorSpeed * rb.mass, 0);

            if (!vehicleCamera.transform.parent)
                vehicleCamera.transform.position += transform.position - prevPosition;
            prevPosition = transform.position;

            if (IsGrounded())
            {
                transform.GetChild(0).localRotation = Quaternion.Slerp(transform.GetChild(0).localRotation, Quaternion.Euler(new Vector3(0, 180, 0)), Time.deltaTime * 2);
            }
            else
            {
                rb.MoveRotation(Quaternion.Slerp(transform.rotation, bodyRotation, Time.deltaTime * 2));
                if (driver)
                {
                    Vector3 targetRotation = new Vector3(0, 180, 0);
                    targetRotation.z = moveInput.x * 20;
                    targetRotation.x = -moveInput.y * 20;
                    transform.GetChild(0).localRotation = Quaternion.Slerp(transform.GetChild(0).localRotation, Quaternion.Euler(targetRotation), Time.deltaTime * 2);
                }
            }
        }

        float verticalForceAmount;
        Vector3 velocityLimits = new Vector3(15, 10, 15);
        private void FixedUpdate()
        {
            if (!driver) { return; }

            Vector3 moveForce = new Vector3(moveInput.x, 0, moveInput.y);
            Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

            if (IsGrounded())
            {
                moveForce.x = 0 - localVelocity.x;
                moveForce.z = 0 - localVelocity.z;
                moveForce = Vector3.ClampMagnitude(moveForce, currentRotorSpeed / 2);
                rb.AddRelativeForce(moveForce, ForceMode.VelocityChange);
            }
            else
            {
                // Move vehicle horizontally
                if (localVelocity.x > velocityLimits.x)
                    moveForce.x -= localVelocity.x - velocityLimits.x;
                if (localVelocity.x < -velocityLimits.x)
                    moveForce.x -= localVelocity.x + velocityLimits.x;
                if (localVelocity.z > velocityLimits.z)
                    moveForce.z -= localVelocity.z - velocityLimits.z;
                if (localVelocity.z < -velocityLimits.z)
                    moveForce.z -= localVelocity.z + velocityLimits.z;
                if (moveInput == Vector2.zero)
                {
                    moveForce.x = 0 - localVelocity.x;
                    moveForce.z = 0 - localVelocity.z;
                }
                moveForce = Vector3.ClampMagnitude(moveForce, currentRotorSpeed / 2);
                rb.AddRelativeForce(moveForce, ForceMode.VelocityChange);
            }

            // Move vehicle up and down in the air
            Vector3 verticalForce = new Vector3(0, verticalForceAmount, 0);
            if (rb.velocity.y > velocityLimits.y)
                verticalForce.y -= rb.velocity.y - velocityLimits.y;
            else if (rb.velocity.y < -velocityLimits.y)
                verticalForce.y -= rb.velocity.y + velocityLimits.y;
            else if (verticalForce == Vector3.zero)
                verticalForce.y = 0 - rb.velocity.y;
            verticalForce = Vector3.ClampMagnitude(verticalForce, currentRotorSpeed / 2);
            rb.AddForce(verticalForce, ForceMode.VelocityChange);
        }

        Transform previousCameraParent;
        Vector3 originalCameraPosition;
        void OnDriverEnter(GameObject newDriver)
        {
            driver = newDriver;
            engineStarted = true;
            vehicleCamera.depth = 1;
            originalCameraPosition = transform.position;
            vehicleCamera.transform.LookAt(transform.position);
            previousCameraParent = vehicleCamera.transform.parent;
            vehicleCamera.transform.SetParent(null, true);
        }

        void OnDriverExit()
        {
            driver = null;
            engineStarted = false;
            vehicleCamera.depth = -1;
            vehicleCamera.transform.SetParent(previousCameraParent, true);
            vehicleCamera.transform.position = originalCameraPosition;
        }

        Vector2 moveInput;
        void OnVehicleMove(Vector2 newMoveInput)
        {
            moveInput = newMoveInput;
        }

        Quaternion bodyRotation;
        void OnVehicleLook(Vector2 newLookInput)
        {
            vehicleCamera.transform.RotateAround(transform.position, transform.up, newLookInput.x);
            vehicleCamera.transform.RotateAround(transform.position, transform.right, newLookInput.y);
            vehicleCamera.transform.LookAt(transform.position);
            Vector3 targetPoint = new Vector3(transform.position.x, vehicleCamera.transform.position.y, transform.position.z);
            bodyRotation = Quaternion.LookRotation(targetPoint - vehicleCamera.transform.position, Vector3.up);
        }

        bool jumping;
        void OnVehicleJump(bool pressed)
        {
            jumping = pressed;
            if (jumping)
                verticalForceAmount = 1;
            else if (crouching)
                verticalForceAmount = -1;
            else
                verticalForceAmount = 0;
        }
        
        bool crouching;
        void OnVehicleCrouch(bool pressed)
        {
            crouching = pressed;
            if (crouching)
                verticalForceAmount = -1;
            else if (jumping)
                verticalForceAmount = 1;
            else
                verticalForceAmount = 0;
        }

        bool sprinting;
        void OnVehicleSprint(bool pressed)
        {
            sprinting = pressed;
            if (sprinting)
                velocityLimits = new Vector3(30, 10, 30);
            else
                velocityLimits = new Vector3(15, 10, 15);
        }

        bool IsGrounded()
        {
            Debug.DrawRay(transform.position, Vector3.down * 2, Color.black, Time.deltaTime);
            return Physics.Raycast(transform.position, Vector3.down, 2);
        }
    }
}