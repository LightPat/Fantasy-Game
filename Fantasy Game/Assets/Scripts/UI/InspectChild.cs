using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat
{
    public class InspectChild : MonoBehaviour
    {
        public float scrollSpeed = 0.1f;
        [HideInInspector]
        public GameObject displayedWeapon;
        [HideInInspector]
        public Vector2 mouseInput, scrollInput;
        [HideInInspector]
        public bool leftClickPressed, reset;

        private Camera thisCam;

        private void Start()
        {
            thisCam = GetComponent<Camera>();
        }

        private void Update()
        {
            if (leftClickPressed & displayedWeapon != null)
            {
                displayedWeapon.GetComponent<Rigidbody>().AddTorque(new Vector3(mouseInput.y, 0, -mouseInput.x));
            }

            if (reset)
            {
                displayedWeapon.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            }

            thisCam.fieldOfView -= scrollInput.y * scrollSpeed;
        }
    }
}
