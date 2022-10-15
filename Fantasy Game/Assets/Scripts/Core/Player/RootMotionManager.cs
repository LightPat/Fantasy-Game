using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core.Player
{
    public class RootMotionManager : MonoBehaviour
    {
        Rigidbody rb;
        Animator animator;
        private void Start()
        {
            rb = GetComponentInParent<Rigidbody>();
            animator = GetComponent<Animator>();
        }

        public bool disable;
        public float drag;
        private void OnAnimatorMove()
        {
            if (disable) { return; }

            Vector3 newVelocity = Vector3.MoveTowards(rb.velocity * Time.timeScale, animator.velocity, drag);
            newVelocity.y = rb.velocity.y * Time.timeScale;
            rb.velocity = newVelocity / Time.timeScale;
        }

        //private void OnAnimatorIK(int layerIndex)
        //{
        //    if (animator.GetLayerWeight(layerIndex) != 1) { return; }

        //    WeaponLoadout weapons = GetComponentInParent<WeaponLoadout>();

        //    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
        //    animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
        //    animator.SetIKPosition(AvatarIKGoal.RightHand, weapons.equippedWeapon.rightHandGrip.position);
        //    animator.SetIKRotation(AvatarIKGoal.RightHand, weapons.equippedWeapon.rightHandGrip.rotation * Quaternion.Euler(-90, 0, 0));

        //    //animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
        //    //animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
        //    //animator.SetIKPosition(AvatarIKGoal.LeftHand, weapons.equippedWeapon.leftHandGrip.position);
        //    //animator.SetIKRotation(AvatarIKGoal.LeftHand, weapons.equippedWeapon.leftHandGrip.rotation * Quaternion.Euler(-90, 0, 0));
        //}
    }
}
