using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class Helicopter : Vehicle
    {
        [Header("Helicopter Specific")]
        public Camera vehicleCamera;
        public Transform mainRotor;
        public Transform tailRotor;
        public float mainRotorSpeed;
        public float tailRotorSpeed;
        public bool engineStarted;
        [Header("Locomotion")]
        public Vector3 velocityLimits;
        public Vector3 sprintVelocityLimits;
        public float forceClampMultiplier;
        public float rotationSpeed;

        float currentRotorSpeed;
        NetworkObject driver;
        Rigidbody rb;
        ConstantForce antiGravity;
        Vector3 prevPosition;
        Vector3 currentVelocityLimits;
        Vector3 originalCameraPositionOffset;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                GetComponent<NestedNetworkObject>().NestedSpawn();
        }

        private void OnTransformChildrenChanged()
        {
            if (transform.childCount < 2) { return; }
            mainRotor = transform.GetChild(1).Find("mainRotor");
            tailRotor = transform.GetChild(1).Find("tailRotor");
        }

        private void Awake()
        {
            motorSoundSource.volume = 0;
            motorSoundSource.Play();
        }

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            antiGravity = gameObject.AddComponent<ConstantForce>();
            bodyRotation = transform.rotation;
            prevPosition = transform.position;
            currentVelocityLimits = velocityLimits;
            originalCameraPositionOffset = vehicleCamera.transform.localPosition;
            vehicleCamera.transform.SetParent(null, true);
            vehicleCamera.transform.LookAt(transform.position);
        }

        [Header("Audio Settings")]
        public AudioSource motorSoundSource;

        private void Update()
        {
            if (!mainRotor | !tailRotor) { return; }

            // Rotate rotors
            if (engineStarted)
                currentRotorSpeed = Mathf.MoveTowards(currentRotorSpeed, sprinting ? 2 : 1, Time.deltaTime / 4);
            else
                currentRotorSpeed = Mathf.MoveTowards(currentRotorSpeed, 0, Time.deltaTime / 4);
            mainRotor.Rotate(0, 0, mainRotorSpeed * Time.deltaTime * currentRotorSpeed, Space.Self);
            tailRotor.Rotate(tailRotorSpeed * Time.deltaTime * currentRotorSpeed, 0, 0, Space.Self);

            antiGravity.force = new Vector3(0, -Physics.gravity.y * currentRotorSpeed * rb.mass, 0);

            vehicleCamera.transform.position += transform.position - prevPosition;
            prevPosition = transform.position;

            motorSoundSource.pitch = currentRotorSpeed;
            if (motorSoundSource.pitch < 0.1f)
                motorSoundSource.volume = Mathf.MoveTowards(motorSoundSource.volume, 0, Time.deltaTime * 5);
            else
                motorSoundSource.volume = Mathf.MoveTowards(motorSoundSource.volume, 1, Time.deltaTime * 5);

            if (driver)
            {
                if (!IsGrounded())
                {
                    rotationOffset.z = moveInput.x * currentVelocityLimits.z;
                    rotationOffset.x = -moveInput.y * currentVelocityLimits.x;
                    rb.MoveRotation(Quaternion.Slerp(transform.rotation, bodyRotation * Quaternion.Inverse(Quaternion.Euler(rotationOffset)), Time.deltaTime * 2));
                }
            }
        }

        [Header("Physics Settings")]
        public float dampenFactor = 0.8f;

        Vector3 rotationOffset;
        float verticalForceAmount;
        private void FixedUpdate()
        {
            if (!driver) { return; }

            Vector3 moveForce = new Vector3(moveInput.x, 0, moveInput.y);
            Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

            if (IsGrounded())
            {
                rb.AddTorque(-rb.angularVelocity * dampenFactor, ForceMode.Acceleration);
                rb.AddRelativeForce(-rb.velocity * dampenFactor, ForceMode.Acceleration);

                moveForce.x = 0 - localVelocity.x;
                moveForce.z = 0 - localVelocity.z;
                moveForce = Vector3.ClampMagnitude(moveForce, currentRotorSpeed * forceClampMultiplier);
                rb.AddRelativeForce(moveForce, ForceMode.VelocityChange);
            }
            else
            {
                // Move vehicle horizontally
                if (localVelocity.x > currentVelocityLimits.x)
                    moveForce.x -= localVelocity.x - currentVelocityLimits.x;
                if (localVelocity.x < -currentVelocityLimits.x)
                    moveForce.x -= localVelocity.x + currentVelocityLimits.x;
                if (localVelocity.z > currentVelocityLimits.z)
                    moveForce.z -= localVelocity.z - currentVelocityLimits.z;
                if (localVelocity.z < -currentVelocityLimits.z)
                    moveForce.z -= localVelocity.z + currentVelocityLimits.z;
                if (moveInput == Vector2.zero)
                {
                    moveForce.x = 0 - localVelocity.x;
                    moveForce.z = 0 - localVelocity.z;
                }
                moveForce = Vector3.ClampMagnitude(moveForce, currentRotorSpeed * forceClampMultiplier);
                rb.AddRelativeForce(moveForce, ForceMode.VelocityChange);
            }

            // Move vehicle up and down in the air
            Vector3 verticalForce = new Vector3(0, verticalForceAmount, 0);
            if (rb.velocity.y > currentVelocityLimits.y)
                verticalForce.y -= rb.velocity.y - currentVelocityLimits.y;
            else if (rb.velocity.y < -currentVelocityLimits.y)
                verticalForce.y -= rb.velocity.y + currentVelocityLimits.y;
            else if (verticalForce == Vector3.zero)
                verticalForce.y = 0 - rb.velocity.y;
            verticalForce = Vector3.ClampMagnitude(verticalForce, currentRotorSpeed * forceClampMultiplier);
            rb.AddForce(verticalForce, ForceMode.VelocityChange);
        }

        public override void OnDriverEnter(ulong networkObjectId)
        {
            if (!IsServer) { Debug.LogWarning("Calling OnDriverEnter from a client"); return; }

            driver = NetworkManager.SpawnManager.SpawnedObjects[networkObjectId];
            engineStarted = true;

            NetworkObject.ChangeOwnership(driver.OwnerClientId);
            transform.GetChild(1).GetComponent<NestedNetworkObject>().NetworkObject.ChangeOwnership(driver.OwnerClientId);

            rb.constraints = RigidbodyConstraints.FreezeRotation;
            driver.SendMessage("OnDriverEnter", this);
            OnDriverEnterClientRpc(networkObjectId);
        }

        [ClientRpc]
        void OnDriverEnterClientRpc(ulong networkObjectId)
        {
            driver = NetworkManager.SpawnManager.SpawnedObjects[networkObjectId];
            engineStarted = true;

            if (driver.IsLocalPlayer)
            {
                vehicleCamera.depth = 1;
                vehicleCamera.enabled = true;
                vehicleCamera.transform.position = transform.position + originalCameraPositionOffset;
                vehicleCamera.transform.LookAt(transform.position);
            }

            rb.constraints = RigidbodyConstraints.FreezeRotation;
            driver.SendMessage("OnDriverEnter", this);
        }

        public override void OnDriverExit()
        {
            if (!IsServer) { Debug.LogWarning("Calling OnDriverExit from a client"); return; }

            NetworkObject.RemoveOwnership();
            transform.GetChild(1).GetComponent<NestedNetworkObject>().NetworkObject.RemoveOwnership();

            rb.constraints = RigidbodyConstraints.None;
            driver.SendMessage("OnDriverExit");

            OnDriverExitClientRpc();

            engineStarted = false;
            driver = null;
        }

        [ClientRpc]
        void OnDriverExitClientRpc()
        {
            if (driver.IsLocalPlayer)
            {
                vehicleCamera.depth = -1;
                vehicleCamera.enabled = false;
            }

            rb.constraints = RigidbodyConstraints.None;
            driver.SendMessage("OnDriverExit");

            engineStarted = false;
            driver = null;
        }

        Vector2 moveInput;
        protected override void OnVehicleMove(Vector2 newMoveInput)
        {
            moveInput = newMoveInput;
        }

        Quaternion bodyRotation;
        protected override void OnVehicleLook(Vector2 newLookInput)
        {
            vehicleCamera.transform.RotateAround(transform.position, transform.up, newLookInput.x);
            vehicleCamera.transform.RotateAround(transform.position, transform.right, newLookInput.y);
            vehicleCamera.transform.LookAt(transform.position);
            Vector3 targetPoint = new Vector3(transform.position.x, vehicleCamera.transform.position.y, transform.position.z);
            bodyRotation = Quaternion.LookRotation(targetPoint - vehicleCamera.transform.position, Vector3.up);
        }

        bool jumping;
        protected override void OnVehicleJump(bool pressed)
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
        protected override void OnVehicleCrouch(bool pressed)
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
        protected override void OnVehicleSprint(bool pressed)
        {
            sprinting = pressed;
            if (sprinting)
                currentVelocityLimits = sprintVelocityLimits;
            else
                currentVelocityLimits = velocityLimits;
        }

        bool IsGrounded()
        {
            return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 2);
        }
    }
}