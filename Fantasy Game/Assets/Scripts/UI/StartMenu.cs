using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.UI
{
    public class StartMenu : MonoBehaviour
    {
        public GameObject settingsMenu;

        public void OpenSettingsMenu()
        {
            GameObject _settings = Instantiate(settingsMenu);
            _settings.GetComponent<DisplaySettingsMenu>().setLastMenu(gameObject);

            gameObject.SetActive(false);
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}
