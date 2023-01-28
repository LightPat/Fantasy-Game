using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class Projectile : NetworkBehaviour
    {
        public float maxDestroyDistance = 300;

        [HideInInspector] public NetworkObject inflicter { get; protected set; }
        [HideInInspector] public Weapon originWeapon { get; protected set; }
        [HideInInspector] public Vector3 startForce { get; protected set; }
        [HideInInspector] public float damage { get; protected set; }

        protected bool damageRunning;
        protected Vector3 startPos; // Despawn bullet after a certain distance traveled
        protected bool projectileInstantiated;

        public void InstantiateProjectile(NetworkObject inflicter, Weapon originWeapon, Vector3 startForce, float damage)
        {
            this.inflicter = inflicter;
            this.originWeapon = originWeapon;
            this.startForce = startForce;
            this.damage = damage;
            projectileInstantiated = true;
        }

        // Start gets called after spawn
        public override void OnNetworkSpawn()
        {
            // Propogate startForce variable change to clients since it is changed before network spawn
            if (IsServer)
                StartCoroutine(WaitForInstantiation());

            startPos = transform.position;
        }

        protected Vector3 originalScale;
        protected void Awake()
        {
            originalScale = transform.localScale;
            transform.localScale = Vector3.zero;
        }

        protected virtual IEnumerator WaitForInstantiation()
        {
            yield return new WaitUntil(() => projectileInstantiated);
            GetComponent<Rigidbody>().AddForce(startForce, ForceMode.VelocityChange);
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