using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class FootIKSolver : MonoBehaviour
    {
        public Transform rootBone;
        public float footSpacing = 0.2f;
        public float stepDistance = 0.7f;
        public float lerpSpeed = 5;
        public float stepHeight = 0.5f;
        public FootIKSolver otherFoot;

        private float lerpProgress;
        private Vector3 newPosition;
        private Vector3 currentPosition;
        private Vector3 oldPosition;

        private void Start()
        {
            currentPosition = transform.position;
            newPosition = transform.position;
            oldPosition = transform.position;
        }

        private void Update()
        {
            transform.position = currentPosition;

            // Root transform moves
            RaycastHit hit;
            // If there is ground below a new step
            if (Physics.Raycast(rootBone.position + (rootBone.right * footSpacing), Vector3.down, out hit, 10, LayerMask.NameToLayer("Player")))
            {
                if (Vector3.Distance(newPosition, hit.point) > stepDistance & !otherFoot.IsMoving())
                {
                    lerpProgress = 0;
                    newPosition = hit.point;
                }
            }

            if (lerpProgress < 1)
            {
                Vector3 interpolatedPosition = Vector3.Lerp(oldPosition, newPosition, lerpProgress);
                interpolatedPosition.y += Mathf.Sin(lerpProgress * Mathf.PI) * stepHeight;

                currentPosition = interpolatedPosition;

                lerpProgress += Time.deltaTime * lerpSpeed;
            }
            else
            {
                oldPosition = newPosition;
            }
        }

        public bool IsMoving()
        {
            return lerpProgress < 1;
        }
    }
}
