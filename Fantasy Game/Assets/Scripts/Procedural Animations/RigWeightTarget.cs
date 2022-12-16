using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Unity.Netcode;
using System.Linq;

namespace LightPat.ProceduralAnimations
{
    public class RigWeightTarget : NetworkBehaviour
    {
        public float weightTarget = 1;
        public float weightSpeed = 5;
        public bool instantWeight;
        Rig rig;
        Animator animator;

        public Rig GetRig()
        {
            return rig;
        }

        public override void OnNetworkSpawn()
        {
            //if (IsOwner)
            //    NetworkManager.NetworkTickSystem.Tick += UpdateRig;
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
                NetworkManager.NetworkTickSystem.Tick -= UpdateRig;
        }

        void UpdateRig()
        {
            UpdateRigServerRpc(weightTarget, OwnerClientId);
        }

        [ServerRpc]
        void UpdateRigServerRpc(float newWeightTarget, ulong clientId)
        {
            weightTarget = newWeightTarget;

            List<ulong> clientIdList = NetworkManager.ConnectedClientsIds.ToList();
            clientIdList.Remove(clientId);

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = clientIdList.ToArray()
                }
            };

            UpdateRigClientRpc(weightTarget, clientRpcParams);
        }

        [ClientRpc] void UpdateRigClientRpc(float newWeightTarget, ClientRpcParams clientRpcParams = default) { weightTarget = newWeightTarget; }

        private void Start()
        {
            rig = GetComponent<Rig>();
            animator = GetComponentInParent<Animator>();
        }

        private void Update()
        {
            if (rig.weight == weightTarget) { return; }
            if (instantWeight) { rig.weight = weightTarget; return; }

            if (Mathf.Abs(weightTarget - rig.weight) > 0.1)
            {
                rig.weight = Mathf.Lerp(rig.weight, weightTarget, Time.deltaTime * weightSpeed * animator.speed);
            }
            else
            {
                rig.weight = Mathf.MoveTowards(rig.weight, weightTarget, Time.deltaTime * animator.speed);
            }
        }
    }
}
