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
        protected bool projectileInstantiated;
        protected NetworkVariable<bool> projectileInstantiatedNetworked = new NetworkVariable<bool>();
        protected NetworkVariable<int> impactSoundAudioClipIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public void InstantiateProjectile(NetworkObject inflicter, Weapon originWeapon, Vector3 startForce, float damage)
        {
            //if (!IsServer) { Debug.LogError("Instantiating a projectile from somewhere other than the server"); }

            this.inflicter = inflicter;
            this.originWeapon = originWeapon;
            this.startForce = startForce;
            this.damage = damage;
            projectileInstantiated = true;
            StartCoroutine(WaitForSpawn());
        }

        private IEnumerator WaitForSpawn()
        {
            yield return new WaitUntil(() => IsSpawned);
            projectileInstantiatedNetworked.Value = projectileInstantiated;
        }

        // Start gets called after spawn
        public override void OnNetworkSpawn()
        {
            impactSoundAudioClipIndex.OnValueChanged += OnSoundAudioClipIndexChage;
            // Propogate startForce variable change to clients since it is changed before network spawn
            if (IsServer)
                StartCoroutine(WaitForInstantiation());

            startPos = transform.position;
        }

        public override void OnNetworkDespawn()
        {
            impactSoundAudioClipIndex.OnValueChanged -= OnSoundAudioClipIndexChage;
        }

        [HideInInspector] public Vector3 originalScale;
        protected void Awake()
        {
            originalScale = transform.localScale;
            transform.localScale = Vector3.zero;
        }

        protected virtual IEnumerator WaitForInstantiation()
        {
            yield return new WaitUntil(() => projectileInstantiatedNetworked.Value);
            GetComponent<Rigidbody>().AddForce(startForce, ForceMode.VelocityChange);
            transform.localScale = originalScale;
        }

        protected bool flyByClipPlayed;
        private void Update()
        {
            // Play bullet whizz sound if the local player is near it
            if (!flyByClipPlayed)
            {
                Collider[] allHits = Physics.OverlapSphere(transform.position, 0.5f, -1, QueryTriggerInteraction.Ignore);
                foreach (Collider c in allHits)
                {
                    NetworkObject colliderObj = c.GetComponentInParent<NetworkObject>();
                    if (colliderObj)
                    {
                        if (colliderObj.IsLocalPlayer)
                        {
                            if (!flyByClipPlayed)
                            {
                                AudioManager.Singleton.PlayClipAtPoint(flyBySound, transform.position, 0.5f);
                                flyByClipPlayed = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            if (Vector3.Distance(startPos, transform.position) > maxDestroyDistance)
            {
                if (IsSpawned)
                    NetworkObject.Despawn(true);
                else
                    Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) { return; }
            if (!IsSpawned) { return; }
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
                NetworkObject.Despawn(true); // If we hit a player despawn TODO replace this with another audioclip
            }
        }

        protected void OnSoundAudioClipIndexChage(int prev, int current)
        {
            AudioManager.Singleton.PlayClipAtPoint(AudioManager.Singleton.networkAudioClips[current], transform.position, 0.8f);
            Debug.Log(current);
            if (IsServer)
                StartCoroutine(DespawnAfterSound());
        }

        private IEnumerator DespawnAfterSound()
        {
            yield return null;
            NetworkObject.Despawn(true);
        }
    }
}