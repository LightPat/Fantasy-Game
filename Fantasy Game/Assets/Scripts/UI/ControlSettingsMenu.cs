using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using TMPro;
using System;
using UnityEngine.UI;

namespace LightPat.UI
{
    public class ControlSettingsMenu : Menu
    {
        public TMP_InputField sensitivityInput;
        public Toggle crouchToggle;
        public Toggle sprintToggle;

        private PlayerController player;
        private float originalSensitivity;

        private void Start()
        {
            player = FindObjectOfType<PlayerController>();
            sensitivityInput.text = player.sensitivity.ToString();
            originalSensitivity = player.sensitivity;

            crouchToggle.isOn = player.toggleCrouch;
            sprintToggle.isOn = player.toggleSprint;
        }

        public void SensitivityChange()
        {
            try
            {
                player.sensitivity = float.Parse(sensitivityInput.text);
            }
            catch (FormatException)
            {
                player.sensitivity = originalSensitivity;
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
