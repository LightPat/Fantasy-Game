using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class ClientManager : NetworkBehaviour
    {
        private Dictionary<ulong, ClientData> clientDataDictionary = new Dictionary<ulong, ClientData>();
        private Queue<KeyValuePair<ulong, ClientData>> queuedClientData = new Queue<KeyValuePair<ulong, ClientData>>();

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

        public void QueueClient(ulong clientId, ClientData clientData)
        {
            queuedClientData.Enqueue(new KeyValuePair<ulong, ClientData>(clientId, clientData));
        }

        public void PopClient()
        {
            if (!IsServer) { return; }
            KeyValuePair<ulong, ClientData> valuePair = queuedClientData.Dequeue();
            clientDataDictionary.Add(valuePair.Key, valuePair.Value);
            AddClientRpc(valuePair.Key, valuePair.Value);
            SynchronizeClients();
        }

        public void RemoveClient(ulong clientId)
        {
            if (!IsServer) { return; }
            clientDataDictionary.Remove(clientId);
            RemoveClientRpc(clientId);
        }

        public Dictionary<ulong, ClientData> GetClientDataDictionary()
        {
            return clientDataDictionary;
        }

        public ClientData GetClient(ulong clientId)
        {
            return clientDataDictionary[clientId];
        }

        public void OverwriteClientData(ulong clientId, ClientData clientData)
        {
            if (IsServer) { Debug.LogError("Calling a server Rpc from the server, this is supposed to only be called from clients"); return; }
            OverwriteClientServerRpc(clientId, clientData);
        }

        private void Awake()
        {
            _singleton = this;
            DontDestroyOnLoad(gameObject);
        }

        private void SynchronizeClients()
        {
            foreach (ulong clientId in clientDataDictionary.Keys)
            {
                SynchronizeClientRpc(clientId, clientDataDictionary[clientId]);
            }
        }

        [ClientRpc] void SynchronizeClientRpc(ulong clientId, ClientData clientData) { clientDataDictionary[clientId] = clientData; }
        [ClientRpc] void AddClientRpc(ulong clientId, ClientData clientData) { clientDataDictionary.Add(clientId, clientData); }
        [ClientRpc] void RemoveClientRpc(ulong clientId) { clientDataDictionary.Remove(clientId); }

        [ServerRpc(RequireOwnership = false)]
        void OverwriteClientServerRpc(ulong clientId, ClientData clientData)
        {
            clientDataDictionary[clientId] = clientData;
            SynchronizeClients();
        }
    }

    public struct ClientData : INetworkSerializable
    {
        public string clientName;
        public bool ready;
        public bool lobbyLeader;

        public ClientData(string clientName, bool ready, bool lobbyLeader)
        {
            this.clientName = clientName;
            this.ready = ready;
            this.lobbyLeader = lobbyLeader;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientName);
            serializer.SerializeValue(ref ready);
            serializer.SerializeValue(ref lobbyLeader);
        }
    }
}
