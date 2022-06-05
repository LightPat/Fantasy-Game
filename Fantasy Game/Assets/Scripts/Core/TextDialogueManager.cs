using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System;

namespace LightPat.Core
{
    public class TextDialogueManager : MonoBehaviour
    {
        public TextMeshProUGUI displayObject;
        private Queue<string> dialogueQueue = new Queue<string>();
        private List<string> dialogueOptions = new List<string>();

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
                string s = dialogueQueue.Dequeue();
                parseDialogueOptions(s);
                displayObject.SetText(s);
            }

            GetComponent<PlayerInput>().SwitchCurrentActionMap("Text Dialogue");
        }

        private void parseDialogueOptions(string dialogue)
        {
            if (dialogue.IndexOf("|") == -1) { dialogueOptions.Clear(); return; }

            int count = 1;
            string splice = count.ToString() + ": ";
            foreach (char c in dialogue)
            {
                if (c == '|')
                {
                    dialogueOptions.Add(splice);
                    count++;
                    splice = count.ToString() + ": ";
                    continue;
                }
                splice += c;
            }

            dialogueOptions.Add(splice);
        }

        private void GoToNextLine()
        {
            if (dialogueQueue.Count == 0)
            {
                displayObject.SetText("");
                GetComponent<PlayerInput>().SwitchCurrentActionMap("First Person");
                return;
            }

            string s = dialogueQueue.Dequeue();
            parseDialogueOptions(s);
            displayObject.SetText(s);
        }

        void OnNextLine()
        {
            if (dialogueOptions.Count != 0) { return; }
            GoToNextLine();
        }

        void OnChooseOption(InputValue inputValue)
        {
            int keyPressed = Convert.ToInt32(inputValue.Get()) - 1;

            // If we press a key that is out of the option range
            Debug.Log(keyPressed + " " + dialogueOptions.Count);
            if (keyPressed >= dialogueOptions.Count) { return; }

            int indexToChange = int.Parse(dialogueOptions[keyPressed].Substring(dialogueOptions[keyPressed].Length - 3, 1));
            sbyte value = sbyte.Parse(dialogueOptions[keyPressed].Substring(dialogueOptions[keyPressed].Length - 2, 2));

            // Change personality values
            GetComponent<Attributes>().personalityValues[indexToChange] += value;

            GoToNextLine();
        }
    }
}
