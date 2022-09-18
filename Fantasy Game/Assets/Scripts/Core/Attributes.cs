using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LightPat.Core
{
    public class Attributes : MonoBehaviour
    {
        //[Header("Affinity Scores")]
        [HideInInspector] public sbyte[] personalityValues;
        [HideInInspector] public sbyte[] physicalValues;
        [HideInInspector] public sbyte[] magicalValues;
        public bool blocking;
        [Header("Health")]
        public float maxHealth = 100f;
        private float HP;
        [Header("World Space Label : Only assign for enemies/NPCs/mobs")]
        public Renderer healthRenderer;
        public TextMeshPro healthPointsWorldText;
        [Header("ScreenSpaceOverlay : Only assign for player/allies/bosses")]
        public Material imageMaterial;
        public TextMeshProUGUI healthPointsUIText;

        Animator animator = null;

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            HP = maxHealth;
            UpdateHPDisplay();
        }

        public void InflictDamage(float damage, GameObject inflicter)
        {
            if (!blocking)
                HP -= damage;

            SendMessage("OnAttacked", inflicter);

            if (animator != null)
            {
                Vector3 dir = (inflicter.transform.position - transform.position).normalized;
                animator.SetFloat("damageAngle", Vector2.SignedAngle(new Vector2(transform.forward.x, transform.forward.z), new Vector2(dir.x, dir.z)));
                animator.SetBool("reactDamage", true);
                StartCoroutine(ResetReactDamageBool());
            }

            if (HP <= 0)
            {
                if (animator != null) { animator.SetBool("dead", true); }
            }

            UpdateHPDisplay();
        }

        public void InflictDamage(float damage, GameObject inflicter, GameObject projectile)
        {
            if (!blocking)
                HP -= damage;

            SendMessage("OnAttacked", inflicter);

            if (animator != null)
            {
                Vector3 dir = (projectile.transform.position - transform.position).normalized;
                animator.SetFloat("damageAngle", Vector2.SignedAngle(new Vector2(transform.forward.x, transform.forward.z), new Vector2(dir.x, dir.z)));
                animator.SetBool("reactDamage", true);
                StartCoroutine(ResetReactDamageBool());
            }

            if (HP <= 0)
            {
                if (animator != null) { animator.SetBool("dead", true); }
            }

            UpdateHPDisplay();
        }

        private IEnumerator ResetReactDamageBool()
        {
            yield return null;
            animator.SetBool("reactDamage", false);
        }

        private void UpdateHPDisplay()
        {
            // If we are a NPC, we edit the renderer's material instance
            if (healthRenderer != null)
            {
                healthRenderer.material.SetFloat("healthPercentage", HP / maxHealth);
                healthPointsWorldText.SetText(HP + " / " + maxHealth);
            }
            else // If we are the player, we have to edit the material directly (limitation of unity's canvas renderer)
            {
                imageMaterial.SetFloat("healthPercentage", HP / maxHealth);
                healthPointsUIText.SetText(HP + " / " + maxHealth);
            }
        }

        void OnAttacked(GameObject attacker)
        {
            //Debug.Log(name + " is being attacked by: " + attacker);
        }
    }
}
