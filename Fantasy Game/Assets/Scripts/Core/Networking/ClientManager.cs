using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class ClientManager : NetworkBehaviour
    {
        public Dictionary<ulong, ClientData> clientDataDictionary { get; private set; }

        private static ClientManager _singleton;

        public static ClientManager Singleton
        {
            get
            {
                if (_singleton == null)
                {
                    Debug.Log("Client Manager is Null");
                }

                return _singleton;
            }
        }

        private void Awake()
        {
            _singleton = this;
            DontDestroyOnLoad(gameObject);
            clientDataDictionary = new Dictionary<ulong, ClientData>();
        }

        public void AddClient(ulong clientId, ClientData clientData)
        {
            clientDataDictionary.Add(clientId, clientData);
        }

        public ClientData GetClient(ulong clientId)
        {
            return clientDataDictionary[clientId];
        }

        public void RemoveClient(ulong clientId)
        {
            clientDataDictionary.Remove(clientId);
        }
    }

    public class ClientData
    {
        public string playerName;

        public ClientData(string playerName)
        {
            this.playerName = playerName;
        }
    }
}
