using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core.Player
{
    public class CaptureTheFlagBase : NetworkBehaviour
    {
        public Team team;
        public Flag flagPrefab;
        [SerializeField] private Vector3 flagLocalPosition;

        private CaptureTheFlagManager gameManager;
        private Flag currentFlag;

        public void RefreshFlag()
        {
            GameObject newFlag = Instantiate(flagPrefab.gameObject, transform.transform, true);
            newFlag.transform.localPosition = flagLocalPosition;
            currentFlag = newFlag.GetComponent<Flag>();
            currentFlag.SetBase(this);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PickUpFlagServerRpc(ulong networkObjectId)
        {
            Destroy(currentFlag.GetComponent<Rigidbody>());
            currentFlag.transform.SetParent(NetworkManager.SpawnManager.SpawnedObjects[networkObjectId].GetComponent<HumanoidWeaponAnimationHandler>().spineStow, true);
            PickUpFlagClientRpc(networkObjectId);
        }

        [ServerRpc]
        public void ReturnFlagServerRpc()
        {
            Destroy(currentFlag.gameObject);
            ReturnFlagClientRpc();
        }

        [ClientRpc]
        void PickUpFlagClientRpc(ulong networkObjectId)
        {
            Destroy(currentFlag.GetComponent<Rigidbody>());
            currentFlag.transform.SetParent(NetworkManager.SpawnManager.SpawnedObjects[networkObjectId].GetComponent<HumanoidWeaponAnimationHandler>().spineStow, true);
        }

        [ClientRpc]
        void ReturnFlagClientRpc()
        {
            Destroy(currentFlag.gameObject);
        }

        private void Start()
        {
            gameManager = FindObjectOfType<CaptureTheFlagManager>();
            RefreshFlag();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) { return; }

            if (other.attachedRigidbody)
            {
                Flag scoringFlag = other.attachedRigidbody.GetComponentInChildren<Flag>();
                if (scoringFlag)
                {
                    if (scoringFlag.team != team)
                    {
                        if (scoringFlag.scored) { return; }
                        scoringFlag.scored = true;
                        gameManager.IncrementScore(team);
                        DestroyFlagClientRpc(scoringFlag.GetComponentInParent<NetworkObject>().NetworkObjectId);
                        Destroy(scoringFlag.gameObject);
                    }
                }
            }
        }

        [ClientRpc]
        void DestroyFlagClientRpc(ulong networkObjectId)
        {
            Destroy(NetworkManager.SpawnManager.SpawnedObjects[networkObjectId].GetComponentInChildren<Flag>().gameObject);
        }
    }
}