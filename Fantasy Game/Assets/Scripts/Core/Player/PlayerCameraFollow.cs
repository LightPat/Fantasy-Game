using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Util;

namespace LightPat.Core.Player
{
    public class PlayerCameraFollow : MonoBehaviour
    {
        public PlayerController playerController;
        public Transform target;
        public RigWeightTarget neckAimRig;
        public float zRotDecay;
        public float targetZRot;
        public bool updateRotationWithTarget;

        bool previousRotationState;
        Animator playerAnimator;
        WeaponManager playerWeaponManager;
        AnimatorLayerWeightManager layerWeightManager;

        private void Start()
        {
            playerAnimator = playerController.GetComponentInChildren<Animator>();
            playerWeaponManager = playerController.GetComponent<WeaponManager>();
            layerWeightManager = playerController.GetComponentInChildren<AnimatorLayerWeightManager>();
        }

        private void Update()
        {
            transform.position = target.position;

            if (updateRotationWithTarget & !previousRotationState)
            {
                neckAimRig.weightTarget = 0;
                playerController.disableLookInput = true;
                if (playerWeaponManager.equippedWeapon != null)
                    layerWeightManager.SetLayerWeight(playerAnimator.GetLayerIndex(playerWeaponManager.equippedWeapon.animationClass), 0);
            }
            else if (!updateRotationWithTarget & previousRotationState)
            {
                neckAimRig.weightTarget = 1;
                playerController.disableLookInput = false;
                if (playerWeaponManager.equippedWeapon != null)
                    layerWeightManager.SetLayerWeight(playerAnimator.GetLayerIndex(playerWeaponManager.equippedWeapon.animationClass), 1);
            }

            if (updateRotationWithTarget)
            {
                transform.rotation = target.rotation;
            }
            else // Interpolate z rotation to 0
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, targetZRot), Time.deltaTime * zRotDecay);
            }

            previousRotationState = updateRotationWithTarget;
        }
    }
}