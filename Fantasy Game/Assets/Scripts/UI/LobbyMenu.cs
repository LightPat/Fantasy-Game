using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;

namespace LightPat.UI
{
    public class LobbyMenu : Menu
    {
        public GameObject playerNamePrefab;
        public Transform playerNamesParent;
        public Vector3 iconSpacing;
        public GameObject startButton;

        public void LeaveLobby()
        {
            Destroy(NetworkManager.Singleton.gameObject);
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("StartMenu");
        }

        public void ToggleReady()
        {
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            ClientData oldClientData = ClientManager.Singleton.GetClient(localClientId);
            ClientManager.Singleton.OverwriteClientData(localClientId, new ClientData(oldClientData.clientName, !oldClientData.ready, oldClientData.lobbyLeader));
        }

        public void StartGame()
        {
            NetworkManager.Singleton.SceneManager.LoadScene("Level1", LoadSceneMode.Single);
        }

        private void Update()
        {
            foreach (Transform child in playerNamesParent)
            {
                Destroy(child.gameObject);
            }

            foreach (ClientData clientData in ClientManager.Singleton.GetClientDataDictionary().Values)
            {
                GameObject nameIcon = Instantiate(playerNamePrefab, playerNamesParent);
                nameIcon.GetComponentInChildren<TextMeshProUGUI>().SetText(clientData.clientName);

                if (clientData.ready)
                    nameIcon.transform.Find("ReadyIcon").GetComponent<Image>().color = new Color(0, 255, 0, 255);
                else
                    nameIcon.transform.Find("ReadyIcon").GetComponent<Image>().color = new Color(255, 0, 0, 255);
                if (clientData.lobbyLeader)
                    nameIcon.transform.Find("CrownIcon").GetComponent<RawImage>().color = new Color(255, 255, 255, 255);
                else
                    nameIcon.transform.Find("CrownIcon").GetComponent<RawImage>().color = new Color(255, 255, 255, 0);

                for (int i = 0; i < playerNamesParent.childCount; i++)
                {
                    playerNamesParent.GetChild(i).localPosition = new Vector3(iconSpacing.x, -(i + 1) * iconSpacing.y, 0);
                }
            }
        }
    }
}