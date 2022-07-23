using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class FootIKSolver : MonoBehaviour
    {
        public Transform rootBone;
        public float footSpacing;
        public float stepDistance;
        public float lerpSpeed;
        public float stepHeight;

        public float progress;

        private Vector3 newPosition;
        private Vector3 oldPosition;

        private void Start()
        {
            newPosition = transform.position;
            oldPosition = Vector3.forward;
        }

        private void Update()
        {
            RaycastHit hit;
            // If there is ground below us, assign target position to be that ground
            if (Physics.Raycast(rootBone.position + (rootBone.right * footSpacing), Vector3.down, out hit, LayerMask.NameToLayer("Player")))
            {
                if (Vector3.Distance(transform.position, hit.point) > stepDistance)
                {
                    newPosition = hit.point;
                }
            }

            // Change back to lerp
            Vector3 interpolatedPosition = Vector3.MoveTowards(transform.position, newPosition, lerpSpeed * Time.deltaTime);

            // Get distance between oldPosition and newPosition
            // Get the percentage along we are, so it is the distance between transform.position and newPosition / #1
            //float progress = 1 - Vector3.Distance(transform.position, newPosition) / Vector3.Distance(oldPosition, newPosition);
            //Debug.Log(progress);
            interpolatedPosition.y += Mathf.Sin(progress * Mathf.PI) * stepHeight;

            //if (progress < 0.5)
            //{
            //    oldPosition = transform.position;
            //}


            transform.position = interpolatedPosition;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(newPosition, 0.2f);
        }
    }
}
