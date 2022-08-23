using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LightPat.Core.TextDialogue
{
    public class TextDialogue : MonoBehaviour
    {
        public TextDialogueManager dialogueManager;
        [Header("Header|Text0-0|Text0+0")]
        // Header, text, personality index to change, value
        [TextArea(3,10)]
        public string[] dialogue;

        public void SendDialogue()
        {
            dialogueManager.LoadDialogue(dialogue);
        }
    }
}
