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
            if (!IsServer | !IsSpawned) { return; }
            if (other.isTrigger) { return; }

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

                    NetworkObject playerNetObj;
                    if (inflicter.TryGetComponent(out playerNetObj))
                    {
                        if (playerNetObj.IsPlayerObject)
                            inflicter.SendMessage("PlayHitmarker", new HitmarkerData(System.Array.IndexOf(AudioManager.Singleton.networkAudioClips, hitmarkerSound), hitmarkerVolume, hitmarkerTime, playerNetObj.OwnerClientId));
                    }
                }
            }
            NetworkObject.Despawn(true);
        }
    }

    public struct HitmarkerData
    {
        public int hitmarkerSoundIndex;
        public float hitmarkerVolume;
        public float hitmarkerTime;
        public ulong targetClient;

        public HitmarkerData(int hitmarkerSoundIndex, float hitmarkerVolume, float hitmarkerTime, ulong targetClient)
        {
            this.hitmarkerSoundIndex = hitmarkerSoundIndex;
            this.hitmarkerVolume = hitmarkerVolume;
            this.hitmarkerTime = hitmarkerTime;
            this.targetClient = targetClient;
        }
    }
}