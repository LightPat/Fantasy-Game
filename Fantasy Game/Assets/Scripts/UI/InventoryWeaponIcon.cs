using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LightPat.UI
{
    public class InventoryWeaponIcon : MonoBehaviour
    {
        public GameObject weaponReference;
        public Vector3 initialInspectRotation;
        public TextMeshProUGUI weaponNameDisplay;
        public Transform weaponCamera;

        private void Start()
        {
            weaponNameDisplay.SetText(weaponReference.name);
        }
        
        public void OnClickDisplayWeapon()
        {
            GameObject UIWeapon = Instantiate(weaponReference, weaponCamera);
            UIWeapon.GetComponent<Rigidbody>().useGravity = false;
            UIWeapon.transform.localPosition = Vector3.forward * 2;
            UIWeapon.transform.rotation = Quaternion.Euler(initialInspectRotation);
            UIWeapon.transform.SetParent(null);
            UIWeapon.GetComponentInChildren<Collider>().enabled = false;
            weaponCamera.GetComponent<InspectChild>().displayedWeapon = UIWeapon;
        }
    }
}
