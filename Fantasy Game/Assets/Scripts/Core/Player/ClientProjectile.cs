using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core.Player
{
    public class ClientProjectile : Projectile
    {
        public AudioClip hitmarkerSound;
        public float hitmarkerVolume = 1;
        public float hitmarkerTime = 1;

        private NetworkVariable<Vector3> startForceNetworked = new NetworkVariable<Vector3>();
        private NetworkVariable<ulong> inflicterNetworkId = new NetworkVariable<ulong>();

        // Start gets called after spawn
        public override void OnNetworkSpawn()
        {
            impactSoundAudioClipIndex.OnValueChanged += OnSoundAudioClipIndexChage;

            // Propogate startForce variable change to clients since it is changed before network spawn
            if (IsServer)
            {
                inflicterNetworkId.Value = inflicter.NetworkObjectId;
                startForceNetworked.Value = startForce;
            }
            
            StartCoroutine(WaitForInstantiation());

            startPos = transform.position;
        }

        public override void OnNetworkDespawn()
        {
            impactSoundAudioClipIndex.OnValueChanged -= OnSoundAudioClipIndexChage;

            if (impactSoundAudioClipIndex.Value == -1) { return; }

            AudioManager.Singleton.PlayClipAtPoint(AudioManager.Singleton.networkAudioClips[impactSoundAudioClipIndex.Value], transform.position, 0.8f);
        }

        protected override IEnumerator WaitForInstantiation()
        {
            yield return new WaitUntil(() => projectileInstantiatedNetworked.Value);
            // Wait for network variable changes to hit this client
            yield return new WaitUntil(() => startForceNetworked.Value != Vector3.zero);

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

        bool despawnSent;
        private void Update()
        {
            if (!inflicter) { return; }

            // Play bullet whizz sound if the local player is near it
            if (!flyByClipPlayed)
            {
                Collider[] allHits = Physics.OverlapSphere(transform.position, 10, -1, QueryTriggerInteraction.Ignore);
                foreach (Collider c in allHits)
                {
                    NetworkObject colliderObj = c.GetComponentInParent<NetworkObject>();
                    if (colliderObj)
                    {
                        if (colliderObj.IsLocalPlayer)
                        {
                            if (!flyByClipPlayed)
                            {
                                if (colliderObj == inflicter) { continue; }
                                AudioManager.Singleton.PlayClipAtPoint(flyBySound, transform.position, 0.5f);
                                flyByClipPlayed = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (despawnSent) { return; }
            if (!IsOwner) { return; }

            if (Vector3.Distance(startPos, transform.position) > maxDestroyDistance)
            {
                despawnSent = true;
                DespawnSelfServerRpc();
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

            if (!hit)
                impactSoundAudioClipIndex.Value = System.Array.IndexOf(AudioManager.Singleton.networkAudioClips, impactSound);

            despawnSent = true;
            DespawnSelfServerRpc();
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

        [ServerRpc]
        private void DespawnSelfServerRpc()
        {
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