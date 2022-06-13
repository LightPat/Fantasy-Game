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
        private RawImage rawImage;

        private void Start()
        {
            rawImage = GetComponent<RawImage>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            rawImage.color = enterColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            rawImage.color = exitColor;
        }
    }
}
