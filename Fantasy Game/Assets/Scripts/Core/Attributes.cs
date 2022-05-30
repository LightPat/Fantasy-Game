using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace LightPat.Core
{
    public class Attributes : MonoBehaviour
    {
        public float maxHealth = 100f;
        private float HP;
        [SerializeField]
        private Renderer healthRenderer;
        [Header("Only assign for player")]
        [SerializeField]
        private Material imageMaterial;

        private void Start()
        {
            HP = maxHealth;

            UpdateMaterial();
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
    }
}
