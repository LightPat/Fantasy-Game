using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    [RequireComponent(typeof(Attributes))]
    public abstract class Enemy : MonoBehaviour
    {
        [Header("Attack Settings")]
        public float attackDamage = 10f;
        public float attackReach = 4f;
        public float attackCooldown = 1.5f;
        protected bool allowAttack = true;

        public void Attack()
        {
            if (!allowAttack) { return; }

            RaycastHit hit;
            bool bHit = Physics.Raycast(transform.position + (transform.forward * 0.1f), transform.forward, out hit, attackReach);

            if (bHit)
            {
                if (hit.transform.GetComponent<Attributes>())
                {
                    hit.transform.GetComponent<Attributes>().InflictDamage(attackDamage, gameObject);
                    StartCoroutine(AttackCooldown());
                }
            }
        }

        protected IEnumerator AttackCooldown()
        {
            allowAttack = false;
            yield return new WaitForSeconds(attackCooldown);
            allowAttack = true;
        }
    }
}
