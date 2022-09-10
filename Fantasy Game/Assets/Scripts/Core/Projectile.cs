using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Projectile : MonoBehaviour
    {
        public GameObject inflicter;
        public float damage;

        bool damageRunning;

        private void OnTriggerEnter(Collider other)
        {
            if (damageRunning) { return; }
            damageRunning = true;

            if (other.attachedRigidbody)
            {
                Attributes hit = other.attachedRigidbody.transform.GetComponent<Attributes>();
                if (hit)
                {
                    hit.InflictDamage(damage, inflicter);
                }
            }            

            Destroy(gameObject);
        }
    }
}