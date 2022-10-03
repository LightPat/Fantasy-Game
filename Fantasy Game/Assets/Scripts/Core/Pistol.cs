using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Pistol : Weapon
    {
        [Header("Pistol Specific")]
        public Transform rightFingersGrips;
        public Transform leftFingersGrips;
        public float forwardMult;
        public float rightMult;
        public float upMult;
        public Transform projectileSpawn;
        public GameObject bullet;
        public float bulletForce;
        public float maxRange;
        public Vector3 ADSPosOffset;
        public float fireRate;
        public AnimationCurve xRecoilCurve;
        public AnimationCurve yRecoilCurve;
        public int magazineSize;
        public int currentBullets;
        public GameObject magazineObject;

        float timeSinceLastShot;
        float lastShotTime;

        public override void Attack1()
        {
            float time = Time.time;
            timeSinceLastShot = time - lastShotTime;
            if (timeSinceLastShot < 1 / (fireRate / 60)) { return; }
            if (currentBullets < 1) { return; }
            if (reloading) { return; }
            lastShotTime = time;

            // Spawn the bullet
            GameObject g = Instantiate(bullet, projectileSpawn.position, projectileSpawn.rotation);
            g.GetComponent<Projectile>().inflicter = gameObject;
            g.GetComponent<Projectile>().damage = baseDamage;

            // Add force so that the bullet flies through the air
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, maxRange))
            {
                g.GetComponent<Rigidbody>().AddForce((hit.point - g.transform.position).normalized * bulletForce, ForceMode.VelocityChange);
            }
            else
            {
                g.GetComponent<Rigidbody>().AddForce(Camera.main.transform.forward * bulletForce, ForceMode.VelocityChange);
            }

            // Apply recoil
            StartCoroutine(Recoil());

            currentBullets -= 1;
            if (currentBullets == 0) { Reload(); }
        }

        bool reloading;
        public void Reload()
        {
            reloading = true;
            if (currentBullets < magazineSize)
            {
                currentBullets = magazineSize;
            }
            reloading = false;
        }

        private IEnumerator Recoil()
        {
            // Set curveLength to the longer curve
            float curveLength = yRecoilCurve.keys[yRecoilCurve.length - 1].time;
            if (xRecoilCurve.keys[xRecoilCurve.length - 1].time > yRecoilCurve.keys[yRecoilCurve.length - 1].time)
            {
                curveLength = xRecoilCurve.keys[xRecoilCurve.length - 1].time;
            }

            float curveTime = 0;
            while (curveTime < curveLength)
            {
                Camera.main.transform.Rotate(xRecoilCurve.Evaluate(curveTime), yRecoilCurve.Evaluate(curveTime), 0, Space.Self);
                curveTime += 0.1f;
                yield return null;
            }
        }

        private new void Start()
        {
            base.Start();
            animationClass = "Pistol";
            lastShotTime = Time.time;
            currentBullets = magazineSize;
        }
    }
}
