using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace LightPat.Core.Player
{
    public class WeaponNetworkTransform : NetworkBehaviour
    {
        public bool interpolate = true;
        public float interpolationSpeed = 10;
        Vector3 currentPosition;
        Quaternion currentRotation;

        public override void OnNetworkSpawn()
        {
            Debug.Log("REACHED " + IsOwner);

            //if (IsOwner)
            //    NetworkManager.NetworkTickSystem.Tick += UpdateTransform;
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
                NetworkManager.NetworkTickSystem.Tick -= UpdateTransform;
        }

        void UpdateTransform()
        {
            if (Vector3.Distance(currentPosition, transform.position) > 0.001f | Quaternion.Angle(currentRotation, transform.rotation) > 0.01f)
                SendTransformServerRpc(transform.position, transform.rotation, OwnerClientId);
        }

        [ServerRpc]
        void SendTransformServerRpc(Vector3 newPosition, Quaternion newRotation, ulong clientId)
        {
            currentPosition = newPosition;
            currentRotation = newRotation;

            List<ulong> clientIdList = NetworkManager.ConnectedClientsIds.ToList();
            clientIdList.Remove(clientId);

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = clientIdList.ToArray()
                }
            };

            SendTransformClientRpc(transform.position, transform.rotation, clientRpcParams);
        }

        [ClientRpc]
        void SendTransformClientRpc(Vector3 newPosition, Quaternion newRotation, ClientRpcParams clientRpcParams = default)
        {
            currentPosition = newPosition;
            currentRotation = newRotation;
        }

        private void LateUpdate()
        {
            if (IsOwner) { return; }

            if (interpolate)
            {
                transform.position = Vector3.Lerp(transform.position, currentPosition, Time.deltaTime * interpolationSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, currentRotation, Time.deltaTime * interpolationSpeed);
            }
            else
            {
                transform.position = currentPosition;
                transform.rotation = currentRotation;
            }
        }
    }
}