using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat
{
    public class ApplyRootMotionToParent : MonoBehaviour
    {
        Animator animator;

        private void Start()
        {
            animator = GetComponent<Animator>();
        }

        private void OnAnimatorMove()
        {
            transform.parent.position += animator.deltaPosition;
            transform.parent.rotation *= animator.deltaRotation;
        }
    }
}
