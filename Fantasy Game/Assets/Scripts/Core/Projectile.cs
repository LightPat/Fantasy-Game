using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class Projectile : NetworkBehaviour
    {
        public GameObject inflicter;
        public Weapon originWeapon;
        public float damage;
        public float maxDestroyDistance = 300;
        public AudioClip hitmarkerSound;
        public float hitmarkerVolume = 1;
        public float hitmarkerTime;

        [HideInInspector] public Vector3 startForce;

        bool damageRunning;
        Vector3 startPos; // Despawn bullet after a certain distance traveled

        public override void OnNetworkSpawn()
        {
            GetComponent<Rigidbody>().AddForce(startForce, ForceMode.VelocityChange);
            startPos = transform.position;
        }

        private void FixedUpdate()
        {
            if (!IsServer) { return; }

            if (Vector3.Distance(startPos, transform.position) > maxDestroyDistance)
                NetworkObject.Despawn(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) { return; }

            if (other.GetComponent<Projectile>()) { return; }

            if (other.attachedRigidbody)
            {
                if (other.attachedRigidbody.gameObject == inflicter) { return; }
                if (damageRunning) { return; }
                damageRunning = true;

                Attributes hit = other.attachedRigidbody.transform.GetComponent<Attributes>();
                if (hit)
                {
                    hit.InflictDamage(damage, inflicter, this);
                    // Change this to be a damage inflicted sound that happens on the target
                    //AudioManager.Instance.PlayClipAtPoint(hitmarkerData.hitmarkerSound, transform.position, hitmarkerData.hitmarkerVolume);
                    inflicter.SendMessage("OnProjectileHit", new HitmarkerData(hitmarkerSound, hitmarkerVolume, hitmarkerTime));
                }
            }

            NetworkObject.Despawn(true);
        }
    }

    public class HitmarkerData
    {
        public AudioClip hitmarkerSound;
        public float hitmarkerVolume;
        public float hitmarkerTime;

        public HitmarkerData(AudioClip hitmarkerSound, float hitmarkerVolume, float hitmarkerTime)
        {
            this.hitmarkerSound = hitmarkerSound;
            this.hitmarkerVolume = hitmarkerVolume;
            this.hitmarkerTime = hitmarkerTime;
        }
    }
}