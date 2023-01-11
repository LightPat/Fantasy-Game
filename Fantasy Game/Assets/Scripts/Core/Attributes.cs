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
            yield return new WaitForSeconds(3);
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
            if (!IsServer) { return false; }
            if (invincible) { return false; }

            bool alreadyDead = HP.Value <= 0;
            float damageInflicted = 0;
            float damageAngle = Vector3.Angle(inflicter.transform.forward, transform.forward);
            SendMessage("OnAttacked", new OnAttackedData(inflicter.name, damageAngle));

            if (inflicter.TryGetComponent(out Attributes inflicterAttributes))
            {
                if (inflicterAttributes.team == team) { return true; }
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

        public bool InflictDamage(float damage, GameObject inflicter, Projectile projectile)
        {
            if (!IsServer) { return false; }
            if (invincible) { return false; }

            bool alreadyDead = HP.Value <= 0;
            float damageInflicted = 0;
            float damageAngle = Vector3.Angle(projectile.transform.forward, transform.forward);
            SendMessage("OnAttacked", new OnAttackedData(inflicter.name, damageAngle));

            if (inflicter.TryGetComponent(out Attributes inflicterAttributes))
            {
                if (inflicterAttributes.team == team) { return true; }
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

        private void OnHPChanged(float previous, float current)
        {
            // If we are a NPC, we edit the renderer's material instance
            if (healthRenderer != null)
            {
                healthRenderer.material.SetFloat("healthPercentage", HP.Value / maxHealth);
                healthPointsWorldText.SetText(HP.Value + " / " + maxHealth);
            }
            else // If we are the player, we have to edit the material directly (limitation of unity's canvas renderer)
            {
                imageMaterial.SetFloat("healthPercentage", HP.Value / maxHealth);
                healthPointsUIText.SetText(HP.Value + " / " + maxHealth);
            }
        }
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
