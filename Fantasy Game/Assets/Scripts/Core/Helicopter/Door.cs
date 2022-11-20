using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Door : MonoBehaviour
    {
        public string parameterName;
        public float automaticDoorCloseDelay = 5;

        Animator animator;
        float lastDoorOpenTime;

        public void ToggleDoor()
        {
            animator.SetBool(parameterName, !animator.GetBool(parameterName));
            if (animator.GetBool(parameterName))
                lastDoorOpenTime = Time.time;
        }

        private void Start()
        {
            animator = GetComponentInParent<Animator>();
        }

        private void Update()
        {
            if (animator.GetBool(parameterName))
                if (Time.time - lastDoorOpenTime > automaticDoorCloseDelay)
                    animator.SetBool(parameterName, !animator.GetBool(parameterName));
        }
    }
}