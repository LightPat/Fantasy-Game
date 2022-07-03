using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class PlayerCameraFollow : MonoBehaviour
    {
        public Transform target;
        public Quaternion offset;
        [HideInInspector]
        public bool UpdateRotation;

        private void Update()
        {
            transform.position = target.position;

            if (UpdateRotation)
            {
                // Append rotation instead of setting it so that it doesn't mess with where the player was looking before
                transform.rotation = target.rotation;
            }
        }
    }
}
