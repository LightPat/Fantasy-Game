using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core.Player
{
    public class WeaponLoadout : MonoBehaviour
    {
        public Weapon equippedWeapon;
        public List<Weapon> startingWeapons;
        List<Weapon> weapons = new List<Weapon>();
        PlayerHUD playerHUD;

        public void DrawWeapon(int slot)
        {
            if (playerHUD)
                playerHUD.ChangeSlotStyle(slot, TMPro.FontStyles.Bold);
            equippedWeapon = weapons[slot];
        }

        public void StowWeapon()
        {
            if (playerHUD)
                playerHUD.ChangeSlotStyle(GetEquippedWeaponIndex(), TMPro.FontStyles.Normal);
            equippedWeapon = null;
        }

        public Weapon GetWeapon(int slot)
        {
            if (slot >= weapons.Count) { return null; }

            return weapons[slot];
        }

        public int AddWeapon(Weapon weapon)
        {
            for (int i = 0; i < weapons.Count; i++)
            {
                if (weapons[i] == null)
                {
                    weapons[i] = weapon;
                    if (playerHUD)
                        playerHUD.UpdateSlotText(i);
                    return i;
                }
            }

            weapons.Add(weapon);
            int slot = weapons.Count - 1;
            if (playerHUD)
            {
                playerHUD.AddWeaponSlot();
                playerHUD.UpdateSlotText(slot);
            }
            return slot;
        }

        public void RemoveEquippedWeapon()
        {
            int slot = GetEquippedWeaponIndex();
            weapons[slot] = null;
            equippedWeapon = null;
            if (playerHUD)
            {
                playerHUD.UpdateSlotText(slot);
                playerHUD.ChangeSlotStyle(slot, TMPro.FontStyles.Normal);
            }
        }

        public void ChangeLoadoutPositions(int fromIndex, int toIndex)
        {
            Weapon temp = weapons[fromIndex];
            weapons[fromIndex] = weapons[toIndex];
            weapons[toIndex] = temp;

            if (playerHUD)
            {
                playerHUD.UpdateSlotText(fromIndex);
                playerHUD.UpdateSlotText(toIndex);

                if (weapons[toIndex] == equippedWeapon)
                {
                    playerHUD.ChangeSlotStyle(fromIndex, TMPro.FontStyles.Normal);
                    playerHUD.ChangeSlotStyle(toIndex, TMPro.FontStyles.Bold);
                }
                else if (weapons[fromIndex] == equippedWeapon)
                {
                    playerHUD.ChangeSlotStyle(toIndex, TMPro.FontStyles.Normal);
                    playerHUD.ChangeSlotStyle(fromIndex, TMPro.FontStyles.Bold);
                }
            }
        }

        public int GetEquippedWeaponIndex()
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

        public int GetWeaponListLength()
        {
            return weapons.Count;
        }

        private void Start()
        {
            playerHUD = GetComponentInChildren<PlayerHUD>();
        }
    }
}