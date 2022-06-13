using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LightPat.UI
{
    public class InventoryWeaponIcon : MonoBehaviour
    {
        public GameObject weaponReference;
        public Vector3 inspectRotation;
        public TextMeshProUGUI nameDisplay;

        private void Start()
        {
            nameDisplay.SetText(weaponReference.name);
        }
    }
}
