using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class RiflePositionSolver : MonoBehaviour
    {
        public Transform parentBone;
        public Vector3 positionMults;

        public void UpdateMultipliers(Vector3 newMultipliers)
        {
            positionMults = newMultipliers;
        }

        private void Update()
        {
            transform.rotation = parentBone.rotation;
            Vector3 endPosition = parentBone.position;
            endPosition += transform.right * positionMults.x + transform.up * positionMults.y + transform.forward * positionMults.z;
            transform.position = endPosition;
        }
    }
}