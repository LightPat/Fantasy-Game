using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Util
{
    public static class Utilities : object
    {
        public static IEnumerator ResetAnimatorBoolAfter1Frame(Animator animator, string parameterName)
        {
            animator.SetBool(parameterName, true);
            yield return null;
            animator.SetBool(parameterName, false);
        }

        public static IEnumerator DestroyAfterSeconds(GameObject gameObject, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Object.Destroy(gameObject);
        }
    }
}