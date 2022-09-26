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
        Vector3 startPos; // Despawn bullet after a certain distance traveled
        
        private void Start()
        {
            startPos = transform.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Projectile>()) { return; }
            if (damageRunning) { return; }
            damageRunning = true;

            if (other.attachedRigidbody)
            {
                Attributes hit = other.attachedRigidbody.transform.GetComponent<Attributes>();
                if (hit)
                {
                    hit.InflictDamage(damage, inflicter, gameObject);
                }
            }

            Destroy(gameObject);
        }
    }
}