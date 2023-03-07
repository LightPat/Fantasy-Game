using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class Chair : NetworkBehaviour
    {
        [Header("Vehicle?")]
        public bool driverChair;
        [Header("IK Transform assignments")]
        public Transform leftFootGrip;
        public Transform rightFootGrip;
        public Transform leftHandGrip;
        public Transform rightHandGrip;
        public Transform leftFingersGrips;
        public Transform rightFingersGrips;
        [Header("Look Input Settings")]
        public bool rotateX = true;
        public bool rotateY = true;
        [Header("Sitting Down")]
        public Vector3 occupantPosition;
        public Vector3 occupantRotation;
        [Header("Exitting Chair")]
        public Vector3 exitPosOffset;

        NetworkObject occupant;

        [ServerRpc(RequireOwnership = false)]
        public void TrySittingServerRpc(ulong networkObjectId)
        {
            if (occupant) { return; }

            occupant = NetworkManager.SpawnManager.SpawnedObjects[networkObjectId];
            
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { occupant.OwnerClientId }
                }
            };

            TrySittingClientRpc(networkObjectId, clientRpcParams);

            if (driverChair)
                GetComponentInParent<Vehicle>().OnDriverEnter(networkObjectId);
        }

        [ClientRpc]
        void TrySittingClientRpc(ulong networkObjectId, ClientRpcParams clientRpcParams = default)
        {
            occupant = NetworkManager.SpawnManager.SpawnedObjects[networkObjectId];
            occupant.SendMessage("OnChairEnter", this);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ExitSittingServerRpc()
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { occupant.OwnerClientId }
                }
            };

            ExitSittingClientRpc(clientRpcParams);

            occupant = null;

            if (driverChair)
                GetComponentInParent<Vehicle>().OnDriverExit();
        }

        [ClientRpc]
        void ExitSittingClientRpc(ClientRpcParams clientRpcParams = default)
        {
            occupant.SendMessage("OnChairExit");
            occupant = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + Quaternion.Euler(occupantRotation) * occupantPosition, 0.2f);
        }
    }
}