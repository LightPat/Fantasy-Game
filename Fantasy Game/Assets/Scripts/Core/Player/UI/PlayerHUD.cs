using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LightPat.Core.Player
{
    public class PlayerHUD : MonoBehaviour
    {
        public Transform weaponSlots;
        public Transform lookAngleDisplay;
        public float lookAngleRotSpeed;
        public Transform ammoDisplay;
        public Transform crosshair;
        public GameObject hitMarker;
        public TextMeshProUGUI fpsCounter;

        WeaponLoadout weaponLoadout;

        public void UpdateSlotText(int slotIndex)
        {
            if (weaponLoadout.GetWeapon(slotIndex))
                weaponSlots.GetChild(slotIndex).GetComponent<TextMeshProUGUI>().SetText(weaponLoadout.GetWeapon(slotIndex).name);
            else
                weaponSlots.GetChild(slotIndex).GetComponent<TextMeshProUGUI>().SetText("Slot " + (slotIndex+1).ToString());
        }

        public void ChangeSlotStyle(int slotIndex, FontStyles fontStyle)
        {
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
            //for (int i = 0; i < weaponSlots.childCount; i++)
            //{
            //    if (weaponManager.GetWeapon(i) != null)
            //        weaponSlots.GetChild(i).GetComponent<TextMeshProUGUI>().SetText(weaponManager.GetWeapon(i).name);
            //}
        }

        private void Update()
        {
            fpsCounter.SetText(Mathf.RoundToInt((float)1.0 / Time.deltaTime).ToString());
        }
    }
}
