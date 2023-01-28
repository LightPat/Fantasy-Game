using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

namespace LightPat.EnemyAI
{
    public class Turret : Enemy
    {
        public Transform projectileSpawn;
        public GameObject projectile;
        public float shootDelay;
        public float baseDamage;
        public float projectileForce;
        public float scaleBulletSize = 1;

        float lastTime;
        bool allowAttack;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                lastTime = Time.time;
                allowAttack = true;
            }
        }

        private void Update()
        {
            if (!IsServer) { return; }
            if (!IsSpawned) { return; }

            if (allowAttack)
            {
                GameObject g = Instantiate(projectile, projectileSpawn.position, projectileSpawn.rotation);
                g.transform.localScale = g.transform.localScale * scaleBulletSize;
                Projectile p = g.GetComponent<Projectile>();
                p.NetworkObject.Spawn(true);
                p.InstantiateProjectile(NetworkObject, null, transform.forward * projectileForce, baseDamage);
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