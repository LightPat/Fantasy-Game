using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    //[RequireComponent(typeof(Attributes))]
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

            // If we don't have a target check a raycast
            RaycastHit[] allHits = Physics.RaycastAll(transform.position, transform.forward, attackReach);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform.gameObject == gameObject)
                {
                    continue;
                }

                if (hit.transform.GetComponent<Attributes>())
                {
                    hit.transform.GetComponent<Attributes>().InflictDamage(attackDamage, gameObject);
                    StartCoroutine(AttackCooldown());
                }
                break;
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
