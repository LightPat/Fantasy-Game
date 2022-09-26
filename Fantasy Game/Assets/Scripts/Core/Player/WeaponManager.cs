using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core.Player
{
    public class WeaponManager : MonoBehaviour
    {
        public Weapon equippedWeapon;
        
        List<Weapon> weapons = new List<Weapon>();
        PlayerController playerController;
        PlayerHUD playerHUD;

        public void DrawWeapon(int slot)
        {
            playerHUD.ChangeSlotStyle(slot, TMPro.FontStyles.Bold);
            equippedWeapon = weapons[slot];
            playerController.rotateBodyWithCamera = true;
            if (equippedWeapon.GetComponent<GreatSword>())
            {
                playerController.lookAngleUI.gameObject.SetActive(true);
            }
        }

        public void StowWeapon()
        {
            playerHUD.ChangeSlotStyle(GetEquippedWeaponIndex(), TMPro.FontStyles.Normal);
            if (equippedWeapon.GetComponent<GreatSword>())
            {
                playerController.lookAngleUI.gameObject.SetActive(false);
            }
            equippedWeapon = null;
            playerController.rotateBodyWithCamera = false;
        }

        public Weapon GetWeapon(int slot)
        {
            if (slot >= weapons.Count) { return null; }

            return weapons[slot];
        }

        public int AddWeapon(Weapon weapon)
        {
            weapons.Add(weapon);
            int slot = weapons.Count - 1;
            playerHUD.UpdateSlotText(slot);
            return slot;
        }

        private void Start()
        {
            playerController = GetComponent<PlayerController>();
            playerHUD = GetComponentInChildren<PlayerHUD>();
        }
        private int GetEquippedWeaponIndex()
        {
            int counter = 0;
            foreach (Weapon weapon in weapons)
            {
                if (weapon == equippedWeapon)
                    return counter;
                counter++;
            }

            return -1;
        }
    }
}