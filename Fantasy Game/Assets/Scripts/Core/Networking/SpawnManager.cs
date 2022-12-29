using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class SpawnManager : MonoBehaviour
    {
        public SpawnData[] spawnArray;

        private void Start()
        {
            if (!NetworkManager.Singleton.IsServer) { return; }
            foreach (SpawnData spawnData in spawnArray)
            {
                GameObject g = Instantiate(spawnData.gameObject, spawnData.spawnPosition, Quaternion.Euler(spawnData.spawnRotation));
                NetworkObject networkObject = g.GetComponent<NetworkObject>();
                networkObject.Spawn(true);
            }
        }

        private void OnDrawGizmos()
        {
            foreach (SpawnData spawnData in spawnArray)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(spawnData.spawnPosition, 1);
            }
        }
    }

    [System.Serializable]
    public class SpawnData
    {
        public GameObject gameObject;
        public Vector3 spawnPosition;
        public Vector3 spawnRotation;
    }
}