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
        [Header("Health")]
        public float maxHealth = 100f;
        private float HP;
        [Header("World Space Label : Only assign for enemies/NPCs/mobs")]
        public Renderer healthRenderer;
        [Header("ScreenSpaceOverlay : Only assign for player/allies/bosses")]
        public Material imageMaterial;
        public TextMeshProUGUI healthPointsText;

        Animator animator = null;

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            HP = maxHealth;
            UpdateHPDisplay();
        }

        public float damageAnimSpeed;
        private void Update()
        {
            Vector2 interp = Vector2.MoveTowards(new Vector2(animator.GetFloat("damageX"), animator.GetFloat("damageY")), new Vector2(xTarget, yTarget), Time.deltaTime * damageAnimSpeed);
            animator.SetFloat("damageX", interp.x);
            animator.SetFloat("damageY", interp.y);

            animator.SetFloat("damageX", Mathf.Lerp(animator.GetFloat("damageX"), xTarget, Time.deltaTime * damageAnimSpeed));
            animator.SetFloat("damageY", Mathf.Lerp(animator.GetFloat("damageY"), yTarget, Time.deltaTime * damageAnimSpeed));

            animator.SetBool("reactDamage", reactDamage);

            if (animator.GetFloat("damageX") == xTarget) { xTarget = 0; }
            if (animator.GetFloat("damageY") == yTarget) { yTarget = 0; }
            if (reactDamage) { reactDamage = false; }
        }

        bool reactDamage;
        float xTarget;
        float yTarget;
        public void InflictDamage(float damage, GameObject inflicter)
        {
            HP -= damage;
            //SendMessage("OnAttacked", inflicter);

            if (animator != null)
            {
                Vector3 dir = (inflicter.transform.position - transform.position).normalized;

                xTarget = dir.x;
                yTarget = dir.z;
                reactDamage = true;
            }

            if (HP <= 0)
            {
                if (animator != null) { animator.Play("Death"); }
            }

            UpdateHPDisplay();
        }

        private void UpdateHPDisplay()
        {
            // If we are a NPC, we edit the renderer's material instance
            if (healthRenderer != null)
            {
                healthRenderer.material.SetFloat("healthPercentage", HP / maxHealth);
            }
            else // If we are the player, we have to edit the material directly (limitation of unity's canvas renderer)
            {
                imageMaterial.SetFloat("healthPercentage", HP / maxHealth);
                healthPointsText.SetText(HP + " / " + maxHealth);
            }
        }

        void OnAttacked(GameObject attacker)
        {
            Debug.Log(name + " is being attacked by: " + attacker);
        }
    }
}
