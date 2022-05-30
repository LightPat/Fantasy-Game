using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace LightPat.Core
{
    public class Attributes : MonoBehaviour
    {
        public int currentLevel = 1;
        public float maxHealth = 100f;
        private float HP;
        [Header("Only assign for NPC/mobs")]
        [SerializeField]
        private Renderer healthRenderer;
        [SerializeField]
        private TextMeshPro worldSpaceLevelDisplay;
        [Header("Only assign for player/allies/bosses")]
        [SerializeField]
        private Material imageMaterial;
        [SerializeField]
        private TextMeshProUGUI screenSpaceLevelDisplay;

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
            }

            UpdateMaterial();
        }

        public void UpdateLevel(int newLevel)
        {
            currentLevel = newLevel;
            UpdateLevel();
        }

        public float GetHP()
        {
            return HP;
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
