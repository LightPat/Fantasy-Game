using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat
{
    public class Weapon : MonoBehaviour
    {
        public Vector3 offset;
        public sbyte[] idealPersonality;
        public int baseDamage;

        private void Update()
        {
            if (transform.parent != null)
            {
                transform.localPosition = offset;
            }
        }
    }
}
