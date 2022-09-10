using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightPat.Core.Player
{
    public class ActionMapHandler : MonoBehaviour
    {
        PlayerInput playerInput;
        GameObject HUD;

        private void Start()
        {
            playerInput = GetComponent<PlayerInput>();
            HUD = transform.Find("PlayerHUD").gameObject;
            if (HUD == null)
                Debug.LogError("Player HUD not found " + this);
        }

        public GameObject inventoryPrefab;
        GameObject inventoryObject;
        bool inventoryEnabled;
        void OnInventoryToggle()
        {
            if (pauseEnabled) { return; }

            inventoryEnabled = !inventoryEnabled;
            if (inventoryEnabled)
            {
                Cursor.lockState = CursorLockMode.None;
                HUD.SetActive(false);
                inventoryObject = Instantiate(inventoryPrefab);
                playerInput.SwitchCurrentActionMap("Inventory");
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                HUD.SetActive(true);
                Destroy(inventoryObject);
                playerInput.SwitchCurrentActionMap("First Person");
            }
        }

        public GameObject pausePrefab;
        GameObject pauseObject;
        bool pauseEnabled;
        void OnPause()
        {
            if (inventoryEnabled) { return; }

            pauseEnabled = !pauseEnabled;
            if (pauseEnabled)
            {
                Cursor.lockState = CursorLockMode.None;
                HUD.SetActive(false);
                pauseObject = Instantiate(pausePrefab);
                playerInput.SwitchCurrentActionMap("Menu");
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                HUD.SetActive(true);
                GameObject childMenu = pauseObject.GetComponent<Menu>().childMenu;
                if (childMenu)
                    Destroy(childMenu);
                Destroy(pauseObject);
                playerInput.SwitchCurrentActionMap("First Person");
            }
        }
    }
}