using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class Projectile : NetworkBehaviour
    {
        public AudioClip impactSound;
        public AudioClip flyBySound;
        public float maxDestroyDistance = 300;

        [HideInInspector] public NetworkObject inflicter { get; protected set; }
        [HideInInspector] public Weapon originWeapon { get; protected set; }
        [HideInInspector] public Vector3 startForce { get; protected set; }
        [HideInInspector] public float damage { get; protected set; }

        protected bool damageRunning;
        protected Vector3 startPos; // Despawn bullet after a certain distance traveled
        protected NetworkVariable<bool> projectileInstantiated = new NetworkVariable<bool>();
        protected NetworkVariable<int> impactSoundAudioClipIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        protected NetworkVariable<Vector3> startForceNetworked = new NetworkVariable<Vector3>();
        protected NetworkVariable<ulong> inflicterNetworkId = new NetworkVariable<ulong>();

        // This is called when the projectile hits a wall
        protected bool impactSoundPlayed;
        protected void OnImpactSoundAudioClipIndexChange(int prev, int current)
        {
            if (impactSoundPlayed) { return; }
            impactSoundPlayed = true;
            AudioManager.Singleton.PlayClipAtPoint(AudioManager.Singleton.networkAudioClips[current], transform.position, 0.8f);
            if (IsServer)
            {
                soundEventCalled = true;
                StartCoroutine(DespawnAfterSound());
            }
        }

        // Despawn after 1 frame to allow change() method to be called
        protected bool soundEventCalled;
        private IEnumerator DespawnAfterSound()
        {
            yield return null;
            NetworkObject.Despawn(true);
        }

        // Used to instantiate a projectile (supply data). Once this is done we will execute more code after the projectile is spawned
        public void InstantiateProjectile(NetworkObject inflicter, Weapon originWeapon, Vector3 startForce, float damage)
        {
            // Access through singleton because this object may not be spawned yet
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("Instantiating a projectile from somewhere other than the server"); return; }

            this.inflicter = inflicter;
            this.originWeapon = originWeapon;
            this.startForce = startForce;
            this.damage = damage;
            StartCoroutine(WaitForSpawn());
        }

        private IEnumerator WaitForSpawn()
        {
            yield return new WaitUntil(() => IsSpawned);
            startForceNetworked.Value = startForce;
            inflicterNetworkId.Value = inflicter.NetworkObjectId;
            projectileInstantiated.Value = true;
        }

        // Once this is done we will execute more code after the projectile is instantiated
        public override void OnNetworkSpawn()
        {
            impactSoundAudioClipIndex.OnValueChanged += OnImpactSoundAudioClipIndexChange;
            // Propogate startForce variable change to clients since it is changed before network spawn
            if (IsServer)
                StartCoroutine(WaitForInstantiation());

            startPos = transform.position;

            if (!IsOwnedByServer) { Debug.LogError("Projectiles should only belong to the server. Use ClientProjectile instead."); }
        }

        protected virtual IEnumerator WaitForInstantiation()
        {
            yield return new WaitUntil(() => projectileInstantiated.Value);
            GetComponent<Rigidbody>().AddForce(startForce, ForceMode.VelocityChange);
            transform.localScale = originalScale;
        }

        public override void OnNetworkDespawn()
        {
            impactSoundAudioClipIndex.OnValueChanged -= OnImpactSoundAudioClipIndexChange;
        }

        [HideInInspector] public Vector3 originalScale;
        protected void Awake()
        {
            originalScale = transform.localScale;
            transform.localScale = Vector3.zero;
        }

        protected bool flyByClipPlayed;
        protected bool despawnSent;
        protected void Update()
        {
            if (!inflicter) { return; }

            // Play bullet whizz sound if the local player is near it
            if (!flyByClipPlayed)
            {
                Collider[] allHits = Physics.OverlapSphere(transform.position, 1, -1, QueryTriggerInteraction.Ignore);
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

            if (!IsSpawned) { return; }
            if (!IsOwner) { return; }
            if (despawnSent) { return; }

            if (Vector3.Distance(startPos, transform.position) > maxDestroyDistance)
            {
                despawnSent = true;
                DespawnSelfServerRpc();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }
            if (other.isTrigger) { return; }
            if (other.GetComponent<Projectile>()) { return; }

            // Use rigidbody in case object is parented to another rigidbody
            Attributes hit = other.GetComponentInParent<Attributes>();
            bool damageSuccess = false;
            if (hit)
            {
                if (!inflicter) { return; } // If we haven't added force (network variables haven't been synced)
                if (hit.gameObject == inflicter.gameObject) { return; }
                if (damageRunning) { return; }
                damageRunning = true;

                damageSuccess = hit.InflictDamage(this);
            }

            if (!damageSuccess)
            {
                impactSoundAudioClipIndex.Value = System.Array.IndexOf(AudioManager.Singleton.networkAudioClips, impactSound);
            }
            else
            {
                impactSoundAudioClipIndex.Value = System.Array.IndexOf(AudioManager.Singleton.networkAudioClips, impactSound);
                //NetworkObject.Despawn(true); // If we hit a player despawn TODO replace this with another audioclip
            }
        }

        [ServerRpc] protected void DespawnSelfServerRpc() { NetworkObject.Despawn(true); }
    }
}