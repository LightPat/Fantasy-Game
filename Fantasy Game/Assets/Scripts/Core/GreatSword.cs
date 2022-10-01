using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class GreatSword : Weapon
    {
        [Header("Great Sword Specific")]
        public Transform leftFingersGrips;

        private new void Start()
        {
            base.Start();
            animationClass = "Great Sword";
        }
    }
}