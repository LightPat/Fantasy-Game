using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

public class WeaponManager : MonoBehaviour
{
    public GameObject equippedWeapon { get; private set; }
    public List<Weapon> weapons;

    public void DrawWeapon(int slot)
    {
        equippedWeapon = weapons[slot].gameObject;
    }

    public void StowWeapon()
    {
        equippedWeapon = null;
    }

    public Weapon GetWeapon(int slot)
    {
        return weapons[slot];
    }

    public void AddWeapon(Weapon weapon)
    {
        weapons.Add(weapon);
    }
}
