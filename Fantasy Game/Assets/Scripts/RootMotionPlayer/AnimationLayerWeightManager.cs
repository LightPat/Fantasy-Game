using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class AnimationLayerWeightManager : MonoBehaviour
    {
        public float transitionSpeed;

        float[] layerWeightTargets;
        Animator animator;

        public void SetLayerWeight(string layerName, float targetWeight)
        {
            layerWeightTargets[animator.GetLayerIndex(layerName)] = targetWeight;
        }

        public void SetLayerWeight(int layerIndex, float targetWeight)
        {
            layerWeightTargets[layerIndex] = targetWeight;
        }

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            layerWeightTargets = new float[animator.layerCount];

            for (int i = 1; i < animator.layerCount; i++)
            {
                layerWeightTargets[i] = animator.GetLayerWeight(i);
            }
        }

        private void Update()
        {
            for (int i = 1; i < layerWeightTargets.Length; i++)
            {
                float weightTarget = layerWeightTargets[i];

                animator.SetLayerWeight(i, Mathf.MoveTowards(animator.GetLayerWeight(i), weightTarget, Time.deltaTime * transitionSpeed));
            }
        }
    }
}
