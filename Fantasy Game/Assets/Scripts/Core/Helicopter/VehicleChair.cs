using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class VehicleChair : NetworkBehaviour
    {
        public bool driverChair;
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

        [ClientRpc] void ExitSittingClientRpc(ClientRpcParams clientRpcParams = default)
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