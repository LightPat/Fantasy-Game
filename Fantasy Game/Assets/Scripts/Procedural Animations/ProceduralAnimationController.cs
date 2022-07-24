using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightPat.ProceduralAnimations
{
    public class ProceduralAnimationController : MonoBehaviour
    {
        public float moveSpeed = 0.05f;

        void Update()
        {
            transform.Translate(new Vector3(moveInput.x, 0, moveInput.y) * moveSpeed);
        }

        private Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }
    }
}
