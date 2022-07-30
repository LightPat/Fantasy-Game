using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightPat.ProceduralAnimations
{
    public class ProceduralAnimationController : MonoBehaviour
    {
        public bool move;
        public float moveSpeed = 0.05f;
        public Vector3 moveVector;

        void Update()
        {
            if (move)
            {
                transform.Translate(moveVector * moveSpeed);
            }
        }

        private Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }
    }
}
