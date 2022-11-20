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

        float currentRotorSpeed;
        Animator driver;
        Rigidbody rb;
        ConstantForce antiGravity;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            antiGravity = gameObject.AddComponent<ConstantForce>();
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

            if (driver)
            {
                Vector3 targetRotation = new Vector3(0, 180, 0);
                targetRotation.z = moveInput.x * 20;
                targetRotation.x = -moveInput.y * 20;
                transform.GetChild(0).localRotation = Quaternion.Slerp(transform.GetChild(0).localRotation, Quaternion.Euler(targetRotation), Time.deltaTime * 2);
            }
        }

        float verticalForceAmount;
        private void FixedUpdate()
        {
            if (!driver) { return; }

            Vector3 moveForce = new Vector3(moveInput.x, 0, moveInput.y);
            Vector3 velocityLimits = new Vector3(15, 10, 15);
            Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

            if (localVelocity.x > velocityLimits.x)
                moveForce.x -= localVelocity.x - velocityLimits.x;
            if (localVelocity.x < -velocityLimits.x)
                moveForce.x -= localVelocity.x + velocityLimits.x;
            if (localVelocity.z > velocityLimits.z)
                moveForce.z -= localVelocity.z - velocityLimits.z;
            if (localVelocity.z < -velocityLimits.z)
                moveForce.z -= localVelocity.z + velocityLimits.z;
            rb.AddRelativeForce(moveForce, ForceMode.VelocityChange);

            Vector3 verticalForce = new Vector3(0, verticalForceAmount, 0);
            if (rb.velocity.y > velocityLimits.y)
                verticalForce.y -= rb.velocity.y - velocityLimits.y;
            else if (rb.velocity.y < -velocityLimits.y)
                verticalForce.y -= rb.velocity.y + velocityLimits.y;
            rb.AddForce(verticalForce, ForceMode.VelocityChange);

            Debug.Log(localVelocity);
        }

        void OnDriverEnter(Animator newDriver)
        {
            driver = newDriver;
            engineStarted = true;
            vehicleCamera.depth = 1;
        }

        void OnDriverExit()
        {
            driver = null;
            engineStarted = false;
            vehicleCamera.depth = -1;
        }

        Vector2 moveInput;
        void OnVehicleMove(Vector2 newMoveInput)
        {
            moveInput = newMoveInput;
        }

        void OnVehicleLook(Vector2 newLookInput)
        {
            rb.AddTorque(new Vector3(0, newLookInput.x * 0.5f, 0), ForceMode.VelocityChange);
        }

        bool jumping;
        void OnVehicleJump(bool pressed)
        {
            jumping = pressed;
            if (jumping)
                verticalForceAmount = 1;
            else if (!crouching)
                verticalForceAmount = 0;
        }
        
        bool crouching;
        void OnVehicleCrouch(bool pressed)
        {
            crouching = pressed;
            if (crouching)
                verticalForceAmount = -1;
            else if (!jumping)
                verticalForceAmount = 0;
        }

        bool sprinting;
        void OnVehicleSprint(bool pressed)
        {
            sprinting = pressed;
        }
    }
}