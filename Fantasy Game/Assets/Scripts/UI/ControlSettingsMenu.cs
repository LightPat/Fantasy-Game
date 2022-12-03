using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using TMPro;
using System;
using UnityEngine.UI;
using LightPat.Core.Player;
using Unity.Netcode;

namespace LightPat.UI
{
    public class ControlSettingsMenu : Menu
    {
        public TMP_InputField sensitivityInput;
        public Toggle crouchToggle;
        public Toggle sprintToggle;

        private PlayerController player;
        private ServerCamera serverCamera;
        private float originalSensitivity;

        private void Start()
        {
            if (NetworkManager.Singleton.IsClient)
            {
                foreach (PlayerController playerController in FindObjectsOfType<PlayerController>())
                {
                    if (playerController.GetComponent<NetworkObject>().IsOwner)
                    {
                        player = playerController;
                    }
                }
                sensitivityInput.text = player.sensitivity.ToString();
                originalSensitivity = player.sensitivity;
                crouchToggle.isOn = player.toggleCrouch;
                sprintToggle.isOn = player.toggleSprint;
            }
            else // If we are the server
            {
                serverCamera = FindObjectOfType<ServerCamera>();
                if (serverCamera)
                {
                    sensitivityInput.text = serverCamera.sensitivity.ToString();
                    originalSensitivity = serverCamera.sensitivity;
                    crouchToggle.gameObject.SetActive(false);
                    sprintToggle.gameObject.SetActive(false);
                    return;
                }
            }
        }

        public void SensitivityChange()
        {
            try
            {
                if (player)
                    player.sensitivity = float.Parse(sensitivityInput.text);
                else if (serverCamera)
                    serverCamera.sensitivity = float.Parse(sensitivityInput.text);
            }
            catch (FormatException)
            {
                if (player)
                    player.sensitivity = originalSensitivity;
                else if (serverCamera)
                    serverCamera.sensitivity = originalSensitivity;
            }
        }

        public void SetCrouchMode()
        {
            player.toggleCrouch = crouchToggle.isOn;
        }

        public void SetSprintMode()
        {
            player.toggleSprint = sprintToggle.isOn;
        }
    }
}
