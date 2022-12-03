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

        private IEnumerator SpawnLocalPlayerOnSceneChange(string sceneName)
        {
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == sceneName);
            SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        private void Awake()
        {
            _singleton = this;
            DontDestroyOnLoad(gameObject);
            foreach (GameObject g in playerPrefabOptions)
            {
                NetworkManager.Singleton.AddNetworkPrefab(g);
            }

            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback += (id) => { ClientConnectCallback(); };
            NetworkManager.Singleton.OnClientDisconnectCallback += (id) => { ClientDisconnectCallback(id); };
        }

        void ClientConnectCallback()
        {
            if (!IsServer) { return; }
            KeyValuePair<ulong, ClientData> valuePair = queuedClientData.Dequeue();
            clientDataDictionary.Add(valuePair.Key, valuePair.Value);
            Debug.Log(valuePair.Value.clientName + " has connected.");
            AddClientRpc(valuePair.Key, valuePair.Value);
            SynchronizeClientDictionaries();
            if (lobbyLeaderId.Value == 0) { RefreshLobbyLeader(); }
        }

        void ClientDisconnectCallback(ulong clientId)
        {
            Debug.Log(clientDataDictionary[clientId].clientName + " has disconnected.");
            if (!IsServer) { return; }
            clientDataDictionary.Remove(clientId);
            if (clientId == lobbyLeaderId.Value) { RefreshLobbyLeader(); }
            RemoveClientRpc(clientId);
        }

        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            // The client identifier to be authenticated
            var clientId = request.ClientNetworkId;

            // Additional connection data defined by user code
            var connectionData = request.Payload;

            // Your approval logic determines the following values
            response.Approved = true;
            response.CreatePlayerObject = false;

            // The prefab hash value of the NetworkPrefab, if null the default NetworkManager player prefab is used
            response.PlayerPrefabHash = null;

            // Position to spawn the player object (if null it uses default of Vector3.zero)
            response.Position = Vector3.zero;

            // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
            response.Rotation = Quaternion.identity;

            // If additional approval steps are needed, set this to true until the additional steps are complete
            // once it transitions from true to false the connection approval response will be processed.
            response.Pending = false;

            QueueClient(clientId, new ClientData(System.Text.Encoding.ASCII.GetString(connectionData), false));
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
