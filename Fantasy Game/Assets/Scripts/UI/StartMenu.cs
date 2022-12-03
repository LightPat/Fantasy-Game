using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LightPat.Core;
using Unity.Netcode;
using TMPro;
using System.Linq;

namespace LightPat.UI
{
    public class StartMenu : Menu
    {
        public GameObject settingsMenu;
        public GameObject sceneTransition;
        public string transitionClipName;
        public TMP_InputField playerNameInput;
        public TMP_InputField IPAddressInput;

        public void OpenSettingsMenu()
        {
            GameObject _settings = Instantiate(settingsMenu);
            _settings.GetComponent<DisplaySettingsMenu>().SetLastMenu(gameObject);

            gameObject.SetActive(false);
        }

        public void StartClient()
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(playerNameInput.text);
            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = IPAddressInput.text;
            //NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UNET.UNetTransport>().ConnectAddress = IPAddressInput.text;
            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log("Started Client, looking for " + IPAddressInput.text);
                //StartCoroutine(WaitForAnimation(transitionClipName)); // This isn't reached due to network scene synchronization
            }
        }

        public void StartServer()
        {
            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = IPAddressInput.text;
            //NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UNET.UNetTransport>().ConnectAddress = IPAddressInput.text;
            if (NetworkManager.Singleton.StartServer())
            {
                Debug.Log("Started Server at " + IPAddressInput.text);
                NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
            }
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

            ClientManager.Singleton.QueueClient(clientId, new ClientData(System.Text.Encoding.ASCII.GetString(connectionData), false));
        }

        private IEnumerator WaitForAnimation(string animationName)
        {
            GameObject instantiated = Instantiate(sceneTransition, transform);
            instantiated.GetComponent<Animator>().Play(animationName);
            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(instantiated.GetComponent<Animator>().GetCurrentAnimatorClipInfo(0)[0].clip.length);
        }

        private void Start()
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback += (id) => { ClientManager.Singleton.ClientConnectCallback(); };
            NetworkManager.Singleton.OnClientDisconnectCallback += (id) => { ClientManager.Singleton.ClientDisconnectCallback(id); };
        }
    }
}
