using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

namespace LightPat.Core
{
    public class VehicleChair : NetworkBehaviour
    {
        public bool driverChair;
        [Header("Sitting Down")]
        public Vector3 occupantPosition;
        public Vector3 occupantRotation;

        NetworkObject occupant;

        public bool TrySitting(NetworkObject newOccupant)
        {
            if (occupant) { return false; }

            TrySittingServerRpc(newOccupant.NetworkObjectId);

            return true;
        }

        [ServerRpc(RequireOwnership = false)]
        void TrySittingServerRpc(ulong networkObjectId)
        {
            if (occupant) { return; }

            NetworkObject newOccupant = NetworkManager.SpawnManager.SpawnedObjects[networkObjectId];

            if (newOccupant.TrySetParent(transform, true))
            {
                occupant = newOccupant;
                if (driverChair)
                    GetComponentInParent<Vehicle>().SendMessage("OnDriverEnter", occupant);
            }
        }

        public bool ExitSitting()
        {
            ExitSittingServerRpc();
            return false;
        }

        [ServerRpc(RequireOwnership = false)]
        void ExitSittingServerRpc()
        {
            if (occupant.TryRemoveParent(true))
            {
                occupant = null;
                if (driverChair)
                    GetComponentInParent<Vehicle>().SendMessage("OnDriverExit");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + Quaternion.Euler(occupantRotation) * occupantPosition, 0.2f);
        }
    }
}