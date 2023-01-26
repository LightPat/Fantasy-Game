using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core.Player
{
    public class Projectile : NetworkBehaviour
    {
        public NetworkObject inflicter;
        public Weapon originWeapon;
        public float damage;
        public float maxDestroyDistance = 300;
        public AudioClip hitmarkerSound;
        public float hitmarkerVolume = 1;
        public float hitmarkerTime;

        [HideInInspector] public Vector3 startForce;

        private NetworkVariable<Vector3> startForceNetworked = new NetworkVariable<Vector3>();
        private NetworkVariable<ulong> inflicterNetworkId = new NetworkVariable<ulong>();

        bool damageRunning;
        Vector3 startPos; // Despawn bullet after a certain distance traveled

        // Start gets called after spawn
        public override void OnNetworkSpawn()
        {
            // Propogate startForce variable change to clients since it is changed before network spawn
            if (IsServer)
            {
                inflicterNetworkId.Value = inflicter.NetworkObjectId;
                startForceNetworked.Value = startForce;
            }
            
            if (IsOwner)
                StartCoroutine(WaitToAddForce());

            startPos = transform.position;
        }

        private IEnumerator WaitToAddForce()
        {
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
                    }
                }
            }
            
            if (projectileSpawnPoint)
            {
                transform.position = projectileSpawnPoint.position;
                transform.rotation = projectileSpawnPoint.rotation;
            }

            GetComponent<Rigidbody>().AddForce(startForceNetworked.Value, ForceMode.VelocityChange);
        }

        Vector3 originalScale;
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

        bool despawnSent;
        private void Update()
        {
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
            if (other.GetComponent<Projectile>()) { return; }

            if (other.attachedRigidbody)
            {
                if (!inflicter) { return; }
                if (other.attachedRigidbody.gameObject == inflicter.gameObject) { return; }
                if (damageRunning) { return; }
                damageRunning = true;

                Attributes hit = other.attachedRigidbody.transform.GetComponent<Attributes>();
                if (hit)
                {
                    InflictDamageServerRpc(hit.NetworkObject.NetworkObjectId);
                }
            }
            despawnSent = true;
            DespawnSelfServerRpc();
        }

        [ServerRpc]
        private void InflictDamageServerRpc(ulong inflictedNetworkObjectId)
        {
            bool damageSuccess = NetworkManager.SpawnManager.SpawnedObjects[inflictedNetworkObjectId].GetComponent<Attributes>().InflictDamage(damage, gameObject, inflicter.gameObject);

            if (inflicter.TryGetComponent(out NetworkObject playerNetObj) & damageSuccess)
            {
                if (playerNetObj.IsPlayerObject)
                    inflicter.SendMessage("PlayHitmarker", new HitmarkerData(System.Array.IndexOf(AudioManager.Singleton.networkAudioClips, hitmarkerSound), hitmarkerVolume, hitmarkerTime, playerNetObj.OwnerClientId));
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