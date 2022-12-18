using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using UnityEngine.Animations.Rigging;

namespace LightPat.ProceduralAnimations
{
    public class SyncIKConstraint : NetworkBehaviour
    {
        enum ConstraintType
        {
            aim,
            rotation,
            position
        }
        MultiAimConstraint aimConstraint;
        MultiRotationConstraint rotationConstraint;
        MultiPositionConstraint positionConstraint;
        Vector3 currentAimConstraintOffset;
        Vector3 currentRotationConstraintOffset;
        Vector3 currentPositionConstraintOffset;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
                NetworkManager.NetworkTickSystem.Tick += UpdateConstraint;
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
                NetworkManager.NetworkTickSystem.Tick -= UpdateConstraint;
        }

        private void Awake()
        {
            aimConstraint = GetComponent<MultiAimConstraint>();
            rotationConstraint = GetComponent<MultiRotationConstraint>();
            positionConstraint = GetComponent<MultiPositionConstraint>();
        }

        void UpdateConstraint()
        {
            if (aimConstraint)
            {
                if (Vector3.Distance(currentAimConstraintOffset, aimConstraint.data.offset) > 0.001f)
                    SendConstraintServerRpc(aimConstraint.data.offset, ConstraintType.aim, OwnerClientId);
                currentAimConstraintOffset = aimConstraint.data.offset;
            }
            if (rotationConstraint)
            {
                if (Vector3.Distance(currentRotationConstraintOffset, rotationConstraint.data.offset) > 0.001f)
                    SendConstraintServerRpc(rotationConstraint.data.offset, ConstraintType.rotation, OwnerClientId);
                currentRotationConstraintOffset = rotationConstraint.data.offset;
            }
            if (positionConstraint)
            {
                if (Vector3.Distance(currentPositionConstraintOffset, positionConstraint.data.offset) > 0.001f)
                    SendConstraintServerRpc(positionConstraint.data.offset, ConstraintType.position, OwnerClientId);
                currentPositionConstraintOffset = positionConstraint.data.offset;
            }
        }

        [ServerRpc]
        void SendConstraintServerRpc(Vector3 newOffset, ConstraintType offsetType, ulong clientId)
        {
            Debug.Log("Sync IK Constraint " + this);

            if (!IsHost)
            {
                if (offsetType == ConstraintType.aim)
                    aimConstraint.data.offset = newOffset;
                else if (offsetType == ConstraintType.rotation)
                    rotationConstraint.data.offset = newOffset;
                else if (offsetType == ConstraintType.position)
                    positionConstraint.data.offset = newOffset;
            }

            List<ulong> clientIdList = NetworkManager.ConnectedClientsIds.ToList();
            clientIdList.Remove(clientId);

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = clientIdList.ToArray()
                }
            };

            SendConstraintClientRpc(newOffset, offsetType, clientRpcParams);
        }

        [ClientRpc]
        void SendConstraintClientRpc(Vector3 newOffset, ConstraintType offsetType, ClientRpcParams clientRpcParams = default)
        {
            if (offsetType == ConstraintType.aim)
                aimConstraint.data.offset = newOffset;
            else if (offsetType == ConstraintType.rotation)
                rotationConstraint.data.offset = newOffset;
            else if (offsetType == ConstraintType.position)
                positionConstraint.data.offset = newOffset;
        }
    }
}
