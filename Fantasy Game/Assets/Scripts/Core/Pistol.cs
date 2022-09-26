using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Pistol : Weapon
    {
        [Header("Pistol Specific")]
        public Transform rightFingersGrips;
        public Transform leftFingersGrips;
        public float forwardMult;
        public float rightMult;
        public float upMult;

        private new void Start()
        {
            base.Start();
            animationClass = "Pistol";
        }
    }
}
