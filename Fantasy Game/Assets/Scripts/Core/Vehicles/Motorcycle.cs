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
        public Transform handleBars;
        public Transform frontSuspension;
        public Transform frontWheelMesh;
        public WheelCollider frontWheelCollider;
        public Transform rearSuspension;
        public WheelCollider rearWheelCollider;

        Rigidbody rb;
        NetworkObject driver;
        float handleBarRotation;
        Quaternion originalHandleBarRotation;
        Wheel[] wheels;

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
            // Handle bar mesh position is found by tracking the y delta local position of the wheel and translating it accordingly
            // Take wheel collider pose and convert that into local space
            // Handle bar mesh rotation is found by 

            frontWheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            frontWheelMesh.rotation = rot;
            // I have no idea why this requires the rear wheel collider
            Vector3 wheelLocPos = rearWheelCollider.transform.InverseTransformPoint(pos);
            frontSuspension.Translate(0, 0, wheelLocPos.y - lastFrontYValue, Space.Self);
            lastFrontYValue = wheelLocPos.y;
            
            rearWheelCollider.GetWorldPose(out pos, out rot);
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
            float steerAngle = -Vector3.SignedAngle(handleBars.up, transform.forward, transform.up);

            foreach (Wheel w in wheels)
            {
                w.Steer(steerAngle);
                w.Accelerate(moveInput.y * power);
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
            driver.SendMessage("OnDriverEnter", this);
            transform.rotation = Quaternion.FromToRotation(rb.transform.up, Vector3.up);
            rb.constraints = RigidbodyConstraints.FreezeRotationZ;
        }

        public override void OnDriverExit()
        {
            if (!IsServer) { Debug.LogWarning("Calling OnDriverExit from a client"); return; }

            NetworkObject.RemoveOwnership();
            driver.SendMessage("OnDriverExit");
            driver = null;
            rb.constraints = RigidbodyConstraints.None;
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