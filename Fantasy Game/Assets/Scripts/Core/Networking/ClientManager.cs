using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Linq;

namespace LightPat.Core
{
    public class ClientManager : NetworkBehaviour
    {
        public GameObject[] playerPrefabOptions;
        public NetworkVariable<ulong> lobbyLeaderId { get; private set; } = new NetworkVariable<ulong>();
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

        public Dictionary<ulong, ClientData> GetClientDataDictionary()
        {
            return clientDataDictionary;
        }

        public ClientData GetClient(ulong clientId)
        {
            return clientDataDictionary[clientId];
        }

        public void QueueClient(ulong clientId, ClientData clientData)
        {
            queuedClientData.Enqueue(new KeyValuePair<ulong, ClientData>(clientId, clientData));
        }

        public void ClientConnectCallback()
        {
            if (!IsServer) { return; }
            KeyValuePair<ulong, ClientData> valuePair = queuedClientData.Dequeue();
            clientDataDictionary.Add(valuePair.Key, valuePair.Value);
            Debug.Log(valuePair.Value.clientName + " has connected.");
            AddClientRpc(valuePair.Key, valuePair.Value);
            SynchronizeClientDictionaries();
            if (lobbyLeaderId.Value == 0) { RefreshLobbyLeader(); }
        }

        public void ClientDisconnectCallback(ulong clientId)
        {
            Debug.Log(clientDataDictionary[clientId].clientName + " has disconnected.");
            if (!IsServer) { return; }
            clientDataDictionary.Remove(clientId);
            if (clientId == lobbyLeaderId.Value) { RefreshLobbyLeader(); }
            RemoveClientRpc(clientId);
        }

        public override void OnNetworkSpawn()
        {
            lobbyLeaderId.OnValueChanged += OnLobbyLeaderChanged;
            if (IsServer) { RefreshLobbyLeader(); }
        }

        private void RefreshLobbyLeader()
        {
            if (clientDataDictionary.Count > 0)
                lobbyLeaderId.Value = clientDataDictionary.Keys.Min();
            else
                lobbyLeaderId.Value = 0;
        }

        private void OnLobbyLeaderChanged(ulong previous, ulong current)
        {
            if (current > 0)
                Debug.Log(clientDataDictionary[current].clientName + " is the new lobby leader.");
        }

        private void SynchronizeClientDictionaries()
        {
            foreach (ulong clientId in clientDataDictionary.Keys)
            {
                SynchronizeClientRpc(clientId, clientDataDictionary[clientId]);
            }
        }

        private void Awake()
        {
            _singleton = this;
            DontDestroyOnLoad(gameObject);
            foreach (GameObject g in playerPrefabOptions)
            {
                NetworkManager.Singleton.AddNetworkPrefab(g);
            }
        }

        [ClientRpc] void SynchronizeClientRpc(ulong clientId, ClientData clientData) { clientDataDictionary[clientId] = clientData; }
        [ClientRpc] void AddClientRpc(ulong clientId, ClientData clientData) { Debug.Log(clientData.clientName + " has connected."); clientDataDictionary.Add(clientId, clientData); }
        [ClientRpc] void RemoveClientRpc(ulong clientId) { clientDataDictionary.Remove(clientId); }
        [ClientRpc] void SpawnAllPlayersOnSceneChangeClientRpc(string sceneName) { StartCoroutine(SpawnLocalPlayerOnSceneChange(sceneName)); }

        [ServerRpc(RequireOwnership = false)]
        public void OverwriteClientDataServerRpc(ulong clientId, ClientData clientData)
        {
            clientDataDictionary[clientId] = clientData;
            SynchronizeClientDictionaries();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChangeSceneServerRpc(ulong clientId, string sceneName, bool spawnPlayers)
        {
            if (clientId != lobbyLeaderId.Value) { Debug.LogError("You can only change the scene if you are the lobby leader!"); return; }
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            if (spawnPlayers)
                SpawnAllPlayersOnSceneChangeClientRpc(sceneName);
        }

        [ServerRpc(RequireOwnership = false)]
        void SpawnPlayerServerRpc(ulong clientId)
        {
            GameObject g = Instantiate(playerPrefabOptions[0]);
            g.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);
        }

        private IEnumerator SpawnLocalPlayerOnSceneChange(string sceneName)
        {
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == sceneName);
            SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    public struct ClientData : INetworkSerializable
    {
        public string clientName;
        public bool ready;

        public ClientData(string clientName, bool ready)
        {
            this.clientName = clientName;
            this.ready = ready;
        }

        public ClientData ToggleReady()
        {
            ClientData copy = this;
            copy.ready = !copy.ready;
            return copy;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientName);
            serializer.SerializeValue(ref ready);
        }
    }
}
