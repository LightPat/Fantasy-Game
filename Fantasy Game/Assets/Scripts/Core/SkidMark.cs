using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class SkidMark : MonoBehaviour
    {
        [SerializeField] private float timeBeforeDestroy = 5;

        private float timeExisting;
        private Renderer thisRenderer;

        private void Start()
        {
            thisRenderer = GetComponent<Renderer>();
        }

        private void Update()
        {
            thisRenderer.material.SetFloat("alphaScale", Mathf.Clamp(1 - timeExisting/timeBeforeDestroy, 0, 1));

            timeExisting += Time.deltaTime;

            if (timeExisting >= timeBeforeDestroy) { Destroy(gameObject); }
        }
    }
}
