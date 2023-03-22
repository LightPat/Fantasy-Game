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
        public float rightAxisFootSpacing;
        public float forwardAxisFootSpacing;
        public SpiderLegIKSolver correspondingLegTarget;

        public bool invertDir;

        [HideInInspector] public SpiderLegsController controller;
        [HideInInspector] public bool permissionToMove = true;

        public RaycastHit raycastHit { get; private set; }
        public bool bHit { get; private set; }

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

        bool prevForwardHit;
        private void Update()
        {
            transform.position = currentPosition;

            Vector3 raycastStartPosition = controller.rootBone.position + (controller.rootBone.right * rightAxisFootSpacing) + (controller.rootBone.forward * (forwardAxisFootSpacing - 1));
            RaycastHit[] allHits = Physics.RaycastAll(raycastStartPosition, controller.rootBone.up * -1, controller.physics.bodyVerticalOffset * 3, -1, QueryTriggerInteraction.Ignore);
            System.Array.Sort(allHits.ToArray(), (x, y) => x.distance.CompareTo(y.distance));

            bHit = false;

            bool frontHit = false;
            
            Vector3 forwardHitStartPos = raycastStartPosition;
            forwardHitStartPos += controller.rootBone.up;
            
            float forwardHitDistance = 3;
            Vector3 dir = controller.physics.velocity.normalized;
            if (invertDir)
            {
                if (Vector3.Distance(dir, controller.rootBone.forward) < 0.1f)
                {
                    dir *= -1;
                    forwardHitDistance = 1;
                }
            }

            if (prevForwardHit)
            {
                forwardHitDistance *= 2;
                forwardHitStartPos -= dir;
            }

            if (controller.physics.velocity.magnitude < 0.001f)
                forwardHitDistance = 0;

            RaycastHit[] forwardHits = Physics.RaycastAll(forwardHitStartPos, dir, forwardHitDistance, -1, QueryTriggerInteraction.Ignore);
            System.Array.Sort(forwardHits.ToArray(), (x, y) => x.distance.CompareTo(y.distance));

            foreach (RaycastHit hit in forwardHits)
            {
                if (hit.transform.gameObject == controller.rootBone.gameObject) { continue; }
                if (hit.rigidbody) { continue; }

                frontHit = true;
                bHit = true;

                if (Vector3.Distance(newPosition, hit.point) > controller.stepDistance & permissionToMove)
                {
                    lerpProgress = 0;
                    newPosition = hit.point;
                    raycastHit = hit;
                    controller.PlayFootstep(this);
                }

                break;
            }

            prevForwardHit = frontHit;
            if (!frontHit)
            {
                foreach (RaycastHit hit in allHits)
                {
                    if (hit.rigidbody) { continue; }
                    // If we raycast anything that isn't this object go to the next hit
                    if (hit.transform.gameObject == controller.rootBone.gameObject) { continue; }

                    bHit = true;

                    if (Vector3.Distance(newPosition, hit.point) > controller.stepDistance & permissionToMove)
                    {
                        lerpProgress = 0;
                        newPosition = hit.point;
                        raycastHit = hit;
                        controller.PlayFootstep(this);
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

        //private void OnDrawGizmos()
        //{
        //    Gizmos.color = Color.green;
        //    Gizmos.DrawSphere(newPosition, 0.5f);
        //}
    }
}
