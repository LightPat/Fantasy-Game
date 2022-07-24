using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class SpiderLegIKSolver : MonoBehaviour
    {
        [HideInInspector] public Transform rootBone;
        [HideInInspector] public SpiderLegsController controller;
        public float rightAxisFootSpacing;
        public float forwardAxisFootSpacing;
        [HideInInspector] public float stepDistance;
        [HideInInspector] public float lerpSpeed;
        [HideInInspector] public float stepHeight;

        public bool permissionToMove = true;
        public bool headLeg = false;

        private float lerpProgress;
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

            currentMovingState = permissionToMove;
        }

        private bool currentMovingState;
        private void Update()
        {
            transform.position = currentPosition;

            // Root transform moves
            RaycastHit hit;
            // If there is ground below a new step
            if (Physics.Raycast(rootBone.position + (rootBone.right * rightAxisFootSpacing) + (rootBone.forward * forwardAxisFootSpacing), Vector3.down, out hit, 10, LayerMask.NameToLayer("Player")))
            {
                if (Vector3.Distance(newPosition, hit.point) > stepDistance & permissionToMove)
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

            //if (headLeg)
            //{
                // If we are moving
                if (currentMovingState)
                {
                    // and we are stopping moving on this frame
                    if (currentMovingState != IsMoving())
                    {
                        // Send message to controller here to switch
                        controller.switchTrigger = true;
                        // Problems could arise here if multiple legs call for a switch at different times
                    }
                }
            //}
            
            currentMovingState = IsMoving();
        }

        public bool IsMoving()
        {
            return lerpProgress < 1;
        }
    }
}
