using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LightPat.Core.Player
{
    public class PlayerHUD : MonoBehaviour
    {
        public Transform weaponSlots;

        WeaponManager weaponManager;

        public void UpdateSlotText(int slotIndex)
        {
            weaponSlots.GetChild(slotIndex).GetComponent<TextMeshProUGUI>().SetText(weaponManager.GetWeapon(slotIndex).name);
        }

        public void ChangeSlotStyle(int slotIndex, TMPro.FontStyles fontStyle)
        {
            weaponSlots.GetChild(slotIndex).GetComponent<TextMeshProUGUI>().fontStyle = fontStyle;
        }

        private void Start()
        {
            weaponManager = GetComponentInParent<WeaponManager>();

            //for (int i = 0; i < weaponSlots.childCount; i++)
            //{
            //    if (weaponManager.GetWeapon(i) != null)
            //        weaponSlots.GetChild(i).GetComponent<TextMeshProUGUI>().SetText(weaponManager.GetWeapon(i).name);
            //}
        }
    }
}
