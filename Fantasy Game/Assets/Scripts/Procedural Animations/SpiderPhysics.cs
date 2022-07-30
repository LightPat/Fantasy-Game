using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class SpiderPhysics : MonoBehaviour
    {
        public float gravitySpeed;
        public float bodyVerticalOffset;
        [HideInInspector] public bool airborne;
        [HideInInspector] public bool landing;
        public float landingTime;
        public float airborneRotateSpeed;

        private bool landingCoroutineRunning;
        private bool landingDipComplete;
        private Rigidbody rb;
        private Vector3 landingDip;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            checkDistance = bodyVerticalOffset + 0.1f;
        }

        private void Update()
        {
            // Slowly self-right the spider while airborne
            if (airborne)
            {
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.Euler(0, 0, rb.rotation.z), airborneRotateSpeed));
            }

            if (airborne != !IsGrounded())
            {
                landing = true;
            }

            if (landing & !landingCoroutineRunning)
            {
                StartCoroutine(Land());
            }

            // This creates a dip in the body's position so that when it hits the ground it looks like some downward force was applied to it
            if (landingCoroutineRunning)
            {
                rb.MovePosition(Vector3.Lerp(rb.position, landingDip, 0.1f));
                if (Vector3.Distance(rb.position, landingDip) < 0.01f & !landingDipComplete)
                {
                    landingDipComplete = true;
                    landingDip.y += 0.5f;
                }
            }

            airborne = !IsGrounded();
        }

        private void FixedUpdate()
        {
            // Adding artificial gravity since we have no rigidbody
            if (airborne)
            {
                rb.MovePosition(rb.position + (Time.deltaTime * gravitySpeed * Vector3.down));
            }
        }

        private IEnumerator Land()
        {
            landingCoroutineRunning = true;
            landingDipComplete = false;
            landingDip = rb.position;
            landingDip.y -= 0.5f;
            yield return new WaitForSeconds(landingTime);
            landing = false;
            landingCoroutineRunning = false;
        }

        [HideInInspector] public float checkDistance;
        private bool IsGrounded()
        {
            return Physics.Raycast(new Vector3(transform.position.x, transform.position.y + 0.1f, transform.position.z), transform.up * -1, checkDistance);
        }
    }
}
