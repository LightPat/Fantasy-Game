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
        public string stowPoint;
        public float drawSpeed = 1;
        [HideInInspector] public string animationClass;

        [Header("Equipped Animation Offsets")]
        public Vector3 playerPositionOffset;
        public Vector3 playerRotationOffset;
        public Vector3 transitionPositionOffset;
        public Vector3 transitionRotationOffset;
        public Vector3 stowedPositionOffset;
        public Vector3 stowedRotationOffset;

        Vector3 targetLocalPosition;
        Vector3 targetLocalRotation;

        public bool disableUpdate;
        [Header("Used for setting offsets")]
        public bool settingOffsets;
        public string offsetType;

        public virtual void Attack1()
        {
            Debug.LogWarning("Attack1() hasn't been implemented yet on this weapon");
        }

        public virtual void Attack2()
        {
            Debug.LogWarning("Attack2() hasn't been implemented yet on this weapon");
        }

        public virtual IEnumerator Reload()
        {
            Debug.LogWarning("Reload hasn't been implemented yet on this weapon");
            yield return null;
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

        protected void Start()
        {
            targetLocalPosition = playerPositionOffset;
            targetLocalRotation = playerRotationOffset;
        }

        protected void Update()
        {
            if (disableUpdate) { return; }

            if (transform.parent != null)
            {
                if (settingOffsets)
                {
                    ChangeOffset(offsetType);
                    transform.localPosition = targetLocalPosition;
                    transform.localRotation = Quaternion.Euler(targetLocalRotation);
                }
                else
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPosition, Time.deltaTime * 8);
                    transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(targetLocalRotation), Time.deltaTime * 8);
                }
            }
        }
    }
}
