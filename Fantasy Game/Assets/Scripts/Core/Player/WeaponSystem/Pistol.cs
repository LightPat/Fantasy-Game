using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Util;

namespace LightPat.Core.Player
{
    public class Pistol : Gun
    {
        [Header("Pistol Specific")]
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
