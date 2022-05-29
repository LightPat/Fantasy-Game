using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

namespace LightPat.FriendlyAI
{
    public class Guard : Friendly
    {
        [SerializeField]
        private Transform target;
        public float walkSpeed = 2f;
        public float chaseSpeed = 4f;
        private Rigidbody rb;
        private AudioSource audioSrc;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            audioSrc = GetComponent<AudioSource>();
        }

        private void FixedUpdate()
        {
            if (target == null)
            {
                Vector3 moveForce = transform.forward * walkSpeed;
                moveForce.x -= rb.velocity.x;
                moveForce.z -= rb.velocity.z;
                rb.AddForce(moveForce, ForceMode.VelocityChange);

                if (!audioSrc.isPlaying & rb.velocity.magnitude > walkSpeed - 1)
                {
                    StartCoroutine(playFootstep());
                }
            }
        }

        public float footstepDetectionRadius = 10f;
        private IEnumerator playFootstep()
        {
            audioSrc.Play();
            Collider[] colliders = Physics.OverlapSphere(transform.position, footstepDetectionRadius);
            foreach (Collider c in colliders)
            {
                c.SendMessageUpwards("OnFootstep", transform.position, SendMessageOptions.DontRequireReceiver);
            }
            yield return new WaitForSeconds(.3f);
            audioSrc.Pause();
        }

        void OnAttacked(GameObject value)
        {
            Debug.Log(value.transform);

        }
    }
}
