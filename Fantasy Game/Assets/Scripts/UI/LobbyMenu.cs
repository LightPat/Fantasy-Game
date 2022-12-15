using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

namespace LightPat.UI
{
    public class LobbyMenu : Menu
    {
        public GameObject playerNamePrefab;
        public Transform playerNamesParent;
        public Vector3 iconSpacing;
        public GameObject startButton;
        public GameObject readyButton;
        public GameObject changeTeamButton;
        public GameObject WaitingToStartText;
        public TMP_Dropdown gameModeDropdown;
        public TMP_Dropdown playerModelDropdown;
        [Header("Capture The Flag")]
        public GameObject test;

        GameObject playerModel;
        MaterialColorChange colors;
        Vector3 cameraPositionOffset;

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
            if (gameModeDropdown.options[gameModeDropdown.value].text == "CaptureTheFlag")
                ClientManager.Singleton.ChangeSceneServerRpc(NetworkManager.Singleton.LocalClientId, "Level1", true);
            else if (gameModeDropdown.options[gameModeDropdown.value].text == "HordeMode")
                Debug.LogError("Horde mode isn't done yet");
            else if (gameModeDropdown.options[gameModeDropdown.value].text == "GhostInTheGraveyard")
                Debug.LogError("Ghost in the graveyard isn't done yet");
        }

        public void UpdatePlayerDisplay()
        {
            if (playerModel)
                Destroy(playerModel);
            playerModel = Instantiate(ClientManager.Singleton.playerPrefabOptions[playerModelDropdown.value]);
            playerModel.SendMessage("TurnOnDisplayModelMode");
            colors = playerModel.AddComponent<MaterialColorChange>();
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            ClientManager.Singleton.OverwriteClientDataServerRpc(localClientId, ClientManager.Singleton.GetClient(localClientId).ChangePlayerPrefabOption(playerModelDropdown.value));
            ColorCycle(false);
        }

        int colorIndex = 1;
        public void ColorCycle(bool incrementIndex = true)
        {
            if (incrementIndex)
                colorIndex++;

            if (colorIndex == 5)
                colorIndex = 1;

            Color teamColor = Color.white;
            if (ClientManager.Singleton.GetClient(NetworkManager.Singleton.LocalClientId).team == Team.Red)
                teamColor = Color.red;
            else if (ClientManager.Singleton.GetClient(NetworkManager.Singleton.LocalClientId).team == Team.Blue)
                teamColor = Color.blue;

            if (colorIndex == 0)
            {
                colors.ResetColors();
            }
            else if (colorIndex == 1)
            {
                colors.materialColors = new Color[] { Color.black, teamColor };
                colors.Apply();
            }
            else if (colorIndex == 2)
            {
                colors.materialColors = new Color[] { teamColor, Color.black };
                colors.Apply();
            }
            else if (colorIndex == 3)
            {
                colors.materialColors = new Color[] { Color.white, teamColor };
                colors.Apply();
            }
            else if (colorIndex == 4)
            {
                colors.materialColors = new Color[] { teamColor, Color.white };
                colors.Apply();
            }

            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            ClientManager.Singleton.OverwriteClientDataServerRpc(localClientId, ClientManager.Singleton.GetClient(localClientId).ChangeColors(colors.materialColors));
        }

        public void UpdateGameModeValue()
        {
            GameMode chosenGameMode;
            System.Enum.TryParse(gameModeDropdown.options[gameModeDropdown.value].text, out chosenGameMode);
            ClientManager.Singleton.UpdateGameModeServerRpc(chosenGameMode);
        }

        public void ChangeTeam()
        {
            bool nextTeam = false;
            bool reached = false;
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            Team originalTeam = ClientManager.Singleton.GetClient(localClientId).team;
            foreach (Team team in System.Enum.GetValues(typeof(Team)).Cast<Team>())
            {
                if (nextTeam)
                {
                    reached = true;
                    ClientManager.Singleton.OverwriteClientDataServerRpc(localClientId, ClientManager.Singleton.GetClient(localClientId).ChangeTeam(team));
                    break;
                }
                if (team == ClientManager.Singleton.GetClient(NetworkManager.Singleton.LocalClientId).team)
                    nextTeam = true;
            }

            if (!reached)
                ClientManager.Singleton.OverwriteClientDataServerRpc(localClientId, ClientManager.Singleton.GetClient(localClientId).ChangeTeam(Team.Red));

            StartCoroutine(WaitForTeamChange(originalTeam, localClientId));
        }

