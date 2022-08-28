using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

public class WeaponManager : MonoBehaviour
{
    public Weapon equippedWeapon;
    public List<Weapon> weapons;

    public void DrawWeapon(int slot)
    {
        equippedWeapon = weapons[slot];
    }

    public void StowWeapon()
    {
        equippedWeapon = null;
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
}
