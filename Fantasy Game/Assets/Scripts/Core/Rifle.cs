using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Rifle : Weapon
    {
        [Header("Rifle Specific")]
        public Transform projectileSpawn;
        public GameObject bullet;
        public float bulletForce;
        public float maxRange;
        public Vector3 ADSPosOffset;

        public override void Attack1()
        {
            GameObject g = Instantiate(bullet, projectileSpawn.position, projectileSpawn.rotation);
            g.GetComponent<Projectile>().inflicter = gameObject;
            g.GetComponent<Projectile>().damage = baseDamage;

            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, maxRange))
            {
                g.GetComponent<Rigidbody>().AddForce((hit.point - g.transform.position).normalized * bulletForce, ForceMode.VelocityChange);
            }
            else
            {
                g.GetComponent<Rigidbody>().AddForce(Camera.main.transform.forward * bulletForce, ForceMode.VelocityChange);
            }
        }

        private new void Start()
        {
            base.Start();
            animationClass = "Rifle";
        }
    }
}