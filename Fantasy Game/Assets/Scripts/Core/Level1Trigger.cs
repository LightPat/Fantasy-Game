using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Level1Trigger : MonoBehaviour
    {
        bool ran;
        private void OnTriggerEnter(Collider other)
        {
            if (ran) { return; }
            ran = true;
            FindObjectOfType<SpawnManager>().SpawnObjects();
        }
    }
}