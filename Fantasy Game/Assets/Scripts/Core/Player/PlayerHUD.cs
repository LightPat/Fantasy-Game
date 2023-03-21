using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using Unity.Netcode;
using LightPat.Singleton;

namespace LightPat.Core.Player
{
    public class PlayerHUD : MonoBehaviour
    {
        public Transform weaponSlots;
        public GameObject weaponSlotPrefab;
        public Vector2 slotSpacing;
        public Transform lookAngleDisplay;
        public float lookAngleRotSpeed;
        public Transform ammoDisplay;
        public Transform crosshair;
        public GameObject hitMarker;
        public TextMeshProUGUI fpsCounter;
        public Image projectileWarning;

        WeaponLoadout weaponLoadout;

        public void AddWeaponSlot()
        {
            GameObject slot = Instantiate(weaponSlotPrefab, weaponSlots);
            slot.GetComponent<TextMeshProUGUI>().SetText("Slot " + weaponSlots.childCount.ToString());
            for (int i = 0; i < weaponSlots.childCount; i++)
            {
                weaponSlots.GetChild(i).localPosition = new Vector3(slotSpacing.x, (weaponSlots.childCount-1-i) * slotSpacing.y, 0);
            }

            ammoDisplay.localPosition = new Vector3(slotSpacing.x, weaponSlots.childCount * slotSpacing.y, 0);
        }

        public void RemoveWeaponSlot()
        {
            foreach (Transform child in weaponSlots)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < weaponLoadout.GetWeaponListLength(); i++)
            {
                GameObject slot = Instantiate(weaponSlotPrefab, weaponSlots);
                slot.GetComponent<TextMeshProUGUI>().SetText(weaponLoadout.GetWeapon(i).weaponName);
            }

            for (int i = 0; i < weaponSlots.childCount; i++)
            {
                weaponSlots.GetChild(i).localPosition = new Vector3(slotSpacing.x, (weaponSlots.childCount - 1 - i) * slotSpacing.y, 0);
            }

            ammoDisplay.localPosition = new Vector3(slotSpacing.x, weaponLoadout.GetWeaponListLength() * slotSpacing.y, 0);
        }

        public void UpdateSlotText(int slotIndex)
        {
            if (weaponLoadout.GetWeapon(slotIndex))
                weaponSlots.GetChild(slotIndex).GetComponent<TextMeshProUGUI>().SetText(weaponLoadout.GetWeapon(slotIndex).name);
            else
                weaponSlots.GetChild(slotIndex).GetComponent<TextMeshProUGUI>().SetText("Slot " + (slotIndex+1).ToString());
        }

        public void ChangeSlotStyle(int slotIndex, FontStyles fontStyle)
        {
            if (slotIndex == -1) { return; }
            weaponSlots.GetChild(slotIndex).GetComponent<TextMeshProUGUI>().fontStyle = fontStyle;
        }

        public void SetAmmoText(string newText)
        {
            ammoDisplay.GetComponent<TextMeshProUGUI>().SetText(newText);
        }

        public IEnumerator ToggleHitMarker(float markerTime)
        {
            if (hitMarker.activeInHierarchy)
            {
                hitMarker.SetActive(false);
                yield return new WaitForSeconds(markerTime/2);
                hitMarker.SetActive(true);
                yield return new WaitForSeconds(markerTime / 2);
                hitMarker.SetActive(false);
            }
            else
            {
                hitMarker.SetActive(true);
                yield return new WaitForSeconds(markerTime);
                hitMarker.SetActive(false);
            }
        }

        private void Start()
        {
            weaponLoadout = GetComponentInParent<WeaponLoadout>();
            hitMarker.SetActive(false);
        }

        List<Projectile> nearbyProjectiles = new List<Projectile>();
        float projectileWarningTargetAlpha;
        float currentAlpha;
        private void Update()
        {
            fpsCounter.SetText(Mathf.RoundToInt((float)1.0 / Time.deltaTime).ToString());

            // Enable/disable crosshair
            if (weaponLoadout.equippedWeapon)
            {
                if (weaponLoadout.equippedWeapon.TryGetComponent(out Gun gun))
                    crosshair.gameObject.SetActive(!gun.aimDownSights);
                else
                    crosshair.gameObject.SetActive(true);
            }
            else
            {
                crosshair.gameObject.SetActive(true);
            }

            nearbyProjectiles.RemoveAll(item => item == null);
            List<Projectile> projectilesToRemove = new List<Projectile>();
            List<float> distances = new List<float>();
            foreach (Projectile projectile in nearbyProjectiles)
            {
                RaycastHit[] allHits = Physics.SphereCastAll(projectile.transform.position, 1, projectile.transform.forward, 1, -1, QueryTriggerInteraction.Ignore);
                System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

                int hitCount = 0;
                foreach (RaycastHit hit in allHits)
                {
                    // If this collider does not belong to the player
                    if (!GetComponentInParent<NetworkObject>().GetComponentsInChildren<Collider>().Contains(hit.collider)) { continue; }

                    distances.Add(Vector3.Distance(hit.collider.transform.position, projectile.transform.position));
                    hitCount += 1;
                }
                if (hitCount == 0) { projectilesToRemove.Add(projectile); }
            }
            nearbyProjectiles.RemoveAll(item => projectilesToRemove.Contains(item));

            if (distances.Count > 0)
            {
                // range of 0, 2
                // take percentage between the 2 values
                // so if distance is 0, 0%, if it is 1 50%, if it is 2 100%
                // then take that percentage of 255 and assign the alpha
                projectileWarningTargetAlpha = Mathf.Clamp(distances.Max(), 0, 1);
            }
            else
            {
                projectileWarningTargetAlpha = 0;
            }

            currentAlpha = Mathf.Lerp(currentAlpha, projectileWarningTargetAlpha, Time.deltaTime * 10);
            projectileWarning.color = new Color(0, 0, 0, Mathf.Clamp(currentAlpha, 0, 0.5f));
        }

        public AudioClip openSound;
        public AudioClip closeSound;
        private void OnEnable()
        {
            //AudioManager.Singleton.PlayClipAtPoint(closeSound, weaponLoadout.transform.position);
            if (GetComponentInParent<NetworkObject>().IsSpawned)
                AudioManager.Singleton.Play2DClip(closeSound);
        }

        private void OnDisable()
        {
            //AudioManager.Singleton.PlayClipAtPoint(openSound, weaponLoadout.transform.position);
            if (GetComponentInParent<NetworkObject>().IsSpawned)
                AudioManager.Singleton.Play2DClip(openSound);
        }

        public void OnProjectileNear(Projectile projectile)
        {
            nearbyProjectiles.Add(projectile);
        }
    }
}
