using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace LightPat.ProceduralAnimations
{
    public class RigWeightTarget : MonoBehaviour
    {
        public float weightTarget;
        public float weightSpeed;
        Rig rig;

        private void Start()
        {
            rig = GetComponent<Rig>();
        }

        private void Update()
        {
            if (rig.weight == weightTarget) { return; }

            if (Mathf.Abs(weightTarget - rig.weight) > 0.1)
            {
                rig.weight = Mathf.Lerp(rig.weight, weightTarget, Time.deltaTime * weightSpeed);
            }
            else
            {
                rig.weight = weightTarget;
            }
        }
    }
}
