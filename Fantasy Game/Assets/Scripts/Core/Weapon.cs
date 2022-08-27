using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Weapon : MonoBehaviour
    {
        public sbyte[] idealPersonality;
        public int baseDamage;
        public string weaponClass;
        public Vector3 heldPositionOffset;
        public Vector3 heldRotationOffset;
    }
}
