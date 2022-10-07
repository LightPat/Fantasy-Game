using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core.Player
{
    public class RootMotionManager : MonoBehaviour
    {
        Rigidbody rb;
        Animator animator;
        private void Start()
        {
            rb = GetComponentInParent<Rigidbody>();
            animator = GetComponent<Animator>();
        }

        public bool disable;
        public float drag;
        private void OnAnimatorMove()
        {
            if (disable) { return; }

            Vector3 newVelocity = Vector3.MoveTowards(rb.velocity * Time.timeScale, animator.velocity, drag);
            newVelocity.y = rb.velocity.y * Time.timeScale;
            rb.velocity = newVelocity / Time.timeScale;
        }

        //private void OnAnimatorMove()
        //{
        //    transform.parent.position += animator.deltaPosition;
        //    transform.parent.rotation *= animator.deltaRotation;
        //}
    }
}
