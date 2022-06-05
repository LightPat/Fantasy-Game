using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LightPat.Core
{
    public class TextDialogue : MonoBehaviour
    {
        public TextDialogueManager dialogueManager;
        [Header("Text0-0|Text0+0")]
        // Text, personality index to change, value
        public string[] dialogue;

        public void SendDialogue()
        {
            dialogueManager.LoadDialogue(dialogue);
        }
    }
}
