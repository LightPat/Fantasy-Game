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

        private void Start()
        {
            weaponLoadout = GetComponentInParent<WeaponLoadout>();

            //for (int i = 0; i < weaponSlots.childCount; i++)
            //{
            //    if (weaponManager.GetWeapon(i) != null)
            //        weaponSlots.GetChild(i).GetComponent<TextMeshProUGUI>().SetText(weaponManager.GetWeapon(i).name);
            //}
        }
    }
}
