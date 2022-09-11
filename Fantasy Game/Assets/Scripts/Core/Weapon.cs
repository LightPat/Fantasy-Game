using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Weapon : MonoBehaviour
    {
        public Transform rightHandGrip;
        public Transform leftHandGrip;

        [Header("Weapon Settings")]
        public sbyte[] idealPersonality;
        public int baseDamage;
        public string weaponClass;
        public string stowPoint;
        public float drawSpeed = 1;

        [Header("Equipped Animation Offsets")]
        public Vector3 playerPositionOffset;
        public Vector3 playerRotationOffset;
        public Vector3 transitionPositionOffset;
        public Vector3 transitionRotationOffset;
        public Vector3 stowedPositionOffset;
        public Vector3 stowedRotationOffset;

        Vector3 targetLocalPosition;
        Vector3 targetLocalRotation;

        [Header("Used for setting offsets")]
        public bool settingOffsets;
        public bool disableRotation;
        public string offsetType;

        public virtual void Attack1()
        {
            Debug.LogWarning("Attack1() hasn't been implemented yet on this weapon");
        }

        public virtual void Attack2()
        {
            Debug.LogWarning("Attack2() hasn't been implemented yet on this weapon");
        }

        public void ChangeOffset(string offsetType)
        {
            if (offsetType == "player")
            {
                targetLocalPosition = playerPositionOffset;
                targetLocalRotation = playerRotationOffset;
            }
            else if (offsetType == "stowed")
            {
                targetLocalPosition = stowedPositionOffset;
                targetLocalRotation = stowedRotationOffset;
            }
            else if (offsetType == "transition")
            {
                targetLocalPosition = transitionPositionOffset;
                targetLocalRotation = transitionRotationOffset;
            }
        }

        private void Start()
        {
            targetLocalPosition = playerPositionOffset;
            targetLocalRotation = playerRotationOffset;
        }

        private void Update()
        {
            if (transform.parent != null)
            {
                if (transform.parent.name == "Y Bot Player") { return; }

                if (settingOffsets)
                {
                    ChangeOffset(offsetType);
                    transform.localPosition = targetLocalPosition;
                    if (disableRotation) { return; }
                    transform.localRotation = Quaternion.Euler(targetLocalRotation);
                }
                else
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPosition, Time.deltaTime * 8);
                    if (disableRotation) { return; }
                    transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(targetLocalRotation), Time.deltaTime * 8);
                    //transform.localPosition = targetLocalPosition;
                    //transform.localRotation = Quaternion.Euler(targetLocalRotation);
                }
            }
        }
    }
}
