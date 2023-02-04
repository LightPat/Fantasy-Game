using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core.Player
{
    public class ClientProjectile : Projectile
    {
        [Header("Hitmarker Data")]
        public AudioClip hitmarkerSound;
        public float hitmarkerVolume = 1;
        public float hitmarkerTime = 1;

        public override void OnNetworkSpawn()
        {
            impactSoundAudioClipIndex.OnValueChanged += OnImpactSoundAudioClipIndexChange;
            StartCoroutine(WaitForInstantiation());
            startPos = transform.position;
        }

        protected override IEnumerator WaitForInstantiation()
        {
            yield return new WaitUntil(() => projectileInstantiated.Value);

            inflicter = NetworkManager.SpawnManager.SpawnedObjects[inflicterNetworkId.Value];

            // Make projectile follow spawn point until the spawn logic has been completed
            Transform projectileSpawnPoint = null;
            if (inflicter.TryGetComponent(out WeaponLoadout weaponLoadout))
            {
                if (weaponLoadout.equippedWeapon)
                {
                    if (weaponLoadout.equippedWeapon.TryGetComponent(out Gun gun))
                    {
                        projectileSpawnPoint = gun.projectileSpawnPoint;
                        originWeapon = gun.OnProjectileSpawn(this);
                    }
                }
            }
            
            if (IsOwner)
            {
                if (projectileSpawnPoint)
                {
                    transform.position = projectileSpawnPoint.position;
                    transform.rotation = projectileSpawnPoint.rotation;
                }

                GetComponent<Rigidbody>().AddForce(startForceNetworked.Value, ForceMode.VelocityChange);
                transform.localScale = originalScale;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (despawnSent) { return; }
            if (!IsOwner) { return; }
            if (!IsSpawned) { return; }
            if (other.isTrigger) { return; }
            if (other.GetComponent<ClientProjectile>()) { return; }

            // Use rigidbody in case object is parented to another rigidbody
            Attributes hit = other.GetComponentInParent<Attributes>();
            if (hit)
            {
                if (!inflicter) { return; } // If we haven't added force (network variables haven't been synced)
                if (hit.gameObject == inflicter.gameObject) { return; }
                if (damageRunning) { return; }
                damageRunning = true;

                InflictDamageServerRpc(hit.NetworkObjectId);
            }

            despawnSent = true;
            impactSoundAudioClipIndex.Value = System.Array.IndexOf(AudioManager.Singleton.networkAudioClips, impactSound);
        }

        [ServerRpc]
        private void InflictDamageServerRpc(ulong inflictedNetworkObjectId)
        {
            bool damageSuccess = NetworkManager.SpawnManager.SpawnedObjects[inflictedNetworkObjectId].GetComponent<Attributes>().InflictDamage(this);

            if (damageSuccess)
            {
                if (inflicter.IsPlayerObject)
                    inflicter.SendMessage("PlayHitmarker", new HitmarkerData(System.Array.IndexOf(AudioManager.Singleton.networkAudioClips, hitmarkerSound), hitmarkerVolume, hitmarkerTime, inflicter.OwnerClientId));
            }
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