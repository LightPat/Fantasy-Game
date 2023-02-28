using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class Motorcycle : Vehicle
    {
        [Header("Wheel Settings")]
        public float power = 15000;
        public float brakePower = 1000;
        [Header("Motorcycle Specific")]
        public float forceClampMultiplier;
        public float maxHandleBarRotation;
        public Transform handlebars;
        public Transform rearSuspension;

        NetworkObject driver;
        float handleBarRotation;
        Quaternion originalHandleBarRotation;

        Wheel[] wheels;

        private void Start()
        {
            originalHandleBarRotation = handlebars.localRotation;
            wheels = GetComponentsInChildren<Wheel>();
        }

        private void FixedUpdate()
        {
            float steerAngle = -Vector3.SignedAngle(handlebars.up, transform.forward, transform.up);

            foreach (Wheel w in wheels)
            {
                w.Steer(steerAngle);
                w.Accelerate(moveInput.y * power);
                w.Brake(jumping & driver ? brakePower : 0);
                w.UpdatePosition();
            }
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
            handleBarRotation += newLookInput.x;
            if (handleBarRotation > maxHandleBarRotation) { handleBarRotation = maxHandleBarRotation; }
            if (handleBarRotation < -maxHandleBarRotation) { handleBarRotation = -maxHandleBarRotation; }
            handlebars.localRotation = originalHandleBarRotation * Quaternion.Euler(0, 0, handleBarRotation);
        }

        bool jumping;
        protected override void OnVehicleJump(bool pressed)
        {
            jumping = pressed;
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