using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using Unity.Netcode;

namespace LightPat.Triggers
{
    public class SetActiveNetworkObjectTrigger : NetworkBehaviour
    {
        public NetworkObject[] objectsToSetActive;

        public override void OnNetworkSpawn()
        {
            // I have no idea why I need to do this
            StartCoroutine(WaitAfterSpawn());
        }

        private IEnumerator WaitAfterSpawn()
        {
            foreach (NetworkObject g in objectsToSetActive)
            {
                yield return new WaitUntil(() => g.IsSpawned);
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                g.gameObject.SetActive(false);
            }
        }

        bool ran;
        private void OnTriggerEnter(Collider other)
        {
            if (ran) { return; }

            NetworkObject otherNetworkObj = other.GetComponentInParent<NetworkObject>();
            if (!otherNetworkObj) { return; }
            if (!otherNetworkObj.IsPlayerObject) { return; }

            ran = true;

            ActivateServerRpc();
        }

        bool serverRunning;
        [ServerRpc(RequireOwnership = false)]
        void ActivateServerRpc()
        {
            if (serverRunning) { return; }
            serverRunning = true;

            foreach (NetworkObject g in objectsToSetActive)
            {
                g.gameObject.SetActive(true);
            }

            NetworkObject.Despawn(true);
        }

        [ClientRpc]
        void ActivateClientRpc()
        {
            foreach (NetworkObject g in objectsToSetActive)
            {
                g.gameObject.SetActive(true);
            }
        }
    }
}