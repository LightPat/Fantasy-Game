using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Sheath : MonoBehaviour
    {
        [Header("Equipped Animation Offsets")]
        public Vector3 stowedPositionOffset;
        public Vector3 stowedRotationOffset;

        [Header("Used for setting offsets")]
        public bool settingOffsets;
        public string offsetType;

        private void Update()
        {
            if (transform.parent != null)
            {
                if (settingOffsets)
                {

                }
                else
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, stowedPositionOffset, Time.deltaTime * 5);
                    transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(stowedRotationOffset), Time.deltaTime * 5);
                }
            }
        }
    }
}