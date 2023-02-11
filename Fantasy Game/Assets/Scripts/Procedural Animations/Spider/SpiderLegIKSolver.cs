using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace LightPat.ProceduralAnimations.Spider
{
    public class SpiderLegIKSolver : NetworkBehaviour
    {
        // Assigned from SpiderLegsController Script
        [HideInInspector] public SpiderLegsController controller;
        public float rightAxisFootSpacing;
        public float forwardAxisFootSpacing;
        public SpiderLegIKSolver correspondingLegTarget;

        [HideInInspector] public bool permissionToMove = true;

        public RaycastHit raycastHit { get; private set; }

        private float lerpProgress;
        private Vector3 newPosition;
        private Vector3 currentPosition;
        private Vector3 oldPosition;

        private NetworkVariable<float> lerpSpeed = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private void Start()
        {
            if (!transform.GetComponentInParent<SpiderLegsController>())
                Debug.LogWarning(transform + " is not the child of a SpiderLegsController component, so it probably won't work properly.");

            currentPosition = transform.position;
            newPosition = transform.position;
            oldPosition = transform.position;
        }

        private void Update()
        {
            transform.position = currentPosition;

            Vector3 raycastStartPosition = controller.rootBone.position + (controller.rootBone.right * rightAxisFootSpacing) + (controller.rootBone.forward * (forwardAxisFootSpacing - 1));
            RaycastHit[] allHits = Physics.RaycastAll(raycastStartPosition, controller.rootBone.up * -1, controller.physics.bodyVerticalOffset * 3, -1, QueryTriggerInteraction.Ignore);
            System.Array.Sort(allHits.ToArray(), (x, y) => x.distance.CompareTo(y.distance));
            Debug.DrawRay(raycastStartPosition, controller.rootBone.up * -1 * controller.physics.bodyVerticalOffset * 3, Color.red, Time.deltaTime);

            bool bHit = false;

            bool frontHit = false;
            Vector3 forwardHitStartPos = raycastStartPosition;
            forwardHitStartPos += controller.rootBone.up * controller.physics.bodyVerticalOffset;
            RaycastHit[] forwardHits = Physics.RaycastAll(forwardHitStartPos, controller.rootBone.forward, 3, -1, QueryTriggerInteraction.Ignore);
            System.Array.Sort(forwardHits.ToArray(), (x, y) => x.distance.CompareTo(y.distance));
            Debug.DrawRay(forwardHitStartPos, controller.rootBone.forward * 3, Color.yellow, Time.deltaTime * 5);

            foreach (RaycastHit hit in forwardHits)
            {
                if (hit.transform.gameObject == controller.rootBone.gameObject) { continue; }

                frontHit = true;
                bHit = true;
                Debug.DrawRay(hit.point, hit.normal, Color.green, Time.deltaTime * 5);

                if (Vector3.Distance(newPosition, hit.point) > controller.stepDistance & permissionToMove)
                {
                    lerpProgress = 0;
                    newPosition = hit.point;
                    raycastHit = hit;
                }

                break;
            }

            if (!frontHit)
            {
                foreach (RaycastHit hit in allHits)
                {
                    // If we raycast anything that isn't this object go to the next hit
                    if (hit.transform.gameObject == controller.rootBone.gameObject) { continue; }

                    bHit = true;

                    if (Vector3.Distance(newPosition, hit.point) > controller.stepDistance & permissionToMove)
                    {
                        lerpProgress = 0;
                        newPosition = hit.point;
                        raycastHit = hit;
                    }

                    break;
                }
            }

            // If we didn't hit anything in the previous for loop
            if (!bHit)
            {
                lerpProgress = 1;
                currentPosition = controller.rootBone.position + (controller.rootBone.right * rightAxisFootSpacing) + (controller.rootBone.forward * forwardAxisFootSpacing) + (controller.rootBone.up * -3);
                newPosition = currentPosition;
                oldPosition = currentPosition;
                raycastHit = new RaycastHit();
            }

            if (lerpProgress < 1)
            {
                // Scale lerp speed with how fast we are moving
                float velocityAverage = controller.physics.velocity.magnitude * controller.lerpSpeedMultiplier;
                float angularVelocityAverage = controller.physics.angularVelocity.magnitude * controller.angularLerpSpeedMultiplier;
                
                if (IsOwner)
                {
                    lerpSpeed.Value = velocityAverage + angularVelocityAverage;
                    if (lerpSpeed.Value < controller.minimumLerpSpeed)
                    {
                        lerpSpeed.Value = controller.minimumLerpSpeed;
                    }
                }

                Vector3 interpolatedPosition = Vector3.Lerp(oldPosition, newPosition, lerpProgress);
                interpolatedPosition += controller.physics.transform.up * Mathf.Sin(lerpProgress * Mathf.PI) * controller.stepHeight;

                currentPosition = interpolatedPosition;

                lerpProgress += Time.deltaTime * lerpSpeed.Value;
            }
            else
            {
                oldPosition = newPosition;
            }

            transform.up = raycastHit.normal;
        }

        public bool IsMoving()
        {
            return lerpProgress < 1;
        }
    }
}
