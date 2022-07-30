using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
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

            // Root transform moves
            RaycastHit hit;
            // If there is ground below a new step
            if (Physics.Raycast(controller.rootBone.position + (controller.rootBone.right * rightAxisFootSpacing) + (controller.rootBone.forward * (forwardAxisFootSpacing - 1)),
                Vector3.down, out hit, controller.physics.checkDistance, LayerMask.NameToLayer("Player")))
            {
                if (Vector3.Distance(newPosition, hit.point) > controller.stepDistance & permissionToMove)
                {
                    lerpProgress = 0;
                    newPosition = hit.point;

                    // This is supposed to solve walking in front of each leg
                    if (correspondingLegTarget.transform.position.z > transform.position.z)
                    {
                        newPosition.z += 1;
                    }
                }
            }
            else
            {
                lerpProgress = 1;
                currentPosition = controller.rootBone.position + (controller.rootBone.right * rightAxisFootSpacing) + (controller.rootBone.forward * forwardAxisFootSpacing) + (controller.rootBone.up * -3);
                newPosition = currentPosition;
            }

            if (lerpProgress < 1)
            {
                // Scale lerp speed with how far away the rootBone is from the leg
                lerpSpeed = Vector3.Distance(controller.rootBone.position, currentPosition);

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
