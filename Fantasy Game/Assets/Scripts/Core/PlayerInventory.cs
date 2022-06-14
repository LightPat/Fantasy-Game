using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using LightPat.UI;

namespace LightPat.Core
{
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Attributes Display")]
        public Transform personality;
        public Transform physical;
        public Transform magical;
        [Header("Model Inspect Window")]
        public InspectChild modelCamera;

        [HideInInspector]
        public GameObject equippedWeapon;
        [HideInInspector]
        public bool leftClickPressed, reset, rotateCamera;
        [HideInInspector]
        public Vector2 mouseInput, scrollInput;
        [HideInInspector]
        public GameObject inspectingModel;

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

        private void Update()
        {
            modelCamera.GetComponent<InspectChild>().mouseInput = mouseInput;
            modelCamera.GetComponent<InspectChild>().leftClickPressed = leftClickPressed;
            modelCamera.GetComponent<InspectChild>().scrollInput = scrollInput;
            modelCamera.GetComponent<InspectChild>().reset = reset;
            modelCamera.GetComponent<InspectChild>().rotateCamera = rotateCamera;
        }
    }
}
