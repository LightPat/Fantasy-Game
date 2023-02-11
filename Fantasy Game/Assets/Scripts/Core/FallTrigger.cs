using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class FallTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!NetworkManager.Singleton.IsServer) { return; }

            Attributes objectAttributes = other.GetComponentInParent<Attributes>();
            if (objectAttributes)
            {
                if (objectAttributes.IsSpawned)
                    objectAttributes.InflictDamage(100000000000000, gameObject);
            }
            else
            {
                Destroy(other.gameObject);
            }
        }
    }
}