using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.UI
{
    public class WorldSpaceInfoLabel : MonoBehaviour
    {
        public float rotationSpeed;
        public float viewDistance;

        private void Update()
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0);
        }
    }
}