        IEnumerator WaitForTeamChange(Team originalTeam, ulong clientId)
        {
            yield return new WaitUntil(() => originalTeam != ClientManager.Singleton.GetClient(clientId).team);
            ColorCycle(false);
        }

        private void Start()
        {
            List<TMP_Dropdown.OptionData> playerModelOptions = new List<TMP_Dropdown.OptionData>();
            foreach (GameObject playerPrefab in ClientManager.Singleton.playerPrefabOptions)
            {
                playerModelOptions.Add(new TMP_Dropdown.OptionData(playerPrefab.name));
            }
            playerModelDropdown.ClearOptions();
            playerModelDropdown.AddOptions(playerModelOptions);

            List<TMP_Dropdown.OptionData> gameModes = new List<TMP_Dropdown.OptionData>();
            foreach (GameMode gameMode in System.Enum.GetValues(typeof(GameMode)).Cast<GameMode>())
            {
                gameModes.Add(new TMP_Dropdown.OptionData(gameMode.ToString()));
            }
            gameModeDropdown.ClearOptions();
            gameModeDropdown.AddOptions(gameModes);

            cameraPositionOffset = Camera.main.transform.localPosition;

            if (NetworkManager.Singleton.IsClient)
                StartCoroutine(WaitForClientConnection());
        }

        private IEnumerator WaitForClientConnection()
        {
            yield return new WaitUntil(() => ClientManager.Singleton.GetClientDataDictionary().ContainsKey(NetworkManager.Singleton.LocalClientId));
            UpdatePlayerDisplay();
            UpdateGameModeValue();
        }

        private void Update()
        {
            // Only let lobby leader change the game mode
            if (NetworkManager.Singleton.LocalClientId == ClientManager.Singleton.lobbyLeaderId.Value)
                gameModeDropdown.interactable = true;
            else
                gameModeDropdown.interactable = false;

            // Set game mode dropdown
            if (gameModeDropdown.options[gameModeDropdown.value].text != ClientManager.Singleton.gameMode.Value.ToString())
            {
                for (int i = 0; i < gameModeDropdown.options.Count; i++)
                {
                    if (ClientManager.Singleton.gameMode.Value.ToString() == gameModeDropdown.options[i].text)
                    {
                        gameModeDropdown.SetValueWithoutNotify(i);
                        break;
                    }
                }
            }

            // Put main camera in right spot
            if (playerModel)
                Camera.main.transform.position = playerModel.transform.position + cameraPositionOffset;

            // Player names logic
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

                // Enable switch teams button if we are the local client for that nameIcon
                if (valuePair.Key == NetworkManager.Singleton.LocalClientId)
                    nameIcon.GetComponentInChildren<Button>().interactable = true;
                else
                    nameIcon.GetComponentInChildren<Button>().interactable = false;

                // Set the color of the team button
                Color teamColor = Color.black;
                if (ClientManager.Singleton.GetClient(valuePair.Key).team == Team.Red)
                {
                    teamColor = Color.red;
                }
                else if (ClientManager.Singleton.GetClient(valuePair.Key).team == Team.Blue)
                {
                    teamColor = Color.blue;
                }
                nameIcon.GetComponentInChildren<Button>().GetComponent<Image>().color = teamColor;

                // Change color of ready icon
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
                
                // Only make crown icon visible on the lobby leader
                if (valuePair.Key == ClientManager.Singleton.lobbyLeaderId.Value)
                    nameIcon.transform.Find("CrownIcon").GetComponent<RawImage>().color = new Color(255, 255, 255, 255);
                else
                    nameIcon.transform.Find("CrownIcon").GetComponent<RawImage>().color = new Color(255, 255, 255, 0);

                // Enable start button if everyone is ready
                if (!valuePair.Value.ready)
                    everyoneIsReady = false;

                // Set positions of all name icons
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