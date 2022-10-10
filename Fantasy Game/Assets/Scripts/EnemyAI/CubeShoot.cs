using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

namespace LightPat.EnemyAI
{
    public class CubeShoot : Enemy
    {
        public Transform projectileSpawn;
        public GameObject projectile;
        public float shootDelay;
        public float baseDamage;
        public float projectileForce;
        bool allowAttack;
        public float scaleBulletSize = 1;

        float lastTime;

        private void Start()
        {
            lastTime = Time.time;
            allowAttack = true;
        }

        public void Update()
        {
            if (allowAttack)
            {
                GameObject g = Instantiate(projectile, projectileSpawn.position, projectileSpawn.rotation);
                g.transform.localScale = g.transform.localScale * scaleBulletSize;
                g.GetComponent<Projectile>().inflicter = gameObject;
                g.GetComponent<Projectile>().damage = baseDamage;
                g.GetComponent<Rigidbody>().AddForce(transform.forward * projectileForce, ForceMode.VelocityChange);

                allowAttack = false;
            }

            if (Time.time - lastTime > shootDelay)
            {
                lastTime = Time.time;
                allowAttack = true;
            }
        }
    }
}