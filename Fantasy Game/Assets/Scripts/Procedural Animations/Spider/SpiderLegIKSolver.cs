using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations.Spider
{
    public class SpiderLegIKSolver : MonoBehaviour
    {
        // Assigned from SpiderLegsController Script
        [HideInInspector] public SpiderLegsController controller;
        public float rightAxisFootSpacing;
        public float forwardAxisFootSpacing;
        public SpiderLegIKSolver correspondingLegTarget;
        [HideInInspector] public bool permissionToMove = true;

        private float lerpProgress;
        private float lerpSpeed;
        private Vector3 newPosition;
        private Vector3 currentPosition;
        private Vector3 oldPosition;

        private void Start()
        {
            if (transform.parent.parent.parent == null | !transform.parent.parent.parent.GetComponent<SpiderLegsController>())
            {
                Debug.LogWarning(transform + " is not the child of a SpiderLegsController component, so it probably won't work properly.");
            }

            currentPosition = transform.position;
            newPosition = transform.position;
            oldPosition = transform.position;
        }

        private void Update()
        {
            transform.position = currentPosition;

            RaycastHit[] allHits = Physics.RaycastAll(controller.rootBone.position + (controller.rootBone.right * rightAxisFootSpacing) + (controller.rootBone.forward * (forwardAxisFootSpacing - 1)),
                Vector3.down, controller.physics.isGroundedDistance);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

            bool bHit = false;

            foreach (RaycastHit hit in allHits)
            {
                // If we raycast anything that isn't this object go to the next hit
                if (hit.transform.gameObject == controller.rootBone.gameObject)
                {
                    continue;
                }

                bHit = true;

                if (Vector3.Distance(newPosition, hit.point) > controller.stepDistance & permissionToMove)
                {
                    lerpProgress = 0;
                    newPosition = hit.point;

                    // This is supposed to solve walking in front of each leg
                    //if (correspondingLegTarget.transform.position.z > transform.position.z & controller.physics.velocity.z > 0)
                    //{
                    //    newPosition.z += 1;
                    //}
                }
                break;
            }

            // If we didn't hit anything in the previous for loop
            if (!bHit)
            {
                lerpProgress = 1;
                currentPosition = controller.rootBone.position + (controller.rootBone.right * rightAxisFootSpacing) + (controller.rootBone.forward * forwardAxisFootSpacing) + (controller.rootBone.up * -3);
                newPosition = currentPosition;
                oldPosition = currentPosition;
            }

            if (lerpProgress < 1)
            {
                // Scale lerp speed with how fast we are moving
                float velocityAverage = controller.physics.velocity.magnitude * controller.lerpSpeedMultiplier;
                float angularVelocityAverage = controller.physics.angularVelocity.magnitude * controller.angularLerpSpeedMultiplier;
                
                lerpSpeed = velocityAverage + angularVelocityAverage;
                if (lerpSpeed < controller.minimumLerpSpeed)
                {
                    lerpSpeed = controller.minimumLerpSpeed;
                }

                Vector3 interpolatedPosition = Vector3.Lerp(oldPosition, newPosition, lerpProgress);
                interpolatedPosition.y += Mathf.Sin(lerpProgress * Mathf.PI) * controller.stepHeight;

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
