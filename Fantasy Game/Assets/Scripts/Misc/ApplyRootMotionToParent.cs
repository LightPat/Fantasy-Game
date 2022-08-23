using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Misc
{
    public class ApplyRootMotionToParent : MonoBehaviour
    {
        private Animator animator;

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
