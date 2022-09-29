using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Util
{
    public class PistolPositionSolver : MonoBehaviour
    {
        public float lerpSpeed;
        public Transform parentBone;
        public float forwardMult;
        public float rightMult;
        public float upMult;
        public Transform firstShoulder;
        public Transform secondShoulder;

        public void UpdateMultipliers(float forward, float right, float up)
        {
            forwardMult = forward;
            rightMult = right;
            upMult = up;
        }

        private void Update()
        {
            transform.rotation = parentBone.rotation;
            Vector3 endPosition = parentBone.position;
            endPosition += transform.forward * forwardMult + transform.right * rightMult + transform.up * upMult;
            endPosition += firstShoulder.position - secondShoulder.position;
            //transform.position = Vector3.Lerp(transform.position, endPosition, Time.deltaTime * lerpSpeed);
            transform.position = endPosition;
        }
    }
}