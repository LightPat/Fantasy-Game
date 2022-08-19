using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class MirrorTarget : MonoBehaviour
    {
        public Transform target;
        public bool move = true;
        public bool rotate = true;

        private void Update()
        {
            if (target)
            {
                if (move) { transform.position = target.position; }
                if (rotate) { transform.rotation = target.rotation; }
            }
        }
    }
}
