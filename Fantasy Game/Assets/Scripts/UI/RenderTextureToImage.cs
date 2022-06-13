using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.UI
{
    public class RenderTextureToImage : MonoBehaviour
    {
        public RenderTexture renderTexture;

        private void Start()
        {
            StartCoroutine(saveImage());
        }

        private IEnumerator saveImage()
        {
            yield return new WaitForSeconds(2f);
            Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            RenderTexture.active = renderTexture;
            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex.Apply();

            byte[] bytes;
            bytes = tex.EncodeToPNG();

            System.IO.File.WriteAllBytes("C:/Users/patse/Desktop/Render-Texture-Output.png", bytes);
        }
    }
}
