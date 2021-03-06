using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LightPat.Core;

namespace LightPat.UI
{
    public class StartMenu : Menu
    {
        public GameObject settingsMenu;
        public GameObject sceneTransition;
        public string transitionClipName;

        public void OpenSettingsMenu()
        {
            GameObject _settings = Instantiate(settingsMenu);
            _settings.GetComponent<DisplaySettingsMenu>().SetLastMenu(gameObject);

            gameObject.SetActive(false);
        }

        public void StartGame()
        {
            StartCoroutine(WaitForAnimation(transitionClipName));
        }

        private IEnumerator WaitForAnimation(string animationName)
        {
            GameObject instantiated = Instantiate(sceneTransition, transform);
            instantiated.GetComponent<Animator>().Play(animationName);
            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(instantiated.GetComponent<Animator>().GetCurrentAnimatorClipInfo(0)[0].clip.length);
            SceneManager.LoadScene("Level1");
        }
    }
}
