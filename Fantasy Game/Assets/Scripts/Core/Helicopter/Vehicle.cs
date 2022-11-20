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
        }

        private void FixedUpdate()
        {
            if (!driver) { return; }

            Vector3 moveForce = new Vector3(moveInput.x, 0, moveInput.y);
            Vector3 currentVelocity = rb.velocity;
            float speedLimit = 5;
            if (currentVelocity.x > speedLimit)
            {
                moveForce.x -= currentVelocity.x;
            }
            else if (currentVelocity.x < -speedLimit)
            {
                moveForce.x -= currentVelocity.x;
            }
            if (currentVelocity.z > speedLimit)
            {
                moveForce.z -= currentVelocity.z;
            }
            else if (currentVelocity.z < -speedLimit)
            {
                moveForce.z -= currentVelocity.z;
            }
            rb.AddForce(moveForce, ForceMode.VelocityChange);
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
            rb.AddForce(new Vector3(0, Mathf.Clamp(-newLookInput.y, -5, 5), 0), ForceMode.VelocityChange);
            rb.AddTorque(new Vector3(0, newLookInput.x * 0.5f, 0), ForceMode.VelocityChange);
        }
    }
}