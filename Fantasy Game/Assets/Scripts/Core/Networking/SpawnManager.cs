using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class SpawnManager : MonoBehaviour
    {
        public SpawnData[] spawnArray;
        
        [SerializeField] private bool spawnOnStart;

        public void SpawnObjects()
        {
            if (!NetworkManager.Singleton.IsServer) { return; }
            foreach (SpawnData spawnData in spawnArray)
            {
                GameObject g = Instantiate(spawnData.gameObject, spawnData.spawnPosition, Quaternion.Euler(spawnData.spawnRotation));
                g.name = spawnData.gameObject.name;
                NetworkObject networkObject = g.GetComponent<NetworkObject>();
                networkObject.Spawn(true);
            }
        }

        private void Start()
        {
            if (!spawnOnStart) { return; }
            if (!NetworkManager.Singleton.IsServer) { return; }
            foreach (SpawnData spawnData in spawnArray)
            {
                GameObject g = Instantiate(spawnData.gameObject, spawnData.spawnPosition, Quaternion.Euler(spawnData.spawnRotation));
                g.name = spawnData.gameObject.name;
                NetworkObject networkObject = g.GetComponent<NetworkObject>();
                networkObject.Spawn(true);
            }
        }

        private void OnDrawGizmos()
        {
            foreach (SpawnData spawnData in spawnArray)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawSphere(spawnData.spawnPosition, 0.5f);
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(spawnData.spawnPosition, Quaternion.Euler(spawnData.spawnRotation) * Vector3.forward * 2);
                Gizmos.color = Color.red;
                Gizmos.DrawRay(spawnData.spawnPosition, Quaternion.Euler(spawnData.spawnRotation) * Vector3.right * 2);
                Gizmos.color = Color.green;
                Gizmos.DrawRay(spawnData.spawnPosition, Quaternion.Euler(spawnData.spawnRotation) * Vector3.up * 2);
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