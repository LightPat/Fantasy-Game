using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Util;

namespace LightPat.Core.Player
{
    public class Rifle : Gun
    {
        [Header("Rifle Specific")]
        public bool fullAuto;
        public bool firing;

        private new void Start()
        {
            base.Start();
            animationClass = "Rifle";
        }

        public override void Attack1(bool pressed)
        {
            if (fullAuto)
            {
                firing = pressed;
            }
            else
            {
                base.Attack1(pressed);
            }
        }

        private new void Update()
        {
            base.Update();
            if (fullAuto)
                base.Attack1(firing);
        }
    }
}