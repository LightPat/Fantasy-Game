using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace LightPat.Core
{
    public class AnimatorLayerWeightManager : NetworkBehaviour
    {
        public float transitionSpeed;

        float[] layerWeightTargets;
        Animator animator;

        public override void OnNetworkSpawn()
        {
            //if (IsOwner)
            //    NetworkManager.NetworkTickSystem.Tick += UpdateLayers;
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
                NetworkManager.NetworkTickSystem.Tick -= UpdateLayers;
        }

        void UpdateLayers()
        {
            SendLayerWeightsServerRpc(layerWeightTargets, OwnerClientId);
        }

        [ServerRpc]
        void SendLayerWeightsServerRpc(float[] newLayerWeightTargets, ulong clientId)
        {
            layerWeightTargets = newLayerWeightTargets;

            List<ulong> clientIdList = NetworkManager.ConnectedClientsIds.ToList();
            clientIdList.Remove(clientId);

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = clientIdList.ToArray()
                }
            };

            SendLayerWeightsClientRpc(layerWeightTargets, clientRpcParams);
        }

        [ClientRpc] void SendLayerWeightsClientRpc(float[] newLayerWeightTargets, ClientRpcParams clientRpcParams = default) { layerWeightTargets = newLayerWeightTargets; }

        public void SetLayerWeight(string layerName, float targetWeight)
        {
            layerWeightTargets[animator.GetLayerIndex(layerName)] = targetWeight;
        }

        public void SetLayerWeight(int layerIndex, float targetWeight)
        {
            layerWeightTargets[layerIndex] = targetWeight;
        }

        public float GetLayerWeight(string layerName)
        {
            return layerWeightTargets[animator.GetLayerIndex(layerName)];
        }

        public float GetLayerWeight(int layerIndex)
        {
            return layerWeightTargets[layerIndex];
        }

        private void Start()
        {
            animator = GetComponent<Animator>();
            layerWeightTargets = new float[animator.layerCount];

            for (int i = 1; i < animator.layerCount; i++)
            {
                layerWeightTargets[i] = animator.GetLayerWeight(i);
            }
        }

        private void Update()
        {
            for (int i = 1; i < layerWeightTargets.Length; i++)
            {
                float weightTarget = layerWeightTargets[i];

                animator.SetLayerWeight(i, Mathf.MoveTowards(animator.GetLayerWeight(i), weightTarget, Time.deltaTime * transitionSpeed));
            }
        }
    }
}
