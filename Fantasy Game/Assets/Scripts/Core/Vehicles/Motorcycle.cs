using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class Motorcycle : Vehicle
    {
        [Header("Motorcycle Specific")]
        public Vector3 velocityLimits;
        public float forceClampMultiplier;
        public float maxHandleBarRotation;
        public Transform handlebars;
        public Transform rearSuspension;

        NetworkObject driver;
        Rigidbody rb;
        float handleBarRotation;
        Vector3 currentVelocityLimits;
        Quaternion originalHandleBarRotation;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            currentVelocityLimits = velocityLimits;
            originalHandleBarRotation = handlebars.localRotation;
        }

        public WheelCollider frontWheel;
        public WheelCollider rearWheel;
        //public Transform frontWheel;
        //public float frontWheelRaycastDistance;
        //public Transform rearWheel;
        //public float rearWheelRaycastDistance;
        private void Update()
        {
            //if (Physics.Raycast(frontWheel.position, Vector3.down, frontWheelRaycastDistance, -1, QueryTriggerInteraction.Ignore) &
            //    Physics.Raycast(rearWheel.position, Vector3.down, rearWheelRaycastDistance, -1, QueryTriggerInteraction.Ignore))
            //{
            //    Quaternion targetRotation = Quaternion.LookRotation(handlebars.up);
            //    if (transform.InverseTransformDirection(rb.velocity).z < 0)
            //        targetRotation = Quaternion.LookRotation(handlebars.up * -1);

            //    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rb.velocity.magnitude / 5);
            //}
        }

        private void FixedUpdate()
        {
            if (!driver) { return; }

            //Vector3 moveForce = new Vector3(moveInput.x, 0, moveInput.y);
            Vector3 moveForce = new Vector3(0, 0, moveInput.y);
            Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

            // Move vehicle horizontally
            //if (localVelocity.x > currentVelocityLimits.x)
            //    moveForce.x -= localVelocity.x - currentVelocityLimits.x;
            //if (localVelocity.x < -currentVelocityLimits.x)
            //    moveForce.x -= localVelocity.x + currentVelocityLimits.x;
            //if (localVelocity.z > currentVelocityLimits.z)
            //    moveForce.z -= localVelocity.z - currentVelocityLimits.z;
            //if (localVelocity.z < -currentVelocityLimits.z)
            //    moveForce.z -= localVelocity.z + currentVelocityLimits.z;
            //if (moveInput == Vector2.zero)
            //{
            //    moveForce.x = 0 - localVelocity.x;
            //    moveForce.z = 0 - localVelocity.z;
            //}
            moveForce = Vector3.ClampMagnitude(moveForce, forceClampMultiplier);
            rb.AddRelativeForce(moveForce, ForceMode.VelocityChange);
        }

        //[Header("OnCollisionStay")]
        //public float maxMantleHeight = 1;
        //public float minTranslateDistance = 0.13f;
        //private void OnCollisionStay(Collision collision)
        //{
        //    if (collision.collider.isTrigger) { return; }
        //    if (collision.collider.bounds.size.magnitude < 0.2f) { return; } // If the collider we are hitting is too small, this is used to filter out shells from guns

        //    if (Mathf.Abs(animator.GetFloat("moveInputX")) > 0.5f | Mathf.Abs(animator.GetFloat("moveInputY")) > 0.5f)
        //    {
        //        float[] yPos = new float[collision.contactCount];
        //        for (int i = 0; i < collision.contactCount; i++)
        //        {
        //            yPos[i] = collision.GetContact(i).point.y;
        //        }
        //        float translateDistance = yPos.Max() - transform.position.y;
        //        if (translateDistance < minTranslateDistance) { return; } // If the wall is beneath us
        //        if (collision.collider.bounds.max.y - transform.position.y > maxMantleHeight) { return; } // If the wall is too high to mantle
        //        transform.Translate(new Vector3(0, translateDistance, 0));
        //    }
        //}

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