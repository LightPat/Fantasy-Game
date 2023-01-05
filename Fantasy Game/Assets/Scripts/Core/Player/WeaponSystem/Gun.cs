using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.ProceduralAnimations;
using Unity.Netcode;

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
        public GameObject shell;
        public Vector3 shellForce;
        public Vector3 shellTorque;
        public float maxRange;
        public float fireRate;
        public float sumTimeOfFireAnimationClips = 1;
        public ParticleSystem muzzleFlash;
        public Transform smokeSpawnPoint;
        public GameObject smokePrefab;
        [Header("Recoil Settings")]
        public bool disableRecoil;
        public AnimationCurve xRecoilCurve;
        public AnimationCurve yRecoilCurve;
        [Header("Reload/Ammo Settings")]
        public int magazineSize;
        public int currentBullets;
        public GameObject magazineObject;
        public Vector3 magazineInHandOffsetPos;
        public Vector3 magazineInHandOffsetRot;
        public float reloadSpeed = 1;
        [Header("Audio Clips")]
        public AudioClip gunshotClip;
        public float gunshotVolume = 1;

        bool reloading;
        float timeSinceLastShot;
        float lastShotTime;
        bool firing;
        Animator gunAnimator;
        PlayerController playerController;
        HumanoidWeaponAnimationHandler playerWeaponAnimationHandler;
        RootMotionManager playerRootMotionManager;
        AudioSource gunshotSource;
        float minTimeBetweenShots;

        public override NetworkObject Attack1(bool pressed)
        {
            firing = pressed;

            if (firing)
                return Shoot();
            return null;
        }

        private void OnTransformParentChanged()
        {
            if (GetComponentInParent<HumanoidWeaponAnimationHandler>())
            {
                playerWeaponAnimationHandler = GetComponentInParent<HumanoidWeaponAnimationHandler>();
                playerController = playerWeaponAnimationHandler.GetComponent<PlayerController>();
                playerRootMotionManager = playerWeaponAnimationHandler.GetComponentInChildren<RootMotionManager>();
            }
        }

        private NetworkObject Shoot()
        {
            if (reloading) { return null; }
            float time = NetworkManager.Singleton.LocalTime.TimeAsFloat;
            timeSinceLastShot = time - lastShotTime;
            if (timeSinceLastShot < minTimeBetweenShots) { return null; }
            if (currentBullets < 1) { return null; }
            if (disableAttack) { return null; }
            lastShotTime = time;

            // Play 2 animation clips that are x seconds long combined, within the time that a next shot can be fired
            gunAnimator.SetFloat("fireSpeed", sumTimeOfFireAnimationClips / minTimeBetweenShots + 0.2f);
            if (!fullAuto)
                StartCoroutine(Utilities.ResetAnimatorBoolAfter1Frame(gunAnimator, "fire"));

            // Display muzzle flash
            muzzleFlash.Play();
            GameObject smoke = Instantiate(smokePrefab, smokeSpawnPoint);
            StartCoroutine(DestroyAfterParticleSystemStops(smoke.GetComponent<ParticleSystem>()));

            // Play gunshot sound
            gunshotSource.PlayOneShot(gunshotClip, gunshotVolume);

            // Eject shell from side of gun
            GameObject s = Instantiate(shell, shellSpawnPoint.position, shellSpawnPoint.rotation);
            Rigidbody rb = s.GetComponent<Rigidbody>();
            rb.AddRelativeTorque(shellTorque * Random.Range(1, 2), ForceMode.VelocityChange);
            rb.AddRelativeForce(shellForce * Random.Range(0.7f, 1.3f), ForceMode.VelocityChange);
            Destroy(s, 5);

            if (playerWeaponAnimationHandler.IsOwner)
            {
                // Apply recoil
                if (!disableRecoil)
                    StartCoroutine(Recoil());
            }

            currentBullets -= 1;
            if (playerController)
                playerController.playerHUD.SetAmmoText(currentBullets + " / " + magazineSize);
            if (currentBullets == 0) { StartCoroutine(Reload(NetworkManager.Singleton.IsClient)); }

            // Spawn the bullet
            if (NetworkManager.Singleton.IsServer)
            {
                GameObject b = Instantiate(bullet, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
                Projectile projectile = b.GetComponent<Projectile>();
                projectile.inflicter = playerWeaponAnimationHandler.gameObject;
                projectile.originWeapon = this;
                projectile.damage = baseDamage;
                projectile.hitmarkerTime = minTimeBetweenShots;
                NetworkObject projectileNetObj = b.GetComponent<NetworkObject>();

                // Add force so that the bullet flies through the air
                RaycastHit[] allHits = Physics.RaycastAll(playerWeaponAnimationHandler.mainCamera.position, playerWeaponAnimationHandler.mainCamera.forward, maxRange);
                System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
                bool bHit = false;
                foreach (RaycastHit hit in allHits)
                {
                    if (hit.transform == playerWeaponAnimationHandler.transform) { continue; }
                    projectile.startForce = (hit.point - b.transform.position).normalized * bulletForce;
                    bHit = true;
                    break;
                }

                if (!bHit)
                    projectile.startForce = playerWeaponAnimationHandler.mainCamera.forward * bulletForce;

                return projectileNetObj;
            }
            else
            {
                return null;
            }
        }

        public override IEnumerator Reload(bool animate)
        {
            if (currentBullets >= magazineSize) { yield break; }
            if (reloading) { yield break; }

            if (!animate)
            {
                currentBullets = magazineSize;
                if (playerController)
                    playerController.playerHUD.SetAmmoText(currentBullets + " / " + magazineSize);
                yield break;
            }

            reloading = true;
            gunAnimator.SetFloat("reloadSpeed", reloadSpeed);
            gunAnimator.SetBool("fire", false);

            // Store magazine's localPosition and localRotation for later
            Vector3 localPos = magazineObject.transform.localPosition;
            Quaternion localRot = magazineObject.transform.localRotation;
            GameObject newMagazine = Instantiate(magazineObject, playerWeaponAnimationHandler.leftHandTarget.transform);
            newMagazine.SetActive(false);

            gunAnimator.SetBool("reload", true);
            yield return new WaitUntil(() => gunAnimator.GetCurrentAnimatorStateInfo(1).IsName("Pre Reload"));
            gunAnimator.SetBool("reload", false);
            yield return new WaitUntil(() => gunAnimator.IsInTransition(1));

            // Unload current magazine
            Transform oldMagParent = magazineObject.transform.parent;
            GameObject oldMagazine = magazineObject;
            oldMagazine.transform.SetParent(null, true);
            foreach (Collider c in oldMagazine.GetComponents<Collider>())
            {
                c.enabled = false;
            }
            Rigidbody rb = oldMagazine.AddComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Move left hand to the new magazine's position
            playerWeaponAnimationHandler.leftFingerRig.weightTarget = 0;
            FollowTarget leftHand = playerWeaponAnimationHandler.leftHandTarget;
            leftHand.lerpSpeed = reloadSpeed * 2;
            leftHand.lerp = true;
            leftHand.target = playerWeaponAnimationHandler.leftHipStow.Find("MagazinePoint");
            playerRootMotionManager.disableLeftHand = true;
            yield return new WaitUntil(() => Vector3.Distance(playerWeaponAnimationHandler.leftHandTarget.transform.position, playerWeaponAnimationHandler.leftHipStow.Find("MagazinePoint").position) < 0.1f);

            foreach (Collider c in oldMagazine.GetComponents<Collider>())
            {
                c.enabled = true;
            }

            // Spawn new magazine and move hand back to gun
            newMagazine.SetActive(true);
            newMagazine.transform.localPosition = magazineInHandOffsetPos;
            newMagazine.transform.localEulerAngles = magazineInHandOffsetRot;
            leftHand.target = leftHandGrip;
            yield return new WaitUntil(() => Vector3.Distance(playerWeaponAnimationHandler.leftHandTarget.transform.position, leftHandGrip.position) < 0.1f);

            // Load new magazine into gun
            Vector3 scale = newMagazine.transform.localScale;
            playerWeaponAnimationHandler.leftFingerRig.weightTarget = 1;
            newMagazine.transform.SetParent(oldMagParent, true);
            newMagazine.transform.localScale = scale;
            newMagazine.transform.localPosition = localPos;
            newMagazine.transform.localRotation = localRot;

            // Play rest of reload animation
            gunAnimator.SetBool("reload", true);
            yield return new WaitUntil(() => gunAnimator.GetCurrentAnimatorStateInfo(1).IsName("Post Reload"));
            leftHand.lerp = false;
            playerRootMotionManager.disableLeftHand = false;
            gunAnimator.SetBool("reload", false);

            yield return new WaitUntil(() => gunAnimator.GetCurrentAnimatorStateInfo(1).IsName("Empty"));

            magazineObject = newMagazine;
            currentBullets = magazineSize;
            if (playerController)
                playerController.playerHUD.SetAmmoText(currentBullets + " / " + magazineSize);
            reloading = false;

            if (fullAuto)
                gunAnimator.SetBool("fire", firing);

            Destroy(oldMagazine, 3);
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
                if (playerController)
                    playerController.Look(new Vector2(yRecoilCurve.Evaluate(curveTime), xRecoilCurve.Evaluate(curveTime)), 1, Time.timeScale, true);
                curveTime += 0.1f;
                yield return null;
            }
        }

        private IEnumerator DestroyAfterParticleSystemStops(ParticleSystem particleSystem)
        {
            yield return new WaitUntil(() => !particleSystem.isPlaying);
            Destroy(particleSystem.gameObject);
        }

        protected new void Start()
        {
            base.Start();
            lastShotTime = Time.time;
            currentBullets = magazineSize;
            gunAnimator = GetComponent<Animator>();
            gunshotSource = projectileSpawnPoint.GetComponent<AudioSource>();
            minTimeBetweenShots = 1 / (fireRate / 60);
        }

        private new void Update()
        {
            base.Update();
            if (!playerWeaponAnimationHandler) { return; }
            if (fullAuto)
            {
                playerWeaponAnimationHandler.Attack1(firing);
                if (!reloading)
                    gunAnimator.SetBool("fire", firing);
            }
        }
    }
}

