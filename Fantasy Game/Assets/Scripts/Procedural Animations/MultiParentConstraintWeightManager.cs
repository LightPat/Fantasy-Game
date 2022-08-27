using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace LightPat.ProceduralAnimations
{
    public class MultiParentConstraintWeightManager : MonoBehaviour
    {
        public float weightSpeed;

        float[] objectWeightTargets;
        MultiParentConstraint constraint;

        public void SetObjectWeightTarget(int objectIndex, float weightTarget)
        {
            objectWeightTargets[objectIndex] = weightTarget;
        }

        private void Start()
        {
            constraint = GetComponent<MultiParentConstraint>();

            objectWeightTargets = new float[constraint.data.sourceObjects.Count];
            for (int i = 0; i < objectWeightTargets.Length; i++)
            {
                objectWeightTargets[i] = constraint.data.sourceObjects.GetWeight(i);
            }
        }

        private void Update()
        {
            WeightedTransformArray sources = constraint.data.sourceObjects;

            for (int i = 0; i < objectWeightTargets.Length; i++)
            {
                sources.SetWeight(i, Mathf.MoveTowards(sources.GetWeight(i), objectWeightTargets[i], Time.deltaTime * weightSpeed));
            }
            constraint.data.sourceObjects = sources;
        }
    }
}
