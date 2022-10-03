using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Util
{
    public class FollowTarget : MonoBehaviour
    {
        public Transform target;
        public bool move = true;
        public bool rotate = true;
        public bool lerpMove;

        private void Update()
        {
            if (target)
            {
                if (lerpMove)
                {
                    if (move) { transform.position = Vector3.Lerp(transform.position, target.position, 5 * Time.deltaTime); }
                }
                else
                {
                    if (move) { transform.position = target.position; }
                }

                if (rotate) { transform.rotation = target.rotation; }
            }
        }
    }
}
