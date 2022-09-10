using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public abstract class Menu : MonoBehaviour
    {
        [HideInInspector] public GameObject childMenu;
        protected GameObject lastMenu;

        public void QuitGame()
        {
            Application.Quit();
        }

        public void SetLastMenu(GameObject lm)
        {
            lastMenu = lm;
        }

        public void GoBackToLastMenu()
        {
            Destroy(gameObject);
            if (lastMenu == null) { return; }
            lastMenu.SetActive(true);
        }
    }
}
