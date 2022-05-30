using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.UI
{
    public class WorldSpaceInfoLabel : MonoBehaviour
    {
        public float rotationSpeed = 0.2f;
        public float animationSpeed = 0.02f;
        public float viewDistance = 10f;

        private void Update()
        {
            Quaternion rotTarget = Quaternion.LookRotation(Camera.main.transform.position - transform.position);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotTarget, rotationSpeed);

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
