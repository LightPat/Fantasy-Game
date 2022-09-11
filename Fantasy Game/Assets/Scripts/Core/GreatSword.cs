using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class GreatSword : Weapon
    {
        [Header("Great Sword Specific")]
        public Vector3 blockingPosition;

        private new void Start()
        {
            base.Start();
            weaponClass = "Great Sword";
        }
    }
}