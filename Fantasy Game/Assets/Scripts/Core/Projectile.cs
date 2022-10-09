using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Projectile : MonoBehaviour
    {
        public GameObject inflicter;
        public float damage;
        public float maxDestroyDistance = 300;

        bool damageRunning;
        Vector3 startPos; // Despawn bullet after a certain distance traveled
        
        private void Start()
        {
            startPos = transform.position;
        }

        private void FixedUpdate()
        {
            if (Vector3.Distance(startPos, transform.position) > maxDestroyDistance)
                Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Projectile>()) { return; }

            if (other.attachedRigidbody)
            {
                if (other.attachedRigidbody.gameObject == inflicter) { return; }
                if (damageRunning) { return; }
                damageRunning = true;

                Attributes hit = other.attachedRigidbody.transform.GetComponent<Attributes>();
                if (hit)
                {
                    Debug.Log(damage + " " + inflicter + " " + this);
                    hit.InflictDamage(damage, inflicter, this);
                }
            }

            Destroy(gameObject);
        }
    }
}