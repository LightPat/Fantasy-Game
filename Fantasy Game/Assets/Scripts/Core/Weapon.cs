using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using LightPat.Singleton;

namespace LightPat.Core
{
    public class Weapon : MonoBehaviour
    {
        public Transform rightHandGrip;
        public Transform leftHandGrip;

        [Header("Weapon Settings")]
        public string weaponName;
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
        public string currentOffsetType { get; private set; }

        Vector3 targetLocalPosition;
        Vector3 targetLocalRotation;

        public bool disableUpdate;
        [Header("Used for setting offsets")]
        public bool settingOffsets;
        public string offsetType;

        [HideInInspector] public bool disableAttack;

        public virtual NetworkObject Attack1(bool pressed)
        {
            Debug.LogWarning("Attack1() hasn't been implemented yet on this weapon");
            return null;
        }

        public virtual void Attack2(bool pressed)
        {
            Debug.LogWarning("Attack2() hasn't been implemented yet on this weapon");
        }

        public virtual IEnumerator Reload(bool animate)
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
            currentOffsetType = offsetType;
        }

        protected void Start()
        {
            name = weaponName;
            targetLocalPosition = playerPositionOffset;
            targetLocalRotation = playerRotationOffset;
            StartCoroutine(WaitForAudioManager());
        }

        private IEnumerator WaitForAudioManager()
        {
            yield return new WaitUntil(() => AudioManager.Singleton);

            AudioSource[] audioSources = GetComponentsInChildren<AudioSource>();
            foreach (AudioSource audioSource in audioSources)
            {
                AudioManager.Singleton.RegisterAudioSource(audioSource);
            }
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
