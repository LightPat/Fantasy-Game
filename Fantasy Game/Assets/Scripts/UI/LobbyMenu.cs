using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace LightPat.UI
{
    public class LobbyMenu : Menu
    {
        public GameObject playerNamePrefab;
        public Transform playerNamesParent;
        public Vector3 iconSpacing;
        [Header("GameObject References")]
        public GameObject startButton;
        public GameObject readyButton;
        public GameObject WaitingToStartText;

        public void LeaveLobby()
        {
            Destroy(NetworkManager.Singleton.gameObject);
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("StartMenu");
        }

        public void ToggleReady()
        {
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            ClientManager.Singleton.OverwriteClientDataServerRpc(localClientId, ClientManager.Singleton.GetClient(localClientId).ToggleReady());
        }

        bool loadingGame;
        public void StartGame()
        {
            if (loadingGame) { return; }
            loadingGame = true;
            Debug.Log("Loading game");
            ClientManager.Singleton.ChangeSceneServerRpc(NetworkManager.Singleton.LocalClientId, "Level1", true);
        }

        private void Update()
        {
            foreach (Transform child in playerNamesParent)
            {
                Destroy(child.gameObject);
            }

            bool everyoneIsReady = true;
            if (ClientManager.Singleton.GetClientDataDictionary().Count == 0)
                everyoneIsReady = false;
            foreach (KeyValuePair<ulong, ClientData> valuePair in ClientManager.Singleton.GetClientDataDictionary())
            {
                GameObject nameIcon = Instantiate(playerNamePrefab, playerNamesParent);
                nameIcon.GetComponentInChildren<TextMeshProUGUI>().SetText(valuePair.Value.clientName);

                if (valuePair.Value.ready)
                {
                    Color newColor = new Color(0, 255, 0, 255);
                    nameIcon.transform.Find("ReadyIcon").GetComponent<Image>().color = newColor;
                    if (valuePair.Key == NetworkManager.Singleton.LocalClientId) // If this is the local player
                    {
                        readyButton.GetComponent<Image>().color = newColor;
                    }
                }
                else
                {
                    Color newColor = new Color(255, 0, 0, 255);
                    nameIcon.transform.Find("ReadyIcon").GetComponent<Image>().color = newColor;
                    if (valuePair.Key == NetworkManager.Singleton.LocalClientId) // If this is the local player
                    {
                        readyButton.GetComponent<Image>().color = newColor;
                    }
                }
                    
                if (valuePair.Key == ClientManager.Singleton.lobbyLeaderId.Value)
                    nameIcon.transform.Find("CrownIcon").GetComponent<RawImage>().color = new Color(255, 255, 255, 255);
                else
                    nameIcon.transform.Find("CrownIcon").GetComponent<RawImage>().color = new Color(255, 255, 255, 0);

                // Enable start button if everyone is ready
                if (!valuePair.Value.ready)
                    everyoneIsReady = false;
                //if (valuePair.Key == NetworkManager.Singleton.LocalClientId)

                for (int i = 0; i < playerNamesParent.childCount; i++)
                {
                    playerNamesParent.GetChild(i).localPosition = new Vector3(iconSpacing.x, -(i + 1) * iconSpacing.y, 0);
                }
            }

            if (everyoneIsReady)
            {
                if (NetworkManager.Singleton.LocalClientId == ClientManager.Singleton.lobbyLeaderId.Value)
                {
                    startButton.SetActive(true);
                }
                else
                {
                    // set waiting to start text to true
                    WaitingToStartText.SetActive(true);
                }
            }
            else
            {
                startButton.SetActive(false);
                WaitingToStartText.SetActive(false);
            }
        }
    }
}