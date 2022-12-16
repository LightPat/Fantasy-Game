using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class FollowTarget : MonoBehaviour
    {
        public Transform target;
        public bool move = true;
        public bool rotate = true;
        public bool lerp;
        public float lerpSpeed = 5;

        private void Update()
        {
            if (target)
            {
                if (lerp)
                {
                    if (move) { transform.position = Vector3.MoveTowards(transform.position, target.position, lerpSpeed * Time.deltaTime); }
                    if (rotate) { transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, lerpSpeed * Time.deltaTime); }
                }
                else
                {
                    if (move) { transform.position = target.position; }
                    if (rotate) { transform.rotation = target.rotation; }
                }
            }
        }
    }
}
