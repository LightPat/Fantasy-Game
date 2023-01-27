using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Linq;

namespace LightPat.Core.Player
{
    public class RootMotionManager : MonoBehaviour
    {
        Rigidbody rb;
        Animator animator;
        WeaponLoadout weaponLoadout;

        private void Awake()
        {
            rb = GetComponentInParent<Rigidbody>();
            animator = GetComponent<Animator>();
            weaponLoadout = GetComponentInParent<WeaponLoadout>();
            rightArmConstraint = rightArmRig.GetComponentInChildren<TwoBoneIKConstraint>();
            leftArmConstraint = leftArmRig.GetComponentInChildren<TwoBoneIKConstraint>();
        }

        [Header("OnAnimatorMove")]
        public bool disableRootMotion;
        public float characterHeight;
        public float drag;
        private void OnAnimatorMove()
        {
            if (disableRootMotion) { return; }
            if (!rb)
            {
                rb = transform.parent.GetComponent<Rigidbody>();

                RaycastHit[] allHits = Physics.RaycastAll(transform.parent.position, animator.deltaPosition, 1, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
                bool applyRootMotion = true;
                foreach (RaycastHit hit in allHits)
                {
                    if (GetComponentsInChildren<Collider>().Contains(hit.collider)) { continue; }

                    applyRootMotion = false;
                    break;
                }

                if (!applyRootMotion)
                {
                    Vector3 position = new Vector3(transform.parent.position.x, transform.parent.position.y + characterHeight, transform.parent.position.z);
                    //Debug.DrawRay(position, animator.deltaPosition * 20, Color.black, 1);
                    allHits = Physics.RaycastAll(position, animator.deltaPosition, 1, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                    System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
                    foreach (RaycastHit hit in allHits)
                    {
                        if (GetComponentsInChildren<Collider>().Contains(hit.collider)) { continue; }

                        applyRootMotion = false;
                        break;
                    }
                }

                if (applyRootMotion)
                    transform.parent.position += animator.deltaPosition;

                return;
            }

            Vector3 newVelocity = Vector3.MoveTowards(rb.velocity * Time.timeScale, animator.velocity, drag);
            newVelocity.y = rb.velocity.y * Time.timeScale;
            rb.velocity = newVelocity / Time.timeScale;
        }

        [Header("OnAnimatorIK")]
        public Rig rightArmRig;
        public Rig leftArmRig;
        public bool disableRightHand;
        public bool disableLeftHand;
        TwoBoneIKConstraint rightArmConstraint;
        TwoBoneIKConstraint leftArmConstraint;
        private void OnAnimatorIK(int layerIndex)
        {
            if (!weaponLoadout.equippedWeapon) { return; }

            if (disableRightHand)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightArmRig.weight * rightArmConstraint.weight);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rightArmRig.weight * rightArmConstraint.weight);
                animator.SetIKPosition(AvatarIKGoal.RightHand, weaponLoadout.equippedWeapon.rightHandGrip.position);
                animator.SetIKRotation(AvatarIKGoal.RightHand, weaponLoadout.equippedWeapon.rightHandGrip.rotation * Quaternion.Euler(-90, 0, 0));
            }

            if (disableLeftHand)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftArmRig.weight * leftArmConstraint.weight);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftArmRig.weight * leftArmConstraint.weight);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, weaponLoadout.equippedWeapon.leftHandGrip.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, weaponLoadout.equippedWeapon.leftHandGrip.rotation * Quaternion.Euler(-90, 0, 0));
            }
        }
    }
}
