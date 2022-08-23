using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using LightPat.Core.TextDialogue;

namespace LightPat.ScriptedEvents
{
    public class Tutorial : MonoBehaviour
    {
        public PlayerInput playerInput;
        public GameObject sceneTransition;
        public string transitionClipName;

        private void Start()
        {
            StartCoroutine(StartTutorial(transitionClipName));
        }

        private IEnumerator StartTutorial(string animationName)
        {
            GameObject instantiated = Instantiate(sceneTransition, transform);
            instantiated.GetComponent<Animator>().Play(animationName);
            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(instantiated.GetComponent<Animator>().GetCurrentAnimatorClipInfo(0)[0].clip.length);
            // Enable playerInput since it is disabled by default
            playerInput.enabled = true;
            GetComponent<TextDialogue>().SendDialogue();
            Destroy(gameObject);


        }
    }
}
