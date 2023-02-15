using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace LightPat.ProceduralAnimations.Spider
{
    public class SpiderPhysics : NetworkBehaviour
    {
        public float gravitySpeed;
        public float bodyVerticalOffset;
        public float landingTime;
        public float airborneRotateSpeed;
        [Header("Info - Do Not Edit These")]
        public bool airborne = true;
        public bool landing;
        public Vector3 velocity;
        public Vector3 angularVelocity;

        private Animator animator;
        private SpiderLegsController legController;

        private Vector3 bodyRestingPosition;
        private Vector3 lastPos;
        private Vector3 lastRot;

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            legController = GetComponentInChildren<SpiderLegsController>();
        }

        public bool forward;
        public float speed;

        private void Update()
        {
            if (!IsOwner) { return; }

            if (forward)
                transform.position += transform.forward * speed;

            velocity = (transform.position - lastPos) / Time.deltaTime;
            angularVelocity = (transform.rotation.eulerAngles - lastRot) / Time.deltaTime;

            animator.SetBool("airborne", airborne);
            animator.SetBool("landing", landing);

            // Orient x rotation based on leg positions to allow spider to traverse ramps
            List<RaycastHit> legHits = new List<RaycastHit>();
            List<Transform> legTransforms = new List<Transform>();
            foreach (SpiderLegIKSolver leg in legController.legSet1)
            {
                if (leg.bHit)
                    legHits.Add(leg.raycastHit);
                legTransforms.Add(leg.transform);
            }
            foreach (SpiderLegIKSolver leg in legController.legSet2)
            {
                if (leg.bHit)
                    legHits.Add(leg.raycastHit);
                legTransforms.Add(leg.transform);
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
            {
                bodyRestingPosition -= Physics.gravity * Time.deltaTime;
            }

            float[] normalAngles = new float[legHits.Count];
            List<float> dotProducts = new List<float>();
            for (int i = 0; i < legHits.Count; i++)
            {
                normalAngles[i] = Vector3.Angle(legHits[i].normal, Vector3.up);

                float dotProduct = Vector3.Dot(transform.up, legTransforms[i].up);
                if (dotProduct != 0)
                    dotProducts.Add(dotProduct);
                Debug.DrawRay(legHits[i].point, legHits[i].normal, Color.black, Time.deltaTime * 5);
            }

            //float yRot = Vector3.SignedAngle(transform.right, Vector3.right, transform.up);

            if (rotate)
            {
                if (normalAngles.Length != 0)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(-normalAngles.Average(), 0, 0), Time.deltaTime * 8);
                else
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, 0), Time.deltaTime * 8);
            }

            if (restingPosition)
            {
                if (dotProducts.Count > 0)
                    bodyRestingPosition += (1 - dotProducts.Average()) * 2 * bodyVerticalOffset * transform.up; // scale the multiplier with velocity
                transform.position = Vector3.MoveTowards(transform.position, bodyRestingPosition, Time.deltaTime * 8);
            }
        }

        public bool restingPosition;
        public bool rotate;
        private void LateUpdate()
        {
            lastPos = transform.position;
            lastRot = transform.eulerAngles;
        }
    }
}
