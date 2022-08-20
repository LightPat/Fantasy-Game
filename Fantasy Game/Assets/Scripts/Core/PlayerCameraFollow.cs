using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class PlayerCameraFollow : MonoBehaviour
    {
        public Transform target;
        public float rotationSpeed;
        public bool UpdateRotation;

        private void Update()
        {
            transform.position = target.position;

            if (UpdateRotation)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target.rotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
