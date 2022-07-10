using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LightPat.Core
{
    public class Attributes : MonoBehaviour
    {
        [Header("Affinity Scores")]
        public sbyte[] personalityValues;
        public sbyte[] physicalValues;
        public sbyte[] magicalValues;
        [Header("Health")]
        public int currentLevel = 1;
        public float maxHealth = 100f;
        private float HP;
        [Header("Only assign for NPC/mobs")]
        public Renderer healthRenderer;
        public TextMeshPro worldSpaceLevelDisplay;
        [Header("Only assign for player/allies/bosses")]
        public Material imageMaterial;
        public TextMeshProUGUI screenSpaceLevelDisplay;

        private void Start()
        {
            HP = maxHealth;
            UpdateMaterial();
            UpdateLevel();
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

            UpdateMaterial();
        }

        private void UpdateMaterial()
        {
            // If we are the player, we have to edit the material directly (limitation of unity's canvas renderer)
            // If we are a NPC, we edit the renderer's material instance
            if (healthRenderer != null)
            {
                healthRenderer.material.SetFloat("healthPercentage", HP / maxHealth);
            }
            else
            {
                imageMaterial.SetFloat("healthPercentage", HP / maxHealth);
            }
        }

        private void UpdateLevel()
        {
            if (worldSpaceLevelDisplay != null)
            {
                worldSpaceLevelDisplay.SetText(currentLevel.ToString());
            }
            else
            {
                screenSpaceLevelDisplay.SetText("Lvl. " + currentLevel.ToString());
            }
        }
    }
}
