using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightPat.Core
{
    public class ServerCamera : MonoBehaviour
    {
        public float moveSpeed = 1;
        public float sensitivity = 0.1f;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            if (pauseEnabled) { return; }

            transform.Translate(new Vector3(moveInput.x, 0, moveInput.y) * moveSpeed);
        }

        Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        void OnLook(InputValue value)
        {
            if (pauseEnabled) { return; }
            Vector2 lookInput = value.Get<Vector2>() * sensitivity;
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x - lookInput.y, transform.localEulerAngles.y + lookInput.x, transform.localEulerAngles.z);
        }

        public GameObject pausePrefab;
        GameObject pauseObject;
        bool pauseEnabled;
        void OnPause()
        {
            pauseEnabled = !pauseEnabled;
            if (pauseEnabled)
            {
                Cursor.lockState = CursorLockMode.None;
                pauseObject = Instantiate(pausePrefab, transform);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                pauseObject.GetComponent<Menu>().DestroyAllMenus();
                Destroy(pauseObject);
            }
        }
    }
}