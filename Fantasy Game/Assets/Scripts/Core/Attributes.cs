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
        [Header("Only assign for NPC/mobs")]
        public Renderer healthRenderer;
        [Header("Only assign for player/allies/bosses")]
        public Material imageMaterial;
        public TextMeshProUGUI healthPointsText;

        private void Start()
        {
            HP = maxHealth;
            UpdateHPDisplay();
        }

        public void InflictDamage(float damage, GameObject inflicter)
        {
            HP -= damage;
            SendMessage("OnAttacked", inflicter);
            if (HP <= 0)
            {
                Debug.Log(name + "'s HP has reached " + HP + ", it is now dead.");
                GetComponent<Animator>().Play("Death");
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
    }
}
