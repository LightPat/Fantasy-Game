using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

public class WeaponManager : MonoBehaviour
{
    public Weapon[] weapons;

    public void AddWeapon(Weapon weapon)
    {
        Weapon[] newWeapons = new Weapon[weapons.Length + 1];

        for (int i = 0; i < weapons.Length; i++)
        {
            newWeapons[i] = weapons[i];
            if (i + 1 == weapons.Length)
            {
                newWeapons[i + 1] = weapon;
            }

            weapons = newWeapons;

            foreach (Weapon w in weapons)
            {
                Debug.Log(w);
            }
        }
    }
}
