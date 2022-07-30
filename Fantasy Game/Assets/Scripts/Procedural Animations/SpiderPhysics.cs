using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat
{
    public class SpiderPhysics : MonoBehaviour
    {
        public float gravitySpeed;
        public float bodyVerticalOffset;
        private void Update()
        {
            // Adding artificial gravity since we have no rigidbody
            if (!Physics.Raycast(transform.position, Vector3.down, bodyVerticalOffset))
            {
                transform.Translate(Time.deltaTime * gravitySpeed * Vector3.down);
            }
        }
    }
}
