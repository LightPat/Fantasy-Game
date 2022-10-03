using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Util;

namespace LightPat.Core.Player
{
    public class Rifle : Weapon
    {
        [Header("Rifle Specific")]
        public Transform rightFingersGrips;
        public Transform leftFingersGrips;
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
        public Vector3 magazineLocalPos;
        public Vector3 magazineLocalRot;
        public float reloadSpeed = 1;

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
            if (currentBullets == 0) { StartCoroutine(Reload()); }
        }

        bool reloading;
        public override IEnumerator Reload()
        {
            if (currentBullets >= magazineSize) { yield break; }
            if (reloading) { yield break; }
            reloading = true;

            WeaponAnimationHandler weaponAnimationHandler = GetComponentInParent<WeaponAnimationHandler>();

            // Store magazine's localPosition and localRotation for later
            Vector3 localPos = magazineObject.transform.localPosition;
            Quaternion localRot = magazineObject.transform.localRotation;
            GameObject newMagazine = Instantiate(magazineObject, weaponAnimationHandler.leftHandTarget);
            newMagazine.SetActive(false);

            // Unload current magazine
            GameObject oldMagazine = magazineObject;
            oldMagazine.transform.SetParent(null, true);
            foreach (Collider c in oldMagazine.GetComponents<Collider>())
            {
                c.enabled = false;
            }
            oldMagazine.AddComponent<Rigidbody>();

            // Move left hand to the new magazine's position
            weaponAnimationHandler.leftFingerRig.weightTarget = 0;
            FollowTarget leftHand = weaponAnimationHandler.leftHandTarget.GetComponent<FollowTarget>();
            leftHand.lerpSpeed = reloadSpeed;
            leftHand.lerp = true;
            leftHand.target = GetComponentInParent<WeaponAnimationHandler>().leftHipStow.Find("MagazinePoint");
            yield return new WaitUntil(() => Vector3.Distance(weaponAnimationHandler.leftHandTarget.position, weaponAnimationHandler.leftHipStow.Find("MagazinePoint").position) < 0.1f);

            foreach (Collider c in oldMagazine.GetComponents<Collider>())
            {
                c.enabled = true;
            }

            // Spawn new magazine and move hand back to gun
            newMagazine.SetActive(true);
            newMagazine.transform.localPosition = magazineLocalPos;
            newMagazine.transform.localEulerAngles = magazineLocalRot;
            leftHand.target = leftHandGrip;
            yield return new WaitUntil(() => Vector3.Distance(weaponAnimationHandler.leftHandTarget.position, leftHandGrip.position) < 0.1f);

            // Load new magazine into gun
            Vector3 scale = newMagazine.transform.localScale;
            newMagazine.transform.SetParent(transform.GetChild(0), true);
            newMagazine.transform.localScale = scale;
            newMagazine.transform.localPosition = localPos;
            newMagazine.transform.localRotation = localRot;
            magazineObject = newMagazine;
            currentBullets = magazineSize;
            leftHand.lerp = false;
            weaponAnimationHandler.leftFingerRig.weightTarget = 1;
            reloading = false;

            yield return new WaitForSeconds(3f);
            Destroy(oldMagazine);
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
                GetComponentInParent<PlayerController>().Look(new Vector2(yRecoilCurve.Evaluate(curveTime), xRecoilCurve.Evaluate(curveTime)));
                curveTime += 0.1f;
                yield return null;
            }
        }

        private new void Start()
        {
            base.Start();
            animationClass = "Rifle";
            lastShotTime = Time.time;
            currentBullets = magazineSize;
        }
    }
}