using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LightPat.Core
{
    public class TextDialogue : MonoBehaviour
    {
        public TextDialogueManager dialogueManager;
        public string[] dialogue;

        public void SendDialogue()
        {
            dialogueManager.LoadDialogue(dialogue);
        }
    }
}
