using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Tutorial : MonoBehaviour
    {
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
            GetComponent<TextDialogue>().SendDialogue();
            Destroy(gameObject);
        }
    }
}
