using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Util;
using TMPro;

namespace LightPat.Core.Player
{
    public class Gun : Weapon
    {
        [Header("Gun Specific")]
        public Transform rightFingersGrips;
        public Transform leftFingersGrips;
        [Header("Firing Settings")]
        public bool fullAuto;
        public Transform projectileSpawnPoint;
        public GameObject bullet;
        public float bulletForce;
        public Transform shellSpawnPoint;
        public Transform shell;
        public Vector3 shellForce;
        public Vector3 shellTorque;
        public float maxRange;
        public float fireRate;
        public float sumTimeOfFireAnimationClips = 1;
        public ParticleSystem muzzleFlash;
        [Header("Recoil Settings")]
        public AnimationCurve xRecoilCurve;
        public AnimationCurve yRecoilCurve;
        [Header("Reload/Ammo Settings")]
        public int magazineSize;
        public int currentBullets;
        public GameObject magazineObject;
        public Vector3 magazineInHandOffsetPos;
        public Vector3 magazineInHandOffsetRot;
        public float reloadSpeed = 1;

        bool reloading;
        float timeSinceLastShot;
        float lastShotTime;
        bool firing;
        Animator animator;

        public override void Attack1(bool pressed)
        {
            firing = pressed;

            if (firing)
                Shoot();
        }

        private void Shoot()
        {
            if (reloading) { return; }
            float time = Time.time;
            timeSinceLastShot = time - lastShotTime;
            float minTimeBetweenShots = 1 / (fireRate / 60);
            if (timeSinceLastShot < minTimeBetweenShots) { return; }
            if (currentBullets < 1) { return; }
            if (disableAttack) { return; }
            lastShotTime = time;

            // Play 2 clips that are x seconds long combined, within the time that a next shot can be fired
            animator.SetFloat("fireSpeed", sumTimeOfFireAnimationClips / minTimeBetweenShots + 0.2f);
            if (!fullAuto)
                StartCoroutine(Utilities.ResetAnimatorBoolAfter1Frame(animator, "fire"));

            muzzleFlash.Play();

            // Spawn the bullet
            GameObject g = Instantiate(bullet, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
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
            GetComponentInParent<PlayerController>().playerHUD.SetAmmoText(currentBullets + " / " + magazineSize);
            if (currentBullets == 0) { StartCoroutine(Reload()); }
        }

        public override IEnumerator Reload()
        {
            if (currentBullets >= magazineSize) { yield break; }
            if (reloading) { yield break; }
            reloading = true;
            animator.SetBool("fire", false);

            HumanoidWeaponAnimationHandler weaponAnimationHandler = GetComponentInParent<HumanoidWeaponAnimationHandler>();

            // Store magazine's localPosition and localRotation for later
            Vector3 localPos = magazineObject.transform.localPosition;
            Quaternion localRot = magazineObject.transform.localRotation;
            GameObject newMagazine = Instantiate(magazineObject, weaponAnimationHandler.leftHandTarget.transform);
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
            leftHand.target = weaponAnimationHandler.leftHipStow.Find("MagazinePoint");
            yield return new WaitUntil(() => Vector3.Distance(weaponAnimationHandler.leftHandTarget.transform.position, weaponAnimationHandler.leftHipStow.Find("MagazinePoint").position) < 0.1f);

            foreach (Collider c in oldMagazine.GetComponents<Collider>())
            {
                c.enabled = true;
            }

            // Spawn new magazine and move hand back to gun
            newMagazine.SetActive(true);
            newMagazine.transform.localPosition = magazineInHandOffsetPos;
            newMagazine.transform.localEulerAngles = magazineInHandOffsetRot;
            leftHand.target = leftHandGrip;
            yield return new WaitUntil(() => Vector3.Distance(weaponAnimationHandler.leftHandTarget.transform.position, leftHandGrip.position) < 0.1f);

            // Load new magazine into gun
            Vector3 scale = newMagazine.transform.localScale;
            newMagazine.transform.SetParent(transform.GetChild(0), true);
            newMagazine.transform.localScale = scale;
            newMagazine.transform.localPosition = localPos;
            newMagazine.transform.localRotation = localRot;
            magazineObject = newMagazine;
            currentBullets = magazineSize;
            GetComponentInParent<PlayerController>().playerHUD.SetAmmoText(currentBullets + " / " + magazineSize);
            leftHand.lerp = false;
            weaponAnimationHandler.leftFingerRig.weightTarget = 1;
            reloading = false;

            if (fullAuto)
                animator.SetBool("fire", firing);

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

        protected new void Start()
        {
            base.Start();
            lastShotTime = Time.time;
            currentBullets = magazineSize;
            animator = GetComponent<Animator>();
        }

        private new void Update()
        {
            base.Update();
            if (fullAuto)
            {
                if (firing)
                    Shoot();
                if (!reloading)
                    animator.SetBool("fire", firing);
            }
        }
    }
}

