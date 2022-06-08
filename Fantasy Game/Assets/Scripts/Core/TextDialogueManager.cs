using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System;
using UnityEngine.UI;

namespace LightPat.Core
{
    public class TextDialogueManager : MonoBehaviour
    {
        public GameObject HUD;
        public TextMeshProUGUI displayObject;
        public GameObject[] buttonOptions;
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
                displayObject.SetText(parseDialogueOptions(dialogueQueue.Dequeue()));
            }

            GetComponent<PlayerInput>().SwitchCurrentActionMap("Text Dialogue");
            HUD.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
        }

        private string parseDialogueOptions(string dialogue)
        {
            // Always clear our dialogue options when parsing a new string
            dialogueOptions.Clear();
            // If this isn't an option string then just return string as is
            if (dialogue.IndexOf("|") == -1) { return dialogue; }

            int count = 0;
            string splice = "";
            string header = "";
            // Loop through the line of dialogue, since this an option string
            foreach (char c in dialogue)
            {
                // If we encouter a |, we want to append where we are in the loop as an option then reset the splice variable to the empty string and repeat the process
                if (c == '|')
                {
                    // If count is 0, we are parsing the header, so we just want to increment count and continue
                    if (count == 0)
                    {
                        count++;
                        continue;
                    }
                    else
                    {
                        // Add spliced string to dialogue options for parsing in ChooseOption()
                        dialogueOptions.Add(splice);
                        // Set button to active
                        buttonOptions[count - 1].SetActive(true);
                        // Remove last 3 characters from splice which are the personality data
                        buttonOptions[count - 1].transform.GetChild(0).GetComponent<TextMeshProUGUI>().SetText(splice.Substring(0, splice.Length - 3));
                        count++;
                        splice = count.ToString() + ": ";
                        continue;
                    }
                }
                // If count is 0, we are parsing the header
                if (count == 0)
                {
                    header += c;
                }
                else
                {
                    splice += c;
                }
            }

            dialogueOptions.Add(splice);
            buttonOptions[count - 1].SetActive(true);
            buttonOptions[count - 1].transform.GetChild(0).GetComponent<TextMeshProUGUI>().SetText(splice.Substring(0, splice.Length - 3));
            return header;
        }

        private void GoToNextLine()
        {
            // If there is no more dialogue left to loop through
            if (dialogueQueue.Count == 0)
            {
                displayObject.SetText("");
                GetComponent<PlayerInput>().SwitchCurrentActionMap("First Person");
                HUD.SetActive(true);
                Cursor.lockState = CursorLockMode.Locked;
                return;
            }

            displayObject.SetText(parseDialogueOptions(dialogueQueue.Dequeue()));
        }
        
        public void ChooseOption(int optionIndex)
        {
            int indexToChange = int.Parse(dialogueOptions[optionIndex].Substring(dialogueOptions[optionIndex].Length - 3, 1));
            sbyte value = sbyte.Parse(dialogueOptions[optionIndex].Substring(dialogueOptions[optionIndex].Length - 2, 2));

            // Change personality values
            GetComponent<Attributes>().personalityValues[indexToChange] += value;

            // Since we are choosing an option, we have to reset the buttons in case the next dialogue isn't a choice
            for (int i = 0; i < dialogueOptions.Count; i++)
            {
                buttonOptions[i].SetActive(false);
            }

            GoToNextLine();
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
            if (keyPressed >= dialogueOptions.Count) { return; }

            ChooseOption(keyPressed);
        }
    }
}
