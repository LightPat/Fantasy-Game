using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class CustomNetworkTransform : NetworkBehaviour
    {
        public bool interpolate = true;
        [Range(0.001f, 1)]
        public float positionThreshold = 0.001f;
        [Range(0.001f, 360)]
        public float rotAngleThreshold = 0.001f;

        private NetworkVariable<Vector3> currentPosition = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> currentRotation = new NetworkVariable<Quaternion>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        float positionSpeed;
        float rotationSpeed;

        public override void OnNetworkSpawn()
        {
            currentPosition.OnValueChanged += OnPositionChanged;
            currentRotation.OnValueChanged += OnRotationChanged;
        }

        public override void OnNetworkDespawn()
        {
            currentPosition.OnValueChanged -= OnPositionChanged;
            currentRotation.OnValueChanged -= OnRotationChanged;
        }

        void OnPositionChanged(Vector3 prevPosition, Vector3 newPosition)
        {
            if (!interpolate)
            {
                transform.localPosition = currentPosition.Value;
            }
            else
            {
                positionSpeed = Vector3.Distance(transform.localPosition, currentPosition.Value);
                if (positionSpeed < 10)
                    positionSpeed = 10;
            }
        }

        void OnRotationChanged(Quaternion prevRotation, Quaternion newRotation)
        {
            rotationSpeed = Quaternion.Angle(transform.localRotation, newRotation);
            if (!interpolate)
                transform.localRotation = currentRotation.Value;
        }

        private void LateUpdate()
        {
            if (!IsSpawned) { return; }

            if (IsOwner)
            {
                if (Vector3.Distance(transform.localPosition, currentPosition.Value) > positionThreshold)
                    currentPosition.Value = transform.localPosition;
                if (Quaternion.Angle(transform.localRotation, currentRotation.Value) > rotAngleThreshold)
                    currentRotation.Value = transform.localRotation;
            }
            else
            {
                if (interpolate)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, currentPosition.Value, Time.deltaTime * positionSpeed);
                    //if (Vector3.Distance(transform.localPosition, currentPosition.Value) < 1)
                    //    transform.localPosition = Vector3.MoveTowards(transform.localPosition, currentPosition.Value, Time.deltaTime * positionSpeed);

                    transform.localRotation = Quaternion.Slerp(transform.localRotation, currentRotation.Value, Time.deltaTime * rotationSpeed);
                }
                else
                {
                    transform.localPosition = currentPosition.Value;
                    transform.localRotation = currentRotation.Value;
                }
            }
        }
    }
}