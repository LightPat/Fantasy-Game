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
        public Vector3 playerPositionOffset;
        public Vector3 playerRotationOffset;

        private void Update()
        {
            if (transform.parent != null)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, playerPositionOffset, Time.deltaTime * 8);
                transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(playerRotationOffset), Time.deltaTime * 8);
            }
        }
    }
}
