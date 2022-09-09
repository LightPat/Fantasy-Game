using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core.Player
{
    public class WeaponManager : MonoBehaviour
    {
        public Weapon equippedWeapon;
        public List<Weapon> weapons;

        PlayerController playerController;

        public void DrawWeapon(int slot)
        {
            equippedWeapon = weapons[slot];
            playerController.rotateBodyWithCamera = true;
        }

        public void StowWeapon()
        {
            equippedWeapon = null;
            playerController.rotateBodyWithCamera = false;
        }

        public Weapon GetWeapon(int slot)
        {
            if (slot >= weapons.Count) { return null; }

            return weapons[slot];
        }

        public void AddWeapon(Weapon weapon)
        {
            weapons.Add(weapon);
        }

        public int GetEquippedWeaponIndex()
        {
            return weapons.FindIndex(equippedWeapon => weapons.Contains(equippedWeapon));
        }

        private void Start()
        {
            playerController = GetComponent<PlayerController>();
        }
    }
}