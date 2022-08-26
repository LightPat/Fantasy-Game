using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

        public float attackReach;
        public float attackDamage;
        void OnInteract(InputValue value)
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

        void OnSlot1()
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
