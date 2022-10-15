using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace LightPat.Core.Player
{
    public class RootMotionManager : MonoBehaviour
    {
        public Rig rightArmRig;
        public Rig leftArmRig;

        Rigidbody rb;
        Animator animator;
        WeaponLoadout weaponLoadout;
        private void Start()
        {
            rb = GetComponentInParent<Rigidbody>();
            animator = GetComponent<Animator>();
            weaponLoadout = GetComponentInParent<WeaponLoadout>();
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

        private void OnAnimatorIK(int layerIndex)
        {
            if (!weaponLoadout.equippedWeapon) { return; }

            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightArmRig.weight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rightArmRig.weight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, weaponLoadout.equippedWeapon.rightHandGrip.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, weaponLoadout.equippedWeapon.rightHandGrip.rotation * Quaternion.Euler(-90, 0, 0));

            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftArmRig.weight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftArmRig.weight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, weaponLoadout.equippedWeapon.leftHandGrip.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, weaponLoadout.equippedWeapon.leftHandGrip.rotation * Quaternion.Euler(-90, 0, 0));
        }
    }
}
