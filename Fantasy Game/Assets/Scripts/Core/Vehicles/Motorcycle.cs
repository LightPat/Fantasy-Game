using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class Motorcycle : Vehicle
    {
        public int mph;
        public int rpm;
        public int gear;
        public int[] rpmGearShifts;
        public float[] engineIdlePitchShifts;
        public AudioClip[] gearSounds;
        public AudioClip engineIdleSound;
        public AudioClip engineStartSound;
        public float loopingAudioBasePitch = 1;
        public float enginePitchSpeed = 5;

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
        Chair driverChair;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                GetComponent<NestedNetworkObject>().NestedSpawn();

            StartCoroutine(WaitForChildSeatSpawn());

            //AudioSource[] audioSources = GetComponents<AudioSource>();
            //loopingAudioSource = audioSources[0];
            //loopingAudioSource.clip = engineIdleSound;
            //notLoopingAudioSource = audioSources[1];
            //notLoopingAudioSource.clip = engineStartSound;
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
            driverChair = GetComponent<Chair>();

            AudioSource[] audioSources = GetComponents<AudioSource>();
            loopingAudioSource = audioSources[0];
            loopingAudioSource.clip = engineIdleSound;
            notLoopingAudioSource = audioSources[1];
            notLoopingAudioSource.clip = engineStartSound;
        }

        [Header("Skid Settings")]
        public GameObject skidPrefab;
        public Vector3 frontSkidRotationOffset;
        public float frontSkidThreshold;
        public Vector3 rearSkidRotationOffset;
        public float rearSkidThreshold;

        private AudioSource notLoopingAudioSource;
        private AudioSource loopingAudioSource;
        private float lastFrontYValue;
        private float lastRearYValue;
        private float idlePitch = 1;
        private bool engineStarted;
        private Vector2 lastMoveInput;
        private float lastGearChangeTime;
        private void Update()
        {
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

            frontWheelCollider.GetGroundHit(out WheelHit frontHit);
            if (Mathf.Abs(frontHit.sidewaysSlip) > frontSkidThreshold | Mathf.Abs(frontHit.forwardSlip) > frontSkidThreshold)
            {
                Instantiate(skidPrefab, frontHit.point, Quaternion.Euler(frontHit.normal) * frontWheelCollider.transform.rotation * Quaternion.Euler(frontSkidRotationOffset));
            }

            rearWheelCollider.GetGroundHit(out WheelHit rearHit);
            if (Mathf.Abs(rearHit.sidewaysSlip) > rearSkidThreshold | Mathf.Abs(rearHit.forwardSlip) > rearSkidThreshold)
            {
                Instantiate(skidPrefab, rearHit.point, Quaternion.Euler(rearHit.normal) * rearWheelCollider.transform.rotation * Quaternion.Euler(rearSkidRotationOffset));
            }

            if (passengerSeatCollider)
                passengerSeatCollider.enabled = driver;

            driverChair.rotateY = crouching;

            rpm = Mathf.RoundToInt(rearWheelCollider.rpm);
            float kilometersPerSecond = transform.InverseTransformDirection(rb.velocity).z / 1000; // m/s to km/s
            float kilometersPerHour = kilometersPerSecond * 3600;
            float milesPerHour = kilometersPerHour / 1.609f;
            mph = Mathf.RoundToInt(Mathf.Clamp(milesPerHour, 0, 999));

            // START Audio Logic
            if (notLoopingAudioSource.clip == engineStartSound)
            {
                if (notLoopingAudioSource.isPlaying)
                {
                    return;
                }
                else if (driver)
                {
                    engineStarted = true;
                }
            }

            if (!engineStarted) { return; }

            if (!loopingAudioSource.isPlaying)
                loopingAudioSource.Play();

            bool gearChange;
            int newGear = gear;
            if (Time.time - lastGearChangeTime > 1)
            {
                for (int i = 0; i < gearSounds.Length; i++)
                {
                    if (rearWheelCollider.rpm > rpmGearShifts[i]) { newGear = Mathf.Clamp(i, 0, gearSounds.Length); }
                }
            }

            gearChange = newGear != gear;
            gear = newGear;

            notLoopingAudioSource.pitch = moveInput.y > 0 ? 1 : -1;
            idlePitch = Mathf.MoveTowards(idlePitch, engineIdlePitchShifts[gear], Time.deltaTime * enginePitchSpeed);
            loopingAudioSource.pitch = idlePitch;

            if (gear != 0)
            {
                notLoopingAudioSource.volume = Mathf.MoveTowards(notLoopingAudioSource.volume, 1, Time.deltaTime * enginePitchSpeed);
            }
            else
            {
                notLoopingAudioSource.volume = Mathf.MoveTowards(notLoopingAudioSource.volume, 0, Time.deltaTime * enginePitchSpeed);
            }

            if (gearChange)
            {
                lastGearChangeTime = Time.time;

                // If we are downshifting
                if (System.Array.IndexOf(gearSounds, notLoopingAudioSource.clip) > gear & moveInput.y <= 0)
                {
                    if (gear != 0)
                    {
                        notLoopingAudioSource.clip = gearSounds[gear];
                        if (notLoopingAudioSource.clip) { notLoopingAudioSource.time = notLoopingAudioSource.clip.length * 0.9f; }
                    }
                }
                else // If we are upshifting
                {
                    if (gear != 0)
                    {
                        notLoopingAudioSource.clip = gearSounds[gear];
                        if (notLoopingAudioSource.clip) { notLoopingAudioSource.time = 0; }
                    }
                }

                if (!notLoopingAudioSource.isPlaying & gear != 0) { notLoopingAudioSource.Play(); }
            }
            else if (gear == gearSounds.Length-1) // If no gear change
            {
                if (!notLoopingAudioSource.isPlaying) { notLoopingAudioSource.Play(); }
            }

            // If the current gear's clip is not playing and we hit the throttle again, play the clip again
            if (gear > 0 & !notLoopingAudioSource.isPlaying)
            {
                if (moveInput.y > 0 & lastMoveInput.y <= 0)
                {
                    notLoopingAudioSource.clip = gearSounds[gear];
                    notLoopingAudioSource.Play();
                }
            }

            lastMoveInput = moveInput;
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

                if (moveInput.y <= 0 & rearWheelCollider.rpm > 100)
                    w.Accelerate(-power * 2);
                else
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

            notLoopingAudioSource.volume = 1;
            notLoopingAudioSource.Play();

            rb.constraints = RigidbodyConstraints.FreezeRotationZ;
            driver.SendMessage("OnDriverEnter", this);
            OnDriverEnterClientRpc(networkObjectId);
        }

        [ClientRpc]
        void OnDriverEnterClientRpc(ulong networkObjectId)
        {
            driver = NetworkManager.SpawnManager.SpawnedObjects[networkObjectId];

            notLoopingAudioSource.volume = 1;
            notLoopingAudioSource.Play();

            rb.constraints = RigidbodyConstraints.FreezeRotationZ;
            driver.SendMessage("OnDriverEnter", this);
        }

        public override void OnDriverExit()
        {
            if (!IsServer) { Debug.LogWarning("Calling OnDriverExit from a client"); return; }

            NetworkObject.RemoveOwnership();
            rb.constraints = RigidbodyConstraints.None;
            notLoopingAudioSource.volume = 0;
            notLoopingAudioSource.Stop();
            engineStarted = false;

            if (!IsClient)
            {
                driver.SendMessage("OnDriverExit");
                driver = null;
            }

            OnDriverExitClientRpc();
        }

        [ClientRpc]
        void OnDriverExitClientRpc()
        {
            notLoopingAudioSource.volume = 0;
            notLoopingAudioSource.Stop();
            engineStarted = false;
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
            if (!crouching)
            {
                handleBarRotation += newLookInput.x;
                if (handleBarRotation > maxHandleBarRotation) { handleBarRotation = maxHandleBarRotation; }
                if (handleBarRotation < -maxHandleBarRotation) { handleBarRotation = -maxHandleBarRotation; }
                handleBars.localRotation = originalHandleBarRotation * Quaternion.Euler(0, 0, handleBarRotation);
            }
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