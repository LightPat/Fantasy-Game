using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Sun : MonoBehaviour
    {
        public float cycleSpeed;

        void Update()
        {
            transform.Rotate(0, cycleSpeed, 0);
        }
    }
}
