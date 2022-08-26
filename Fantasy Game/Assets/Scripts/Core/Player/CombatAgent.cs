using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using LightPat.ProceduralAnimations;

namespace LightPat.Core.Player
{
    public class CombatAgent : MonoBehaviour
    {
        public GameObject equippedWeapon = null;
        public bool combat;

        AnimationLayerWeightManager weightManager;

        private void Start()
        {
            weightManager = GetComponentInChildren<AnimationLayerWeightManager>();
        }

        [Header("Reach Procedural Anim Settings")]
        public float reach;
        public float reachSpeed;
        public Rig armsRig;
        public Transform rightHandTarget;
        public Transform leftHandTarget;
        void OnInteract()
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, reach))
            {
                if (hit.transform.GetComponent<Weapon>())
                {
                    //if (equippedWeapon != null) { equippedWeapon.SetActive(false); }

                    //StartCoroutine(PickUpWeapon(hit.transform.Find("ref_right_hand_grip")));
                    armsRig.GetComponent<RigWeightTarget>().weightSpeed = reachSpeed;
                    armsRig.GetComponent<RigWeightTarget>().weightTarget = 1;

                    rightHandTarget.GetComponent<FollowTarget>().target = hit.transform.Find("ref_right_hand_grip");
                }
            }
        }

        public float attackReach;
        public float attackDamage;
        void OnAttack2(InputValue value) // TODO change this to attack1
        {
            if (!value.isPressed) { return; }

            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, attackReach))
            {
                if (hit.transform.GetComponent<Attributes>())
                {
                    hit.transform.GetComponent<Attributes>().InflictDamage(attackDamage, gameObject);
                }
            }
        }

        void OnSlot1() // TODO not finished yet
        {
            combat = !combat;
            if (combat)
            {
                if (equippedWeapon == null)
                {
                    weightManager.SetLayerWeight("Fists", 1);
                }
                else
                {
                    // GetComponent<Weapon>().weaponClass;
                    weightManager.SetLayerWeight("", 1);
                }
            }
            else
            {
                if (equippedWeapon == null)
                {
                    weightManager.SetLayerWeight("Fists", 0);
                }
                else
                {
                    // GetComponent<Weapon>().weaponClass;
                    weightManager.SetLayerWeight("", 1);
                }
            }
        }
    }
}
