using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

namespace LightPat.Core
{
    public class Attributes : MonoBehaviour
    {
        public bool invincible;
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

        public float HP { get; private set; }
        Animator animator;

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            HP = maxHealth;
            UpdateHPDisplay();
        }

        public void InflictDamage(float damage, GameObject inflicter)
        {
            if (invincible) { return; }

            float damageAngle = Vector3.Angle(inflicter.transform.forward, transform.forward);

            if (blocking)
            {
                float[] array = new float[3] { 0, 90, 180 };
                float nearest = array.OrderBy(x => Mathf.Abs((long)x - damageAngle)).First();
                if (nearest != 180)
                    HP -= damage;
            }
            else
            {
                HP -= damage;
            }

            if (HP < 0)
                HP = 0;

            SendMessage("OnAttacked", inflicter);

            if (animator != null)
            {
                animator.SetFloat("damageAngle", damageAngle);
                StartCoroutine(Utilities.ResetAnimatorBoolAfter1Frame(animator, "reactDamage"));

                if (HP <= 0)
                {
                    animator.SetBool("dead", true);
                    SendMessage("OnDeath");
                }
            }
            else
            {
                if (HP <= 0) { gameObject.SetActive(false); }
            }

            UpdateHPDisplay();
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
                    HP -= damage;
            }
            else
            {
                HP -= damage;
            }

            if (HP < 0)
                HP = 0;

            SendMessage("OnAttacked", inflicter);

            if (animator != null)
            {
                animator.SetFloat("damageAngle", damageAngle);
                StartCoroutine(Utilities.ResetAnimatorBoolAfter1Frame(animator, "reactDamage"));

                if (HP <= 0)
                {
                    animator.SetBool("dead", true);
                    SendMessage("OnDeath");
                }
            }
            else
            {
                if (HP <= 0) { gameObject.SetActive(false); }
            }

            UpdateHPDisplay();
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
    }
}
