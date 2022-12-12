using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using LightPat.ProceduralAnimations;

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
        [HideInInspector] public bool deactivateWeaponLayers;

        bool previousRotationState;
        Animator playerAnimator;
        WeaponLoadout playerWeaponLoadout;
        AnimatorLayerWeightManager layerWeightManager;

        public void RefreshCameraParent()
        {
            if (updateRotationWithTarget)
            {
                transform.SetParent(BoneRotParent, true);
            }
            else
            {
                if (!playerController.rotateBodyWithCamera)
                {
                    transform.SetParent(playerController.transform.parent, true);
                }
                else
                {
                    playerController.transform.rotation = Quaternion.Euler(playerController.bodyRotation);
                    transform.SetParent(CamParent, true);
                    transform.localPosition = Vector3.zero;
                }
            }
        }

        private void Start()
        {
            playerAnimator = playerController.GetComponentInChildren<Animator>();
            playerWeaponLoadout = playerController.GetComponent<WeaponLoadout>();
            layerWeightManager = playerController.GetComponentInChildren<AnimatorLayerWeightManager>();
        }

        private void LateUpdate()
        {
            transform.position = followTarget.position;

            if (updateRotationWithTarget & !previousRotationState) // if we just activated updateRotationWithTarget
            {
                //neckAimRig.weightTarget = 0;
                neckAimRig.GetComponentInChildren<MultiAimConstraint>().weight = 0;
                neckAimRig.GetComponentInChildren<MultiRotationConstraint>().weight = 1;
                playerController.disableLookInput = true;
                playerController.SetLean(0);
                RefreshCameraParent();
                // Fixes coming out of a breakfall roll and turning 90 degrees for no reason cause the animation is bad
                if (playerWeaponLoadout.equippedWeapon != null & deactivateWeaponLayers)
                    layerWeightManager.SetLayerWeight(playerAnimator.GetLayerIndex(playerWeaponLoadout.equippedWeapon.animationClass), 0);
            }
            else if (!updateRotationWithTarget & previousRotationState) // if we just deactivated updateRotationWithTarget
            {
                //neckAimRig.weightTarget = 1;
                neckAimRig.GetComponentInChildren<MultiRotationConstraint>().weight = 0;
                neckAimRig.GetComponentInChildren<MultiAimConstraint>().weight = 1;
                playerController.disableLookInput = false;
                RefreshCameraParent();
                // Fixes coming out of a breakfall roll and turning 90 degrees for no reason cause the animation is bad
                if (playerWeaponLoadout.equippedWeapon != null)
                    layerWeightManager.SetLayerWeight(playerAnimator.GetLayerIndex(playerWeaponLoadout.equippedWeapon.animationClass), 1);
            }

            if (!updateRotationWithTarget) // "Tilt" the parent constraint by a Z offset so that you don't have to mess with the camera's actual rotation
                leanConstraint.data.offset = Vector3.Lerp(leanConstraint.data.offset, new Vector3(0, 0, targetZRot), zRotOffsetSpeed * Time.deltaTime);
            else // Remove localRotation from camera during an animation because the rotation is stored on the parent
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.identity, zLocalRotDecay * Time.deltaTime);

            // Remove local Z rot over time
            if (playerController.rotateBodyWithCamera)
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(playerController.attemptedXAngle, 0, 0), zLocalRotDecay * Time.deltaTime);
            else
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(transform.localEulerAngles.x, transform.localEulerAngles.y, 0), zLocalRotDecay * Time.deltaTime);

            previousRotationState = updateRotationWithTarget;
        }
    }
}