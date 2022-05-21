using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

namespace LightPat.Core
{
    public class TextDialogueManager : MonoBehaviour
    {
        public TextMeshProUGUI displayObject;
        private Queue<string> dialogueQueue = new Queue<string>();

        /// <summary>
        /// Public method for this manager to recieve dialogue from other objects in the scene
        /// </summary>
        /// <param name="newDialogue">An array of strings that is each step in the dialogue</param>
        public void LoadDialogue(string[] newDialogue)
        {
            foreach (string s in newDialogue)
            {
                dialogueQueue.Enqueue(s);
            }

            if (displayObject.text == "")
            {
                displayObject.SetText(dialogueQueue.Dequeue());
            }

            GetComponent<PlayerInput>().SwitchCurrentActionMap("Text Dialogue");
        }
        
        void OnNextLine()
        {
            if (dialogueQueue.Count == 0)
            {
                displayObject.SetText("");
                GetComponent<PlayerInput>().SwitchCurrentActionMap("First Person");
                return;
            }

            string s = dialogueQueue.Dequeue();
            displayObject.SetText(s);
        }
    }
}
