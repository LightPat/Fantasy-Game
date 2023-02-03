using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using Unity.Netcode;

namespace LightPat.Core
{
    public class Attributes : NetworkBehaviour
    {
        public Team team;
        //[Header("Affinity Scores")]
        [HideInInspector] public sbyte[] personalityValues;
        [HideInInspector] public sbyte[] physicalValues;
        [HideInInspector] public sbyte[] magicalValues;
        public bool blocking;
        [Header("Health")]
        public float maxHealth = 100f;
        [Header("World Space Label : Only assign for enemies/NPCs/mobs")]
        public Renderer healthRenderer;
        public TextMeshPro healthPointsWorldText;
        [Header("ScreenSpaceOverlay : Only assign for player/allies/bosses")]
        public Material imageMaterial;
        public TextMeshProUGUI healthPointsUIText;
        public AudioClip damageTakenSound;
        public AudioClip lowHealthSound;
        [Header("Collider Damage Multipliers")]
        public Collider headCollider;

        public NetworkVariable<float> HP { get; private set; } = new NetworkVariable<float>();
        bool invincible;

        public override void OnNetworkSpawn()
        {
            HP.OnValueChanged += OnHPChanged;
            HP.Value = maxHealth;
            if (NetworkObject.IsPlayerObject)
            {
                team = ClientManager.Singleton.GetClient(OwnerClientId).team;
            }
            StartCoroutine(SpawnProtection());
        }

        public override void OnNetworkDespawn()
        {
            HP.OnValueChanged -= OnHPChanged;
        }

        private IEnumerator SpawnProtection()
        {
            invincible = true;
            yield return new WaitForSeconds(0.1f);
            invincible = false;
        }

        private void Start()
        {
            // If we are a NPC, we edit the renderer's material instance
            if (healthRenderer != null)
            {
                healthRenderer.material.SetFloat("healthPercentage", HP.Value / maxHealth);
                healthPointsWorldText.SetText(HP.Value + " / " + maxHealth);
            }
            if (imageMaterial != null) // If we are the player, we have to edit the material directly (limitation of unity's canvas renderer)
            {
                imageMaterial.SetFloat("healthPercentage", HP.Value / maxHealth);
                healthPointsUIText.SetText(HP.Value + " / " + maxHealth);
            }
        }

        public bool InflictDamage(float damage, GameObject inflicter)
        {
            if (!IsServer) { Debug.LogError("Calling InflictDamage() from a client, use a ServerRpc"); return false; }
            if (invincible) { return false; }

            bool alreadyDead = HP.Value <= 0;
            float damageInflicted = 0;
            float damageAngle = Vector3.Angle(inflicter.transform.forward, transform.forward);
            SendMessage("OnAttacked", new OnAttackedData(inflicter.name, damageAngle));

            if (inflicter.TryGetComponent(out Attributes inflicterAttributes))
            {
                if (inflicterAttributes.team == team) { return false; }
            }

            if (blocking)
            {
                float[] array = new float[3] { 0, 90, 180 };
                float nearest = array.OrderBy(x => Mathf.Abs((long)x - damageAngle)).First();
                if (nearest != 180)
                {
                    HP.Value -= damage;
                    damageInflicted = damage;
                }
            }
            else
            {
                HP.Value -= damage;
                damageInflicted = damage;
            }

            if (HP.Value < 0)
            {
                HP.Value = 0;
                if (alreadyDead)
                    damageInflicted = 0;
            }

            if (HP.Value <= 0)
                SendMessage("OnDeath");

            if (inflicter.TryGetComponent(out NetworkObject inflicterNetworkObject))
            {
                if (inflicterNetworkObject.IsPlayerObject)
                {
                    if (damageInflicted > 0)
                    {
                        ClientManager.Singleton.AddDamage(inflicterNetworkObject.OwnerClientId, damageInflicted);
                        if (HP.Value == 0)
                        {
                            ClientManager.Singleton.AddDeaths(OwnerClientId, 1);
                            ClientManager.Singleton.AddKills(inflicterNetworkObject.OwnerClientId, 1);
                        }
                    }
                }
            }
            else
            {
                if (HP.Value == 0 & damageInflicted > 0)
                    ClientManager.Singleton.AddDeaths(OwnerClientId, 1);
            }

            return true;
        }

        public bool InflictDamage(Projectile projectile)
        {
            if (!IsServer) { Debug.LogError("Calling InflictDamage() from a client, use a ServerRpc"); return false; }
            if (invincible) { return false; }

            bool alreadyDead = HP.Value <= 0;
            float damageInflicted = 0;
            float damageAngle = Vector3.Angle(projectile.transform.forward, transform.forward);
            SendMessage("OnAttacked", new OnAttackedData(projectile.inflicter.name, damageAngle));

            if (projectile.inflicter.TryGetComponent(out Attributes inflicterAttributes))
            {
                if (inflicterAttributes.team == team) { return false; }
            }

            if (blocking)
            {
                float[] array = new float[3] { 0, 90, 180 };
                float nearest = array.OrderBy(x => Mathf.Abs((long)x - damageAngle)).First();
                if (nearest != 180)
                {
                    HP.Value -= projectile.damage;
                    damageInflicted = projectile.damage;
                }
            }
            else
            {
                HP.Value -= projectile.damage;
                damageInflicted = projectile.damage;
            }

            if (HP.Value < 0)
            {
                HP.Value = 0;
                if (alreadyDead)
                    damageInflicted = 0;
            }

            if (HP.Value <= 0)
                SendMessage("OnDeath");

            if (projectile.inflicter.IsPlayerObject)
            {
                if (damageInflicted > 0)
                {
                    ClientManager.Singleton.AddDamage(projectile.inflicter.OwnerClientId, damageInflicted);
                    if (HP.Value == 0)
                    {
                        ClientManager.Singleton.AddDeaths(OwnerClientId, 1);
                        ClientManager.Singleton.AddKills(projectile.inflicter.OwnerClientId, 1);
                    }
                }
            }

            return true;
        }

        private void OnHPChanged(float previous, float current)
        {
            // If we are a NPC, we edit the renderer's material instance
            if (healthRenderer != null)
            {
                healthRenderer.material.SetFloat("healthPercentage", current / maxHealth);
                healthPointsWorldText.SetText(current + " / " + maxHealth);
            }
            else // If we are the player, we have to edit the material directly (limitation of unity's canvas renderer)
            {
                imageMaterial.SetFloat("healthPercentage", current / maxHealth);
                healthPointsUIText.SetText(current + " / " + maxHealth);
            }

            AudioManager.Singleton.PlayClipAtPoint(damageTakenSound, transform.position, 1);
            if (previous / maxHealth > 0.2f & current / maxHealth <= 0.2f)
                AudioManager.Singleton.PlayClipAtPoint(lowHealthSound, transform.position, 1);
        }

        private void OnAttacked(OnAttackedData data) { }
        private void OnDeath() { }
    }

    public struct OnAttackedData : INetworkSerializable
    {
        public string inflicterName;
        public float damageAngle;

        public OnAttackedData(string inflicterName, float damageAngle)
        {
            this.inflicterName = inflicterName;
            this.damageAngle = damageAngle;
        }

        void INetworkSerializable.NetworkSerialize<T>(BufferSerializer<T> serializer)
        {
            serializer.SerializeValue(ref inflicterName);
            serializer.SerializeValue(ref damageAngle);
        }
    }
}
