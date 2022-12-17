using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace LightPat.Core
{
    //[ExecuteAlways]
    public class CustomNetworkAnimator : NetworkBehaviour
    {
        public List<string> parameterNamesToSync = new List<string>();
        public bool refreshParameters;

        Animator animator;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
                NetworkManager.NetworkTickSystem.Tick += UpdateAnimator;
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
                NetworkManager.NetworkTickSystem.Tick -= UpdateAnimator;
        }

        private void Start()
        {
            animator = GetComponent<Animator>();
        }

        // Uncomment execute always to generate list of parameter strings
        //private void Update()
        //{
        //    if (!refreshParameters) { return; }
        //    parameterNamesToSync.Clear();
        //    foreach (AnimatorControllerParameter parameter in animator.parameters)
        //    {
        //        parameterNamesToSync.Add(parameter.name);
        //    }
        //}

        void UpdateAnimator()
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                // If we don't want to sync this parameter
                if (!parameterNamesToSync.Contains(parameter.name)) { continue; }

                if (parameter.type == AnimatorControllerParameterType.Bool)
                    SendAnimatorServerRpc(parameter.name, animator.GetBool(parameter.name), OwnerClientId);
                else if (parameter.type == AnimatorControllerParameterType.Float)
                    SendAnimatorServerRpc(parameter.name, animator.GetFloat(parameter.name), OwnerClientId);
                else if (parameter.type == AnimatorControllerParameterType.Int)
                    SendAnimatorServerRpc(parameter.name, animator.GetInteger(parameter.name), OwnerClientId);
                else if (parameter.type == AnimatorControllerParameterType.Trigger)
                    Debug.LogWarning("Triggers are not supported");
            }
        }

        [ServerRpc]
        void SendAnimatorServerRpc(string stateName, bool value, ulong clientId)
        {
            if (!IsHost)
                animator.SetBool(stateName, value);

            List<ulong> clientIdList = NetworkManager.ConnectedClientsIds.ToList();
            clientIdList.Remove(clientId);

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = clientIdList.ToArray()
                }
            };

            SendAnimatorClientRpc(stateName, value, clientRpcParams);
        }

        [ServerRpc]
        void SendAnimatorServerRpc(string stateName, float value, ulong clientId)
        {
            if (!IsHost)
                animator.SetFloat(stateName, value);

            List<ulong> clientIdList = NetworkManager.ConnectedClientsIds.ToList();
            clientIdList.Remove(clientId);

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = clientIdList.ToArray()
                }
            };

            SendAnimatorClientRpc(stateName, value, clientRpcParams);
        }

        [ServerRpc]
        void SendAnimatorServerRpc(string stateName, int value, ulong clientId)
        {
            if (!IsHost)
                animator.SetInteger(stateName, value);

            List<ulong> clientIdList = NetworkManager.ConnectedClientsIds.ToList();
            clientIdList.Remove(clientId);

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = clientIdList.ToArray()
                }
            };

            SendAnimatorClientRpc(stateName, value, clientRpcParams);
        }

        [ClientRpc]
        void SendAnimatorClientRpc(string stateName, bool value, ClientRpcParams clientRpcParams = default)
        {
            animator.SetBool(stateName, value);
        }

        [ClientRpc]
        void SendAnimatorClientRpc(string stateName, float value, ClientRpcParams clientRpcParams = default)
        {
            animator.SetFloat(stateName, value);
        }

        [ClientRpc]
        void SendAnimatorClientRpc(string stateName, int value, ClientRpcParams clientRpcParams = default)
        {
            animator.SetInteger(stateName, value);
        }
    }
}