using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

namespace LightPat.Interactables
{
    public class Store : Interactable
    {
        public override void Invoke(GameObject invoker)
        {
            Debug.Log("Interacting with: " + gameObject.name);
        }
    }
}
