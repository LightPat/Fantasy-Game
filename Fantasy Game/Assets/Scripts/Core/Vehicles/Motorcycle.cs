using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class Motorcycle : Vehicle
    {
        [Header("Wheel Settings")]
        public float power = 1500;
        public float brakePower = 1500;
        [Header("Motorcycle Specific")]
        public float forceClampMultiplier;
        public float maxHandleBarRotation;
        public Transform handleBars;
        public Transform frontSuspension;
        public Transform frontWheelMesh;
        public WheelCollider frontWheelCollider;
        public Transform rearSuspension;
        public WheelCollider rearWheelCollider;

        Collider passengerSeatCollider;
        Rigidbody rb;
        NetworkObject driver;
        float handleBarRotation;
        Quaternion originalHandleBarRotation;
        Wheel[] wheels;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                GetComponent<NestedNetworkObject>().NestedSpawn();

            StartCoroutine(WaitForChildSeatSpawn());
        }

        private IEnumerator WaitForChildSeatSpawn()
        {
            while (true)
            {
                bool broken = false;
                foreach (Transform child in transform)
                {
                    if (child.TryGetComponent(out Chair chair))
                    {
                        passengerSeatCollider = chair.GetComponent<Collider>();
                        broken = true;
                        break;
                    }
                }
                if (broken) { break; }
                yield return null;
            }
        }

        private void Start()
        {
            originalHandleBarRotation = handleBars.localRotation;
            wheels = GetComponentsInChildren<Wheel>();
            rb = GetComponent<Rigidbody>();
        }

        private float lastFrontYValue;
        private float lastRearYValue;
        private void Update()
        {
            if (passengerSeatCollider)
                passengerSeatCollider.enabled = driver;

            if (!IsOwner) { return; }

            // Suspension position is found by tracking the y delta local position of the wheel and translating it accordingly in local space

            frontWheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            frontWheelMesh.rotation = rot;
            // I have no idea why this requires the rear wheel collider
            Vector3 wheelLocPos = rearWheelCollider.transform.InverseTransformPoint(pos);
            frontSuspension.Translate(0, 0, wheelLocPos.y - lastFrontYValue, Space.Self);
            lastFrontYValue = wheelLocPos.y;
            
            rearWheelCollider.GetWorldPose(out pos, out rot);
            // Take wheel collider pose and convert that into local space
            wheelLocPos = rearWheelCollider.transform.InverseTransformPoint(pos);
            rearSuspension.Translate(0, 0, wheelLocPos.y - lastRearYValue, Space.Self);
            lastRearYValue = wheelLocPos.y;
        }

        [Header("Physics Settings")]
        public float dampenFactor = 0.8f;
        public float adjustFactor = 2;
        public float stopAdjustMaxVelocity = 15;
        public float stopAdjustMaxAngularVelocity = 10;
        public float stopAdjustQuaternionAngle = 15;
        private void FixedUpdate()
        {
            if (!IsOwner) { return; }

            float steerAngle = -Vector3.SignedAngle(handleBars.up, transform.forward, transform.up);

            foreach (Wheel w in wheels)
            {
                w.Steer(steerAngle);
                w.Accelerate(sprinting ? moveInput.y * power * 2 : moveInput.y * power);
                w.Brake(jumping | !driver ? brakePower : 0);
                w.UpdatePosition();
            }

            if (driver)
            {
                Quaternion deltaQuat = Quaternion.FromToRotation(rb.transform.up, Vector3.up);

                if ((rb.velocity.magnitude > stopAdjustMaxVelocity | rb.angularVelocity.magnitude > stopAdjustMaxAngularVelocity) & Quaternion.Angle(deltaQuat, Quaternion.identity) > stopAdjustQuaternionAngle)
                    //| !frontWheelCollider.isGrounded & !rearWheelCollider.isGrounded)
                {
                    rb.constraints = RigidbodyConstraints.None;
                }
                else
                {
                    deltaQuat.ToAngleAxis(out float angle, out Vector3 axis);
                    rb.AddTorque(-rb.angularVelocity * dampenFactor, ForceMode.Acceleration);
                    rb.AddTorque(adjustFactor * angle * axis.normalized, ForceMode.Acceleration);
                    rb.constraints = RigidbodyConstraints.FreezeRotationZ;
                }
            }
        }

        public override void OnDriverEnter(ulong networkObjectId)
        {
            if (!IsServer) { Debug.LogWarning("Calling OnDriverEnter from a client"); return; }
            driver = NetworkManager.SpawnManager.SpawnedObjects[networkObjectId];
            
            NetworkObject.ChangeOwnership(driver.OwnerClientId);
            GetComponentInChildren<NestedNetworkObject>().NetworkObject.ChangeOwnership(driver.OwnerClientId);

            rb.constraints = RigidbodyConstraints.FreezeRotationZ;
            driver.SendMessage("OnDriverEnter", this);
            OnDriverEnterClientRpc(networkObjectId);
        }

        [ClientRpc]
        void OnDriverEnterClientRpc(ulong networkObjectId)
        {
            driver = NetworkManager.SpawnManager.SpawnedObjects[networkObjectId];

            rb.constraints = RigidbodyConstraints.FreezeRotationZ;
            driver.SendMessage("OnDriverEnter", this);
        }

        public override void OnDriverExit()
        {
            if (!IsServer) { Debug.LogWarning("Calling OnDriverExit from a client"); return; }

            NetworkObject.RemoveOwnership();
            rb.constraints = RigidbodyConstraints.None;

            OnDriverExitClientRpc();

            driver.SendMessage("OnDriverExit");
            driver = null;
        }

        [ClientRpc]
        void OnDriverExitClientRpc()
        {
            rb.constraints = RigidbodyConstraints.None;
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
            handleBars.localRotation = originalHandleBarRotation * Quaternion.Euler(0, 0, handleBarRotation);
        }

        bool jumping;
        protected override void OnVehicleJump(bool pressed)
        {
            jumping = pressed;
        }

        bool crouching;
        protected override void OnVehicleCrouch(bool pressed)
        {
            crouching = pressed;
        }

        bool sprinting;
        protected override void OnVehicleSprint(bool pressed)
        {
            sprinting = pressed;
        }
    }
}