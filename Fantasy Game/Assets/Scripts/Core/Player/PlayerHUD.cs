using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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
            Destroy(weaponSlots.GetChild(0).gameObject);
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

        private void Update()
        {
            fpsCounter.SetText(Mathf.RoundToInt((float)1.0 / Time.deltaTime).ToString());
        }
    }
}
