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

        public float drag;
        private void OnAnimatorMove()
        {
            Vector3 newVelocity = Vector3.MoveTowards(rb.velocity, animator.velocity, drag);
            newVelocity.y = rb.velocity.y;
            rb.velocity = newVelocity;
        }

        //private void OnAnimatorMove()
        //{
        //    transform.parent.position += animator.deltaPosition;
        //    transform.parent.rotation *= animator.deltaRotation;
        //}
    }
}
