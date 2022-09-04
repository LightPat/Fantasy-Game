using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace LightPat.Util
{
    public class RigWeightTarget : MonoBehaviour
    {
        public float weightTarget;
        public float weightSpeed;
        public bool instantWeight;
        Rig rig;
        Animator animator;

        private void Start()
        {
            rig = GetComponent<Rig>();
            animator = GetComponentInParent<Animator>();
        }

        private void Update()
        {
            if (rig.weight == weightTarget) { return; }
            if (instantWeight) { rig.weight = weightTarget; return; }

            if (Mathf.Abs(weightTarget - rig.weight) > 0.1)
            {
                rig.weight = Mathf.Lerp(rig.weight, weightTarget, Time.deltaTime * weightSpeed * animator.speed);
            }
            else
            {
                rig.weight = Mathf.MoveTowards(rig.weight, weightTarget, Time.deltaTime);
            }
        }
    }
}
