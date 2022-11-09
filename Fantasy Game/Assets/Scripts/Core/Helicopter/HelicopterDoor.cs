using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class HelicopterDoor : MonoBehaviour
    {
        public string parameterName;

        Animator animator;

        public void ToggleDoor()
        {
            animator.SetBool(parameterName, !animator.GetBool(parameterName));
        }

        private void Start()
        {
            animator = GetComponentInParent<Animator>();
        }
    }
}