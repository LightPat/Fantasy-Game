using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Pistol : Weapon
    {
        [Header("Pistol Specific")]
        public float forwardMult;
        public float rightMult;
        public float upMult;

        private new void Start()
        {
            base.Start();
            weaponClass = "Pistol";
        }
    }
}
