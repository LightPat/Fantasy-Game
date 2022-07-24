using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class SpiderLegIKSolver : MonoBehaviour
    {
        public Transform rootBone;
        public float rightAxisFootSpacing;
        public float forwardAxisFootSpacing;
        public float stepDistance;
        public float lerpSpeed;
        public float stepHeight;

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
            if (Physics.Raycast(rootBone.position + (rootBone.right * rightAxisFootSpacing) + (rootBone.forward * forwardAxisFootSpacing), Vector3.down, out hit, 10, LayerMask.NameToLayer("Player")))
            {
                if (Vector3.Distance(newPosition, hit.point) > stepDistance)
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

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(newPosition, 0.2f);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(oldPosition, 0.2f);
        }
    }
}
