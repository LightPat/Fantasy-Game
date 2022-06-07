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
            if (dialogue.IndexOf("|") == -1) { dialogueOptions.Clear(); return dialogue; }

            int count = 1;
            string splice = count.ToString() + ": ";
            foreach (char c in dialogue)
            {
                if (c == '|')
                {
                    dialogueOptions.Add(splice);
                    // Remove last 3 characters which are the personality data
                    buttonOptions[count - 1].SetActive(true);
                    buttonOptions[count - 1].transform.GetChild(0).GetComponent<TextMeshProUGUI>().SetText(splice.Substring(0, splice.Length - 3));
                    buttonOptions[count - 1].GetComponent<Button>().onClick.AddListener(() => ChooseOption(count - 1));
                    count++;
                    splice = count.ToString() + ": ";
                    continue;
                }
                splice += c;
            }
            // Add onclick listener for buttons
            // SetActive when dialogue options aren't being shown
            // Add space for prompt above buttons
            dialogueOptions.Add(splice);
            buttonOptions[count - 1].SetActive(true);
            buttonOptions[count - 1].transform.GetChild(0).GetComponent<TextMeshProUGUI>().SetText(splice.Substring(0, splice.Length - 3));
            buttonOptions[count - 1].GetComponent<Button>().onClick.AddListener(() => ChooseOption(count - 1));
            return "";
        }

        private void GoToNextLine()
        {
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
        
        private void ChooseOption(int optionIndex)
        {
            int indexToChange = int.Parse(dialogueOptions[optionIndex].Substring(dialogueOptions[optionIndex].Length - 3, 1));
            sbyte value = sbyte.Parse(dialogueOptions[optionIndex].Substring(dialogueOptions[optionIndex].Length - 2, 2));

            // Change personality values
            GetComponent<Attributes>().personalityValues[indexToChange] += value;

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
