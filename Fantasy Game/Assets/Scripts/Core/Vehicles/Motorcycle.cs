using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class Motorcycle : Vehicle
    {
        public Vector3 velocityLimits;
        public float forceClampMultiplier;

        NetworkObject driver;
        Rigidbody rb;
        Vector3 currentVelocityLimits;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            currentVelocityLimits = velocityLimits;
        }

        private void FixedUpdate()
        {
            if (!driver) { return; }

            Vector3 moveForce = new Vector3(moveInput.x, 0, moveInput.y);
            Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

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
            moveForce = Vector3.ClampMagnitude(moveForce, forceClampMultiplier);
            rb.AddRelativeForce(moveForce, ForceMode.VelocityChange);
        }

        public override void OnDriverEnter(ulong networkObjectId)
        {
            if (!IsServer) { Debug.LogWarning("Calling OnDriverEnter from a client"); return; }
            driver = NetworkManager.SpawnManager.SpawnedObjects[networkObjectId];
            NetworkObject.ChangeOwnership(driver.OwnerClientId);
            driver.SendMessage("OnDriverEnter", this);
        }

        public override void OnDriverExit()
        {
            if (!IsServer) { Debug.LogWarning("Calling OnDriverExit from a client"); return; }

            NetworkObject.RemoveOwnership();
            driver.SendMessage("OnDriverExit");
            driver = null;
        }

        Vector2 moveInput;
        protected override void OnVehicleMove(Vector2 newMoveInput)
        {
            moveInput = newMoveInput;
        }

        protected override void OnVehicleLook(Vector2 newLookInput)
        {
            transform.Rotate(new Vector3(0, newLookInput.x, 0));
        }

        protected override void OnVehicleJump(bool pressed)
        {
            Debug.Log("OnVehicleJump");
        }

        protected override void OnVehicleCrouch(bool pressed)
        {
            Debug.Log("OnVehicleCrouch");
        }

        protected override void OnVehicleSprint(bool pressed)
        {
            Debug.Log("OnVehicleSprint");
        }
    }
}