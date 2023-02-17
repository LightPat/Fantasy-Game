using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace LightPat.ProceduralAnimations.Spider
{
    public class SpiderPhysics : NetworkBehaviour
    {
        public float bodyVerticalOffset;
        public float maxVerticalOffset;
        [Header("Extras")]
        public bool forward;
        public float speed;
        public int yRotation;

        public Vector3 velocity { get; private set; }
        public Vector3 angularVelocity { get; private set; }

        private Vector3 lastPos;
        private Vector3 lastRot;

        private SpiderLegsController legController;
        private Vector3 bodyRestingPosition;

        private void Awake()
        {
            legController = GetComponentInChildren<SpiderLegsController>();
        }

        private void Update()
        {
            if (!IsOwner) { return; }

            if (forward)
                transform.position += transform.forward * speed;

            velocity = (transform.position - lastPos) / Time.deltaTime;

            // Orient x rotation based on leg positions to allow spider to traverse ramps
            List<RaycastHit> legHits = new List<RaycastHit>();
            foreach (SpiderLegIKSolver leg in legController.legSet1)
            {
                if (leg.bHit)
                    legHits.Add(leg.raycastHit);
            }
            foreach (SpiderLegIKSolver leg in legController.legSet2)
            {
                if (leg.bHit)
                    legHits.Add(leg.raycastHit);
            }

            RaycastHit[] allHits = Physics.RaycastAll(transform.position, transform.up * -1);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
            foreach (RaycastHit hit in allHits)
            {
                // If we raycast this object, skip it
                if (hit.transform.gameObject == gameObject) { continue; }

                bodyRestingPosition = hit.point + transform.up * bodyVerticalOffset;
                break;
            }

            if (allHits.Length == 0)
                bodyRestingPosition -= Physics.gravity * Time.deltaTime;

            float yRot = Vector3.SignedAngle(transform.right, Vector3.right, transform.up * -1);
            //if (transform.up.y < 0)
            //    yRot *= -1;

            Debug.Log(yRot + " " + transform.up);

            float[] normalAngles = new float[legHits.Count];
            Quaternion[] quaternions = new Quaternion[normalAngles.Length];
            for (int i = 0; i < normalAngles.Length; i++)
            {
                normalAngles[i] = Vector3.Angle(legHits[i].normal, Vector3.up);

                if (legHits[i].normal.z <= 0)
                    normalAngles[i] *= -1;

                quaternions[i] = Quaternion.Euler(normalAngles[i], 0, 0);
            }

            List<float> dotProducts = new List<float>();
            for (int i = 0; i < legHits.Count; i++)
            {
                dotProducts.Add(Vector3.Dot(transform.up, legHits[i].normal));
            }

            if (rotate)
            {
                if (normalAngles.Length > 0)
                    transform.rotation = Quaternion.Slerp(transform.rotation, AverageQuaternion(quaternions) * Quaternion.Euler(0, yRot, 0), Time.deltaTime * 8);
                else
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, yRot, 0), Time.deltaTime * 8);
            }
            
            if (dotProducts.Count > 0)
                bodyRestingPosition += Vector3.ClampMagnitude((1 - dotProducts.Min()) * 2 * bodyVerticalOffset * transform.up, (transform.up * maxVerticalOffset).magnitude);

            if (restingPosition)
                transform.position = Vector3.MoveTowards(transform.position, bodyRestingPosition, Time.deltaTime * 8);

            angularVelocity = (transform.eulerAngles - lastRot) / Time.deltaTime;

            lastPos = transform.position;
            lastRot = transform.eulerAngles;
        }

        public bool restingPosition;
        public bool rotate;

        private Quaternion AverageQuaternion(Quaternion[] qArray)
        {
            Quaternion qAvg = qArray[0];
            float weight;
            for (int i = 1; i < qArray.Length; i++)
            {
                weight = 1.0f / (float)(i + 1);
                qAvg = Quaternion.Slerp(qAvg, qArray[i], weight);
            }
            return qAvg;
        }
    }
}
