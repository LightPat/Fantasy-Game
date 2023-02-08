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

        public bool updatePos;
        private void Update()
        {
            if (updatePos) { return; }

            if (Vector3.Distance(bodyRestingPosition, transform.position) < 0.1f)
            {
                transform.Translate(transform.forward * 0.03f, Space.World);
            }
        }

        private void LateUpdate()
        {
            if (!IsOwner) { return; }

            velocity = (transform.position - lastPos) / Time.deltaTime;
            angularVelocity = (transform.rotation.eulerAngles - lastRot) / Time.deltaTime;

            RaycastHit[] allHits = Physics.RaycastAll(new Vector3(transform.position.x, transform.position.y + 0.1f, transform.position.z), transform.up * -1);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
            foreach (RaycastHit hit in allHits)
            {
                // If we raycast this object, skip it
                if (hit.transform.gameObject == gameObject) { continue; }

                // If we are not at resting position, move to it
                // hit.distance < bodyVerticalOffset

                bodyRestingPosition = new Vector3(transform.position.x, hit.point.y + bodyVerticalOffset, transform.position.z);
                break;
            }
            bodyRestingPosition = new Vector3(transform.position.x, bodyRestingPosition.y, transform.position.z);

            animator.SetBool("airborne", airborne);
            animator.SetBool("landing", landing);

            // Orient x rotation based on leg positions to allow spider to traverse ramps
            List<RaycastHit> legHits = new List<RaycastHit>();
            foreach (var leg in legController.legSet1)
            {
                RaycastHit hit;
                bool bHit = Physics.Raycast(leg.transform.position, transform.up * -1, out hit);
                if (bHit)
                    legHits.Add(hit);
            }
            foreach (var leg in legController.legSet2)
            {
                RaycastHit hit;
                bool bHit = Physics.Raycast(leg.transform.position, transform.up * -1, out hit);
                if (bHit)
                    legHits.Add(hit);
            }

            float[] normalAngles = new float[legHits.Count];
            for (int i = 0; i < legHits.Count; i++)
            {
                normalAngles[i] = Vector3.Angle(legHits[i].normal, Vector3.up);
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(-normalAngles.Average(), transform.eulerAngles.y, transform.eulerAngles.z), Time.deltaTime * Mathf.Clamp(velocity.magnitude, 1, 1000));
            transform.position = Vector3.MoveTowards(transform.position, bodyRestingPosition, Time.deltaTime * 8);

            lastPos = transform.position;
            lastRot = transform.eulerAngles;
        }
    }
}
