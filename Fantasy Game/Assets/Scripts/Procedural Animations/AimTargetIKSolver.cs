using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using UnityEngine.Animations.Rigging;
using LightPat.Core.Player;

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
