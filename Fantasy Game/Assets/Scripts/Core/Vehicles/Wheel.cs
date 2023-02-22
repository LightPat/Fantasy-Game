using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Wheel : MonoBehaviour
    {
        public bool powered;
        public float maxAngle = 90;
        public float offset;

        private float turnAngle;
        private WheelCollider wcol;
        private Transform wmesh;

        private void Start()
        {
            wcol = GetComponentInChildren<WheelCollider>();
            wmesh = GetComponentInChildren<Renderer>().transform;
        }

        public void Steer(float steerInput)
        {
            turnAngle = steerInput * maxAngle + offset;
            wcol.steerAngle = turnAngle;
        }

        public void Accelerate(float powerInput)
        {
            if (powered) wcol.motorTorque = powerInput;
            else wcol.brakeTorque = 0;
        }

        public void UpdatePosition()
        {
            wcol.GetWorldPose(out Vector3 pos, out Quaternion rot);
            wmesh.position = pos;
            wmesh.rotation = rot;
        }
    }
}