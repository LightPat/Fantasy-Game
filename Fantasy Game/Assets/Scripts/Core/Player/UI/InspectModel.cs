using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core.Player
{
    public class InspectModel : MonoBehaviour
    {
        public float rotateCamSpeed = 0.1f;
        public float scrollSpeed = 0.1f;
        [HideInInspector]
        public GameObject displayedModel;
        [HideInInspector]
        public Vector2 mouseInput, scrollInput;
        [HideInInspector]
        public bool leftClickPressed, reset, rotateCamera;

        private Camera thisCam;

        private void Start()
        {
            thisCam = GetComponent<Camera>();
        }

        private void Update()
        {
            if (displayedModel == null) { return; }

            // Rotate rigidbody by holding left click and moving your mouse
            if (leftClickPressed)
            {
                displayedModel.GetComponent<Rigidbody>().AddTorque(new Vector3(mouseInput.y, 0, -mouseInput.x));
            }

            // Press R to remove all forces from the rigidbody
            if (reset)
            {
                displayedModel.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            }

            // Rotate camera with right click and moving your mouse
            if (rotateCamera)
            {
                transform.RotateAround(displayedModel.transform.position, Vector3.up, mouseInput.x * rotateCamSpeed);
                transform.RotateAround(displayedModel.transform.position, Vector3.left, mouseInput.y * rotateCamSpeed);
            }

            // Camera zooming with scroll whell
            thisCam.fieldOfView -= scrollInput.y * scrollSpeed;
        }

        private void OnDisable()
        {
            // Destroy model when this script is destroyed or disabled
            DestroyDisplayedModel();
        }

        public void DestroyDisplayedModel()
        {
            if (displayedModel != null) { Destroy(displayedModel); }
        }
    }
}
