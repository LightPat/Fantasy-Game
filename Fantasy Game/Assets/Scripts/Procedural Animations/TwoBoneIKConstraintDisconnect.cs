using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace LightPat.ProceduralAnimations
{
    public class TwoBoneIKConstraintDisconnect : MonoBehaviour
    {
        public float disconnectDistance = 1000;
        public bool lerpWeight = true;
        public float weightSpeed = 5;
        public bool debugDistance;

        Animator animator;
        TwoBoneIKConstraint thisConstraint;
        TwoBoneIKConstraint[] otherTwoBoneConstraints;
        MultiAimConstraint[] otherAimConstraints;

        private void Start()
        {
            animator = GetComponentInParent<Animator>();
            thisConstraint = GetComponent<TwoBoneIKConstraint>();
            otherTwoBoneConstraints = transform.parent.GetComponentsInChildren<TwoBoneIKConstraint>();
            otherAimConstraints = transform.parent.GetComponentsInChildren<MultiAimConstraint>();
        }

        private void Update()
        {
            if (debugDistance)
                Debug.Log(Vector3.Distance(thisConstraint.data.target.position, thisConstraint.data.root.position));

            float weightTarget = thisConstraint.weight;

            if (Vector3.Distance(thisConstraint.data.target.position, thisConstraint.data.root.position) > disconnectDistance)
            {
                weightTarget = 0;
            }
            else
            {
                weightTarget = 1;
            }

            if (lerpWeight)
            {
                if (Mathf.Abs(weightTarget - thisConstraint.weight) > 0.1)
                {
                    float currentWeight = Mathf.Lerp(thisConstraint.weight, weightTarget, Time.deltaTime * weightSpeed * animator.speed);

                    thisConstraint.weight = currentWeight;
                    foreach (TwoBoneIKConstraint c in otherTwoBoneConstraints)
                    {
                        c.weight = currentWeight;
                    }

                    foreach (MultiAimConstraint c in otherAimConstraints)
                    {
                        c.weight = currentWeight;
                    }
                }
                else
                {
                    float currentWeight = Mathf.MoveTowards(thisConstraint.weight, weightTarget, Time.deltaTime * animator.speed);

                    thisConstraint.weight = currentWeight;
                    foreach (TwoBoneIKConstraint c in otherTwoBoneConstraints)
                    {
                        c.weight = currentWeight;
                    }

                    foreach (MultiAimConstraint c in otherAimConstraints)
                    {
                        c.weight = currentWeight;
                    }
                }
            }
            else
            {
                thisConstraint.weight = weightTarget;
                foreach (TwoBoneIKConstraint c in otherTwoBoneConstraints)
                {
                    c.weight = weightTarget;
                }

                foreach (MultiAimConstraint c in otherAimConstraints)
                {
                    c.weight = weightTarget;
                }
            }            
        }
    }
}