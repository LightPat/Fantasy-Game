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
        WeaponLoadout playerWeaponLoadout;
        AnimatorLayerWeightManager layerWeightManager;

        private void Start()
        {
            playerAnimator = playerController.GetComponentInChildren<Animator>();
            playerWeaponLoadout = playerController.GetComponent<WeaponLoadout>();
            layerWeightManager = playerController.GetComponentInChildren<AnimatorLayerWeightManager>();
        }

        private void Update()
        {
            if (!playerController.rotateBodyWithCamera)
                transform.position = followTarget.position;

            if (updateRotationWithTarget & !previousRotationState) // if we just activated updateRotationWithTarget
            {
                neckAimRig.weightTarget = 0;
                playerController.disableLookInput = true;
                playerController.SetLean(0);
                transform.SetParent(BoneRotParent, true);
                // Fixes coming out of a breakfall roll and turning 90 degrees for no reason cause the animation is bad
                if (playerWeaponLoadout.equippedWeapon != null)
                    layerWeightManager.SetLayerWeight(playerAnimator.GetLayerIndex(playerWeaponLoadout.equippedWeapon.animationClass), 0);
            }
            else if (!updateRotationWithTarget & previousRotationState) // if we just deactivated updateRotationWithTarget
            {
                neckAimRig.weightTarget = 1;
                playerController.disableLookInput = false;
                if (playerController.rotateBodyWithCamera)
                    transform.SetParent(CamParent, true);
                else
                    transform.SetParent(null, true);
                // Fixes coming out of a breakfall roll and turning 90 degrees for no reason cause the animation is bad
                if (playerWeaponLoadout.equippedWeapon != null)
                    layerWeightManager.SetLayerWeight(playerAnimator.GetLayerIndex(playerWeaponLoadout.equippedWeapon.animationClass), 1);
            }

            if (!updateRotationWithTarget) // "Tilt" the parent constraint by a Z offset so that you don't have to mess with the camera's actual rotation
            {
                leanConstraint.data.offset = Vector3.Lerp(leanConstraint.data.offset, new Vector3(0, 0, targetZRot), zRotOffsetSpeed * Time.deltaTime);
            }
            else // Remove localRotation from camera during an animation because the rotation is stored on the parent
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.identity, zLocalRotDecay * Time.deltaTime);
            }

            // Remove local Z rot over time
            if (playerController.rotateBodyWithCamera)
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(playerController.attemptedXAngle, 0, 0), zLocalRotDecay * Time.deltaTime);
            else
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(transform.localEulerAngles.x, transform.localEulerAngles.y, 0), zLocalRotDecay * Time.deltaTime);

            previousRotationState = updateRotationWithTarget;
        }
    }
}