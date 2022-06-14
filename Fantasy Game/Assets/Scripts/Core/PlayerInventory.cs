using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using LightPat.UI;
using UnityEngine.UI;

namespace LightPat.Core
{
    public class PlayerInventory : MonoBehaviour
    {
        public InspectModel modelCamera;
        public Transform weaponIconsParent;
        [Header("Attributes Display")]
        public Transform personality;
        public Transform physical;
        public Transform magical;

        private GameObject equippedWeapon;

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
            //equippedWeapon = g;
            if (g == null) { return; }

            foreach (Transform child in weaponIconsParent)
            {
                if (child.GetComponent<InventoryWeaponIcon>().weaponReference.name == g.name)
                {
                    child.GetComponent<Button>().onClick.Invoke();
                }
            }
        }

        public void UpdateInspectInput(bool newBool, int boolIndex)
        {
            if (boolIndex == 0)
            {
                modelCamera.GetComponent<InspectModel>().leftClickPressed = newBool;
            }
            else if (boolIndex == 1)
            {
                modelCamera.GetComponent<InspectModel>().reset = newBool;
            }
            else if (boolIndex == 2)
            {
                modelCamera.GetComponent<InspectModel>().rotateCamera = newBool;
            }
        }

        public void UpdateInspectInput(Vector2 newVector2, int vectorIndex)
        {
            if (vectorIndex == 0)
            {
                modelCamera.GetComponent<InspectModel>().mouseInput = newVector2;
            }
            else if (vectorIndex == 1)
            {
                modelCamera.GetComponent<InspectModel>().scrollInput = newVector2;
            }
        }
    }
}
