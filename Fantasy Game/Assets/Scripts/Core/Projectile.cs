using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class Projectile : NetworkBehaviour
    {
        public float maxDestroyDistance = 300;
        public AudioClip hitmarkerSound;
        public float hitmarkerVolume = 1;
        public float hitmarkerTime;

        [HideInInspector] public NetworkObject inflicter;
        [HideInInspector] public Weapon originWeapon;
        [HideInInspector] public Vector3 startForce;
        [HideInInspector] public float damage;

        protected bool damageRunning;
        protected Vector3 startPos; // Despawn bullet after a certain distance traveled

        // Start gets called after spawn
        public override void OnNetworkSpawn()
        {
            // Propogate startForce variable change to clients since it is changed before network spawn
            if (IsServer)
                GetComponent<Rigidbody>().AddForce(startForce, ForceMode.VelocityChange);

            startPos = transform.position;
        }

        protected Vector3 originalScale;
        private void Awake()
        {
            originalScale = transform.localScale;
            transform.localScale = Vector3.zero;
            StartCoroutine(WaitToChangeScale());
        }

        private IEnumerator WaitToChangeScale()
        {
            yield return new WaitUntil(() => GetComponent<Rigidbody>().velocity.magnitude > 0);
            transform.localScale = originalScale;
        }

        private void Update()
        {
            if (!IsOwner) { return; }

            if (Vector3.Distance(startPos, transform.position) > maxDestroyDistance)
                NetworkObject.Despawn(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) { return; }
            if (!IsSpawned) { return; }
            if (other.isTrigger) { return; }
            if (other.GetComponent<Projectile>()) { return; }

            // Use rigidbody in case object is parented to another rigidbody
            Attributes hit = other.GetComponentInParent<Attributes>();
            if (hit)
            {
                if (!inflicter) { return; } // If we haven't added force (network variables haven't been synced)
                if (hit.gameObject == inflicter.gameObject) { return; }
                if (damageRunning) { return; }
                damageRunning = true;

                bool damageSuccess = hit.InflictDamage(damage, gameObject, inflicter.gameObject);
            }

            NetworkObject.Despawn(true);
        }
    }
}