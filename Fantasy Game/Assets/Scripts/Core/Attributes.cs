using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LightPat.Core
{
    public class Attributes : MonoBehaviour
    {
        public float maxHealth = 100f;
        public TextMeshProUGUI overlayText;
        public TextMeshPro worldSpaceText;
        private float HP;

        private void Start()
        {
            HP = maxHealth;
            SetDisplayText(HP.ToString() + " HP");
        }

        public void InflictDamage(float damage, GameObject inflicter)
        {
            HP -= damage;
            SetDisplayText(HP.ToString() + " HP");
            SendMessage("OnAttacked", inflicter);
            if (HP <= 0)
            {
                Debug.Log(name + "'s HP has reached " + HP + ", it is now dead.");
            }
        }

        public float GetHP()
        {
            return HP;
        }

        private void SetDisplayText(string newText)
        {
            if (overlayText != null)
            {
                overlayText.SetText(newText);
            }
            if (worldSpaceText != null)
            {
                worldSpaceText.SetText(newText);
            }
        }
    }
}
