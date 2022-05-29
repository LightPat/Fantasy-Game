using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public abstract class Enemy : MonoBehaviour
    {
        [Header("Attack Settings")]
        public float attackDamage = 10f;
        public float attackReach = 4f;

        public void Attack()
        {
            RaycastHit hit;
            bool bHit = Physics.Raycast(transform.position, transform.forward, out hit, attackReach);

            if (bHit)
            {
                if (hit.transform.GetComponent<Attributes>())
                {
                    hit.transform.GetComponent<Attributes>().InflictDamage(attackDamage);
                }
            }
        }
    }
}
