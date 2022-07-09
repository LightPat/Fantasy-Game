using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using TMPro;
using System;

namespace LightPat.UI
{
    public class ControlSettingsMenu : Menu
    {
        public TMP_InputField sensitivityInput;
        private PlayerController player;
        private float originalSensitivity;

        private void Start()
        {
            player = FindObjectOfType<PlayerController>();
            sensitivityInput.text = player.sensitivity.ToString();
            originalSensitivity = player.sensitivity;
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
    }
}
