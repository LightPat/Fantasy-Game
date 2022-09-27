using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class AimTargetIKSolver : MonoBehaviour
    {
        public bool disableUpdate;

        private void Update()
        {
            if (disableUpdate) { return; }
            transform.position = Camera.main.transform.position + Camera.main.transform.forward * 3;
        }
    }
}
