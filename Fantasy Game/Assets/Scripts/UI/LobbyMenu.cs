using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

namespace LightPat.UI
{
    public class LobbyMenu : Menu
    {
        public GameObject playerNamePrefab;
        public Transform playerNamesParent;
        public Vector3 iconSpacing;

        List<GameObject> playerIcons = new List<GameObject>();

        public void LeaveLobby()
        {
            SceneManager.LoadScene("StartMenu");
        }

        public void ToggleReady(string playerName)
        {
            //Image image = playerIcons[playerList.IndexOf(playerName)].transform.Find("ReadyIcon").GetComponent<Image>();
            //if (image.color != new Color(0, 255, 0, 255))
            //    image.color = new Color(0, 255, 0, 255);
            //else
            //    image.color = new Color(255, 0, 0, 255);
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
                nameIcon.GetComponentInChildren<TextMeshProUGUI>().SetText(clientData.playerName);
                playerIcons.Add(nameIcon);
                for (int i = 0; i < playerNamesParent.childCount; i++)
                {
                    playerNamesParent.GetChild(i).localPosition = new Vector3(iconSpacing.x, -(i + 1) * iconSpacing.y, 0);
                }
            }
        }
    }
}