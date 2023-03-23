using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

namespace LightPat.Triggers
{
    public class Level1Trigger : MonoBehaviour
    {
        bool ran;
        private void OnTriggerEnter(Collider other)
        {
            if (!other.GetComponentInParent<Attributes>()) { return; }
            if (ran) { return; }
            ran = true;
            FindObjectOfType<SpawnManager>().SpawnObjects();
        }
    }
}