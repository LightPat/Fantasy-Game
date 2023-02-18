using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using LightPat.ProceduralAnimations.Spider;

namespace LightPat.EnemyAI
{
    public class Spider : Enemy
    {
        public List<Vector3> pathFindingPositions;
        public float moveTowardsSpeed;
        public float rotationSpeed;

        private Vector3 currentTargetPosition;
        private float verticalOffset;
        private SpiderPhysics spiderPhysics;

        private void Start()
        {
            spiderPhysics = GetComponent<SpiderPhysics>();
            verticalOffset = spiderPhysics.bodyVerticalOffset;
        }

        private void Update()
        {
            if (pathFindingPositions.Count < 1) { return; }

            bool targetPositionHit = false;
            RaycastHit[] allHits = Physics.RaycastAll(pathFindingPositions[0], transform.up * -1);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
            foreach (RaycastHit hit in allHits)
            {
                if (hit.collider.isTrigger) { continue; }
                if (hit.rigidbody) { continue; }

                currentTargetPosition = hit.point + transform.up * verticalOffset;
                targetPositionHit = true;
                break;
            }

            if (!targetPositionHit) { return; }

            currentTargetPosition += spiderPhysics.dotProductOffset;

            if (Vector3.Distance(transform.position, currentTargetPosition) > 0.1f)
                transform.position += moveTowardsSpeed * Time.deltaTime * transform.forward;
            else
                pathFindingPositions.RemoveAt(0);

            Quaternion targetRotation = Quaternion.LookRotation(currentTargetPosition - transform.position, transform.up);
            if (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(currentTargetPosition, 0.5f);

            foreach (Vector3 pos in pathFindingPositions)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(pos, 0.3f);
            }
        }
    }
}