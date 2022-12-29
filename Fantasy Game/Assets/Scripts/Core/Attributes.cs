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
        Animator animator;
        bool invincible;

        public override void OnNetworkSpawn()
        {
            HP.OnValueChanged = UpdateHPDisplay;
            HP.Value = maxHealth;
            team = ClientManager.Singleton.GetClient(OwnerClientId).team;
            StartCoroutine(SpawnProtection());
        }

        private IEnumerator SpawnProtection()
        {
            invincible = true;
            yield return new WaitForSeconds(5);
            invincible = false;
        }

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();

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

        public void InflictDamage(float damage, GameObject inflicter)
        {
            if (invincible) { return; }
            Attributes inflicterAttributes;
            if (inflicter.TryGetComponent(out inflicterAttributes))
            {
                if (inflicterAttributes.team == team) { return; }
            }

            float damageAngle = Vector3.Angle(inflicter.transform.forward, transform.forward);

            if (blocking)
            {
                float[] array = new float[3] { 0, 90, 180 };
                float nearest = array.OrderBy(x => Mathf.Abs((long)x - damageAngle)).First();
                if (nearest != 180)
                    HP.Value -= damage;
            }
            else
            {
                HP.Value -= damage;
            }

            if (HP.Value < 0)
                HP.Value = 0;

            SendMessage("OnAttacked", inflicter);

            if (animator != null)
            {
                animator.SetFloat("damageAngle", damageAngle);
                StartCoroutine(Utilities.ResetAnimatorBoolAfter1Frame(animator, "reactDamage"));

                if (HP.Value <= 0)
                {
                    animator.SetBool("dead", true);
                    SendMessage("OnDeath");
                }
            }
            else
            {
                if (HP.Value <= 0) { gameObject.SetActive(false); }
            }
        }

        public void InflictDamage(float damage, GameObject inflicter, Projectile projectile)
        {
            if (invincible) { return; }

            float damageAngle = Vector3.Angle(projectile.transform.forward, transform.forward);

            if (blocking)
            {
                float[] array = new float[3] { 0, 90, 180 };
                float nearest = array.OrderBy(x => Mathf.Abs((long)x - damageAngle)).First();
                if (nearest != 180)
                    HP.Value -= damage;
            }
            else
            {
                HP.Value -= damage;
            }

            if (HP.Value < 0)
                HP.Value = 0;

            SendMessage("OnAttacked", inflicter);

            if (animator != null)
            {
                animator.SetFloat("damageAngle", damageAngle);
                StartCoroutine(Utilities.ResetAnimatorBoolAfter1Frame(animator, "reactDamage"));

                if (HP.Value <= 0)
                {
                    animator.SetBool("dead", true);
                    SendMessage("OnDeath");
                }
            }
            else
            {
                if (HP.Value <= 0) { gameObject.SetActive(false); }
            }
        }

        private void UpdateHPDisplay(float previous, float current)
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
}
