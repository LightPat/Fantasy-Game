using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class PistolPositionSolver : MonoBehaviour
    {
        public Transform positionSource;
        public Transform rotationSource;
        public float forwardMult;
        public float upMult;
        public float rightMult;

        private void Update()
        {
            transform.position = positionSource.position + transform.forward * forwardMult + transform.up * upMult + transform.right * rightMult;
            transform.rotation = rotationSource.rotation;
        }
    }
}