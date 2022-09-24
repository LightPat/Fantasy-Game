using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace LightPat.ProceduralAnimations
{
    public class TwoBoneIKConstraintDisconnect : MonoBehaviour
    {
        public float disconnectDistance;
        TwoBoneIKConstraint constraint;

        private void Start()
        {
            constraint = GetComponent<TwoBoneIKConstraint>();
        }

        private void Update()
        {
            if (Vector3.Distance(constraint.data.target.position, constraint.data.root.position) > disconnectDistance)
            {
                constraint.weight = 0;
            }
            else
            {
                constraint.weight = 1;
            }
        }
    }
}