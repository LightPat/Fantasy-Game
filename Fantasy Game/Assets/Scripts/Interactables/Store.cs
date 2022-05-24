using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

namespace LightPat.Interactables
{
    public class Store : Interactable
    {
        public override void Invoke()
        {
            Debug.Log("Interacting with: " + gameObject.name);
        }
    }
}
