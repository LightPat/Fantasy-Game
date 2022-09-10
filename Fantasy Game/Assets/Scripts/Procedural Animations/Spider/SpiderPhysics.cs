using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations.Spider
{
    public class SpiderPhysics : MonoBehaviour
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

        private Rigidbody rb;
        private bool landingCoroutineRunning;
        private float landingDipOffset;
        private float landingDipSpeed;
        private Vector3 landingDip;
        private Vector3 bodyRestingPosition;
        private Vector3 lastPos;
        private Vector3 lastRot;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            checkDistance = bodyVerticalOffset + 0.1f;
        }

        private void FixedUpdate()
        {
            velocity = (transform.position - lastPos) / Time.fixedDeltaTime;
            angularVelocity = (transform.rotation.eulerAngles - lastRot) / Time.fixedDeltaTime;

            // If we are just now hitting the ground, start landing
            if (airborne != !IsGrounded())
            {
                landing = true;
            }

            if (landing & !landingCoroutineRunning)
            {
                StartCoroutine(Land());
            }

            // Adding artificial gravity since we have a kinematic rigidbody
            if (airborne)
            {
                rb.MovePosition(rb.position + (Time.fixedDeltaTime * gravitySpeed * Vector3.down));
            }
            else
            {
                // If we are on the ground, return the body to its resting position
                RaycastHit[] allHits = Physics.RaycastAll(new Vector3(rb.position.x, rb.position.y + 0.1f, rb.position.z), transform.up * -1, checkDistance);
                System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
                foreach (RaycastHit hit in allHits)
                {
                    // If we raycast this object, skip it
                    if (hit.transform.gameObject == gameObject)
                    {
                        continue;
                    }

                    // If we are not at resting position, move up to it
                    if (!landingCoroutineRunning & hit.distance < bodyVerticalOffset)
                    {
                        bodyRestingPosition = new Vector3(rb.position.x, hit.point.y + bodyVerticalOffset, rb.position.z);
                        rb.MovePosition(Vector3.Lerp(rb.position, bodyRestingPosition, Time.fixedDeltaTime * 1));
                    }
                    break;
                }
            }

            // This creates a dip in the body's position so that when it hits the ground it looks like some downward force was applied to it
            if (landingCoroutineRunning)
            {
                rb.MovePosition(Vector3.Lerp(rb.position, landingDip, Time.fixedDeltaTime * landingDipSpeed));
            }

            // Slowly self-right the spider
            //rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.Euler(0, rb.rotation.eulerAngles.y, 0), Time.fixedDeltaTime * airborneRotateSpeed));

            airborne = !IsGrounded();
            lastPos = transform.position;
            lastRot = transform.rotation.eulerAngles;
        }

        private IEnumerator Land()
        {
            landingCoroutineRunning = true;
            landingDip = transform.position;
            // Add the negative vertical velocity
            landingDipOffset = velocity.y;
            // Set the speed to the impact velocity before giving it a floor so that our dip speed scales with our impact velocity
            landingDipSpeed = Mathf.Abs(landingDipOffset);
            // Need to make sure it not too big so that we don't clip through the floor
            if (landingDipOffset < bodyVerticalOffset * -0.5f)
            {
                landingDipOffset = bodyVerticalOffset * -0.5f;
            }
            // Assign the target position for our dip, so that we can lerp to it in Update()
            landingDip.y += landingDipOffset;
            // Wait while landing logic completes
            yield return new WaitForSeconds(landingTime);
            // Reset variables so that we can activate landing again
            landing = false;
            landingCoroutineRunning = false;
        }

        [HideInInspector] public float checkDistance;
        private bool IsGrounded()
        {
            RaycastHit[] allHits = Physics.RaycastAll(new Vector3(transform.position.x, transform.position.y + 0.1f, transform.position.z), transform.up * -1, checkDistance);

            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform.gameObject == gameObject)
                {
                    continue;
                }
                return true;
            }

            return false;
        }
    }
}
