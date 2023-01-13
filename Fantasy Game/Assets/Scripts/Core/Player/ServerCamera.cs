using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightPat.Core.Player
{
    public class ServerCamera : MonoBehaviour
    {
        public float moveSpeed = 1;
        public float sensitivity = 0.1f;
        int fps;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            if (pauseEnabled) { return; }

            transform.Translate(new Vector3(moveInput.x, 0, moveInput.y) * moveSpeed);
            fps = Mathf.RoundToInt((float)1.0 / Time.deltaTime);
        }

        private void OnGUI()
        {
            // FPS Label
            GUIStyle guiStyle = new GUIStyle();
            guiStyle.fontSize = 48;
            guiStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(Screen.currentResolution.width - 100, 50, 100, 50), fps.ToString(), guiStyle);
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