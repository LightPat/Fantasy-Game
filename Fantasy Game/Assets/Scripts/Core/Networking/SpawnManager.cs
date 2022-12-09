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
                GameObject g = Instantiate(spawnData.gameObject, spawnData.spawnPosition, Quaternion.Euler(spawnData.spawnPosition));
                g.GetComponent<NetworkObject>().Spawn(true);
            }
        }
    }

    [System.Serializable]
    public class SpawnData
    {
        public GameObject gameObject;
        public Vector3 spawnPosition;
        public Quaternion spawnRotation;
    }
}