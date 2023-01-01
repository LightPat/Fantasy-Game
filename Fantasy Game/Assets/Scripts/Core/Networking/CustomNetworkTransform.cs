using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class CustomNetworkTransform : NetworkBehaviour
    {
        public bool interpolate;

        private NetworkVariable<Vector3> currentPosition = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> currentRotation = new NetworkVariable<Quaternion>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

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
                transform.localPosition = currentPosition.Value;
        }

        void OnRotationChanged(Quaternion prevRotation, Quaternion newRotation)
        {
            if (!interpolate)
                transform.localRotation = currentRotation.Value;
        }

        private void LateUpdate()
        {
            if (IsOwner)
            {
                currentPosition.Value = transform.localPosition;
                currentRotation.Value = transform.localRotation;
            }
            else
            {
                if (interpolate)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, currentPosition.Value, Time.deltaTime * 8);
                    transform.localRotation = Quaternion.Slerp(transform.localRotation, currentRotation.Value, Time.deltaTime * 8);
                }
            }
        }
    }
}