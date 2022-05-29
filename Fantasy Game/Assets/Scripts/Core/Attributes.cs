using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Attributes : MonoBehaviour
    {
        public float maxHealth = 100f;
        private float HP;

        private void Start()
        {
            HP = maxHealth;
        }

        public void InflictDamage(float damage, GameObject inflicter)
        {
            HP -= damage;
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
    }
}
