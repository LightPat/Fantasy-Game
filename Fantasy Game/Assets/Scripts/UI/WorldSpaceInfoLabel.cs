using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.UI
{
    public class WorldSpaceInfoLabel : MonoBehaviour
    {
        public float rotationSpeed;
        public float animationSpeed = 0.02f;
        public float viewDistance = 10f;

        private void Update()
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0);

            if (Vector3.Distance(Camera.main.transform.position, transform.position) > viewDistance)
            {
                // Might have to add time.DeltaTime to this instead of just using animationSpeed
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, animationSpeed);
            }
            else
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, animationSpeed);
            }
        }
    }
}
