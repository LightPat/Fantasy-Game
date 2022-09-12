using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class HintPositionSolver : MonoBehaviour
    {
        public Transform shoulder;
        public Transform wrist;
        public Vector3 offset;

        private void Update()
        {
            Vector3 wristPosition = wrist.position + wrist.up * -1;
            Vector3 shoulderPosition = shoulder.position + shoulder.up;

            transform.position = (shoulderPosition + wristPosition) / 2;
            transform.localPosition += offset;
        }
    }
}
