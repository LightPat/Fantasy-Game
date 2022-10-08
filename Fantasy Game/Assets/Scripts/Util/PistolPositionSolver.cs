using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Util
{
    public class PistolPositionSolver : MonoBehaviour
    {
        public Transform parentBone;
        public Vector3 positionMults;
        public Transform firstShoulder;
        public Transform secondShoulder;

        public void UpdateMultipliers(Vector3 newMultipliers)
        {
            positionMults = newMultipliers;
        }

        private void Update()
        {
            transform.rotation = parentBone.rotation;
            Vector3 endPosition = parentBone.position;
            endPosition += transform.right * positionMults.x + transform.up * positionMults.y + transform.forward * positionMults.z;
            endPosition += firstShoulder.position - secondShoulder.position;
            transform.position = endPosition;
        }
    }
}