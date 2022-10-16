using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public static class Utilities : object
    {
        public static IEnumerator ResetAnimatorBoolAfter1Frame(Animator animator, string parameterName)
        {
            animator.SetBool(parameterName, true);
            yield return null;
            animator.SetBool(parameterName, false);
        }

        public static IEnumerator DestroyAfterParticleSystemStops(ParticleSystem particleSystem)
        {
            yield return new WaitUntil(() => !particleSystem.isPlaying);
            Object.Destroy(particleSystem.gameObject);
        }
    }
}