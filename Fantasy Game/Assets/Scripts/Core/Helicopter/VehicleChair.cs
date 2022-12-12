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
        [Header("Exitting Chair")]
        public Vector3 exitPosOffset;

        NetworkObject occupant;
        ConfigurableJoint joint;

        [ServerRpc(RequireOwnership = false)]
        public void TrySittingServerRpc(ulong networkObjectId)
        {
            if (occupant | joint) { return; }

            NetworkManager.SpawnManager.SpawnedObjects[networkObjectId].TrySetParent(transform, true);
            TrySitting(networkObjectId);

            TrySittingClientRpc(networkObjectId);

            if (driverChair)
                GetComponentInParent<Vehicle>().OnDriverEnter(networkObjectId);
        }

        [ClientRpc]
        void TrySittingClientRpc(ulong networkObjectId)
        {
            TrySitting(networkObjectId);
        }

        private void TrySitting(ulong networkObjectId)
        {
            occupant = NetworkManager.SpawnManager.SpawnedObjects[networkObjectId];
            occupant.SendMessage("OnChairEnter", this);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ExitSittingServerRpc()
        {
            occupant.TryRemoveParent();
            if (!IsHost)
                ExitSitting();

            ExitSittingClientRpc();

            if (driverChair)
                GetComponentInParent<Vehicle>().OnDriverExit();
        }

        [ClientRpc]
        void ExitSittingClientRpc()
        {
            ExitSitting();
        }

        private void ExitSitting()
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