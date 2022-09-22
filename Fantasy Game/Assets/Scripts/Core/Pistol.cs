using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Pistol : Weapon
    {
        private new void Start()
        {
            base.Start();
            weaponClass = "Pistol";
        }
    }
}
