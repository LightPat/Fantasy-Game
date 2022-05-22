using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

namespace LightPat.Interactables
{
    [RequireComponent(typeof(TextDialogue))]
    public class LoadTextDialogueInteractable : Interactable
    {
        public override void Invoke()
        {
            GetComponent<TextDialogue>().SendDialogue();
        }
    }
}
