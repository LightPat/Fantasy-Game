using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Util;

namespace LightPat.Core.Player
{
    public class Rifle : Gun
    {
        private new void Start()
        {
            base.Start();
            animationClass = "Rifle";
        }
    }
}