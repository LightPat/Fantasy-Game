using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LightPat.Core
{
    public class PlayerInventory : MonoBehaviour
    {
        public Transform personality;
        public Transform physical;
        public Transform magical;

        [HideInInspector]
        public GameObject equippedWeapon;

        public void UpdateAttributes(Attributes newAttributes)
        {
            int i = 0;
            foreach (Transform child in personality)
            {
                child.GetComponent<TextMeshProUGUI>().SetText(child.name + " " + newAttributes.personalityValues[i]);
                i++;
            }

            i = 0;
            foreach (Transform child in physical)
            {
                child.GetComponent<TextMeshProUGUI>().SetText(child.name + " " + newAttributes.physicalValues[i]);
                i++;
            }

            i = 0;
            foreach (Transform child in magical)
            {
                child.GetComponent<TextMeshProUGUI>().SetText(child.name + " " + newAttributes.magicalValues[i]);
                i++;
            }
        }

        public void UpdateWeapon(GameObject g)
        {
            equippedWeapon = g;
        }
    }
}
