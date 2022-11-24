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

        public void LeaveLobby()
        {
            SceneManager.LoadScene("StartMenu");
        }

        public void ToggleReady()
        {
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            ClientData oldClientData = ClientManager.Singleton.GetClient(localClientId);
            ClientManager.Singleton.ToggleReady(localClientId, new ClientData(oldClientData.clientName, !oldClientData.ready, oldClientData.lobbyLeader));
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
                Image image = nameIcon.transform.Find("ReadyIcon").GetComponent<Image>();

                if (clientData.ready)
                    image.color = new Color(0, 255, 0, 255);
                else
                    image.color = new Color(255, 0, 0, 255);

                for (int i = 0; i < playerNamesParent.childCount; i++)
                {
                    playerNamesParent.GetChild(i).localPosition = new Vector3(iconSpacing.x, -(i + 1) * iconSpacing.y, 0);
                }
            }
        }
    }
}