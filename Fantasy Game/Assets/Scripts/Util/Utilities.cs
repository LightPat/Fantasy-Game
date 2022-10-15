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
    }
}