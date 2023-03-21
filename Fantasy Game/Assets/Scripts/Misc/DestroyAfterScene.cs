using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LightPat.Misc
{
    public class DestroyAfterScene : MonoBehaviour
    {
        public string sceneNameToDestroyAfter;

        void Start()
        {
            DontDestroyOnLoad(gameObject);

            // Move this object back to being a scene object after the scene change has ocurred (this will destroy the object after the scene has been exited)
            SceneManager.activeSceneChanged += OnSceneChange;
        }

        void OnSceneChange(Scene current, Scene next)
        {
            if (next.name == sceneNameToDestroyAfter)
            {
                SceneManager.MoveGameObjectToScene(gameObject, next);
            }
        }
    }
}

