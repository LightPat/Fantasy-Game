using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class GreatSword : Weapon
    {
        [Header("Great Sword Specific")]
        public Vector3 blockingPosition;
        public Vector3 blockingRotation;

        //private void Start()
        //{
        //    weaponClass = "Great Sword";
        //}
    }
}