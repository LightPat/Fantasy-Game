using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public abstract class Menu : MonoBehaviour
    {
        protected GameObject lastMenu;

        public void QuitGame()
        {
            Application.Quit();
        }

        public void setLastMenu(GameObject lm)
        {
            lastMenu = lm;
        }

        public void goBackToLastMenu()
        {
            Destroy(gameObject);
            if (lastMenu == null) { return; }
            lastMenu.SetActive(true);
        }
    }
}
