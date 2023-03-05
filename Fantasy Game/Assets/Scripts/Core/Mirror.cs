using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Mirror : MonoBehaviour
    {
        Camera thisCamera;

        private void Start()
        {
            thisCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            int sqr = 256;

            RenderTexture tex = new RenderTexture(sqr, sqr, 0);
            thisCamera.targetTexture = tex;

            thisCamera.Render();

            RenderTexture.active = tex;
            Texture2D photo = new Texture2D(sqr, sqr);
            photo.ReadPixels(new Rect(0, 0, sqr, sqr), 0, 0);
            photo.Apply();
            GetComponentInParent<Renderer>().material.SetTexture("inputTexture", photo);

            RenderTexture.active = null;
        }
    }
}