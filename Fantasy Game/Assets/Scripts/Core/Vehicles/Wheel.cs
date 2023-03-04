using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Wheel : MonoBehaviour
    {
        public bool powered;
        public bool steer;
        public float offset;

        private float turnAngle;
        private WheelCollider wcol;
        [SerializeField] private Transform wmesh;

        private void Awake()
        {
            wcol = GetComponentInChildren<WheelCollider>();
        }

        public void Steer(float steerInput)
        {
            if (!steer) { return; }

            turnAngle = steerInput + offset;
            wcol.steerAngle = turnAngle;
        }

        public void Accelerate(float powerInput)
        {
            if (powered) wcol.motorTorque = powerInput;
        }

        public void Brake(float brakePower)
        {
            wcol.brakeTorque = brakePower;
        }

        public void UpdatePosition()
        {
            wcol.GetWorldPose(out Vector3 pos, out Quaternion rot);

            wmesh.position = pos;
            wmesh.rotation = rot;
        }
    }
}