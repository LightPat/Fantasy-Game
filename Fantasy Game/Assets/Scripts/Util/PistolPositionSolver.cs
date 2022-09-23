using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Util
{
    public class PistolPositionSolver : MonoBehaviour
    {
        public Transform parentBone;
        public float forwardMult;
        public float rightMult;
        public float upMult;
        public Transform firstShoulder;
        public Transform secondShoulder;

        private void Update()
        {
            transform.position = parentBone.position;
            transform.rotation = parentBone.rotation;
            transform.position += transform.forward * forwardMult + transform.right * rightMult + transform.up * upMult;
            transform.position += firstShoulder.position - secondShoulder.position;
        }
    }
}