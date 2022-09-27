using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Util;
using UnityEngine.Animations.Rigging;

namespace LightPat.Core.Player
{
    public class PlayerCameraFollow : MonoBehaviour
    {
        public Transform followTarget;
        public PlayerController playerController;
        public Transform CamParent;
        public Transform BoneRotParent;
        public MultiRotationConstraint leanConstraint;
        public RigWeightTarget neckAimRig;
        public float zLocalRotDecay;
        public float zRotOffsetSpeed;
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
            if (updateRotationWithTarget & !previousRotationState) // if we just activated updateRotationWithTarget
            {
                neckAimRig.weightTarget = 0;
                playerController.disableLookInput = true;
                transform.SetParent(BoneRotParent, true);
                // Fixes coming out of a breakfall roll and turning 90 degrees for no reason cause the animation is bad
                if (playerWeaponManager.equippedWeapon != null)
                    layerWeightManager.SetLayerWeight(playerAnimator.GetLayerIndex(playerWeaponManager.equippedWeapon.animationClass), 0);
            }
            else if (!updateRotationWithTarget & previousRotationState) // if we just deactivated updateRotationWithTarget
            {
                neckAimRig.weightTarget = 1;
                playerController.disableLookInput = false;
                transform.SetParent(CamParent);
                // Fixes coming out of a breakfall roll and turning 90 degrees for no reason cause the animation is bad
                if (playerWeaponManager.equippedWeapon != null)
                    layerWeightManager.SetLayerWeight(playerAnimator.GetLayerIndex(playerWeaponManager.equippedWeapon.animationClass), 1);
            }

            if (!updateRotationWithTarget)
            {
                leanConstraint.data.offset = Vector3.Lerp(leanConstraint.data.offset, new Vector3(0, 0, targetZRot), zRotOffsetSpeed * Time.deltaTime);
            }
            else
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.identity, zLocalRotDecay * Time.deltaTime);
            }

            if (!playerController.rotateBodyWithCamera)
                transform.localPosition = followTarget.position;
            else
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(playerController.attemptedXAngle, 0, 0), zLocalRotDecay * Time.deltaTime);

            previousRotationState = updateRotationWithTarget;
        }
    }
}