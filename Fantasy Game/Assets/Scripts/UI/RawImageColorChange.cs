using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace LightPat.UI
{
    public class RawImageColorChange : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Color enterColor;
        public Color exitColor;
        public float colorFadeSpeed = 0.3f;
        private RawImage rawImage;

        private void Start()
        {
            rawImage = GetComponent<RawImage>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            StartCoroutine(OnHover());
        }

        private IEnumerator OnHover()
        {
            Color currentColor = rawImage.color;

            while (rawImage.color != enterColor)
            {
                rawImage.color = Vector4.MoveTowards(currentColor, enterColor, colorFadeSpeed);
                currentColor = rawImage.color;
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForEndOfFrame();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StartCoroutine(OnHoverExit());
        }

        private IEnumerator OnHoverExit()
        {
            Color currentColor = rawImage.color;

            while (rawImage.color != exitColor)
            {
                rawImage.color = Vector4.MoveTowards(currentColor, exitColor, colorFadeSpeed);
                currentColor = rawImage.color;
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForEndOfFrame();
        }
    }
}
