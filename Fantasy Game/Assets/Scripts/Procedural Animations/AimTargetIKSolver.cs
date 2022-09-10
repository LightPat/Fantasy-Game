using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class AimTargetIKSolver : MonoBehaviour
    {
        private void Update()
        {
            transform.position = Camera.main.transform.position + Camera.main.transform.forward * 3;
        }
    }
}
