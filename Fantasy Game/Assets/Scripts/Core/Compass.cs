using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Compass : MonoBehaviour
    {
        public float fpsUpdateInterval = 0.5f;
        public GUIStyle style = new GUIStyle();

        float accum = 0.0f;
        int frames = 0;
        float timeleft;
        float fps;

        private void Start()
        {
            timeleft = fpsUpdateInterval;
        }

        private void Update()
        {
            timeleft -= Time.deltaTime;
            accum += Time.timeScale / Time.deltaTime;
            ++frames;

            // Interval ended - update GUI text and start new interval
            if (timeleft <= 0.0)
            {
                // display two fractional digits (f2 format)
                fps = (accum / frames);
                timeleft = fpsUpdateInterval;
                accum = 0.0f;
                frames = 0;
            }
        }

        private void OnGUI()
        {
            Vector3 point = Camera.main.transform.forward;

            GUI.Label(new Rect(1600,10,100,25), fps.ToString("F2") + " FPS", style);

            if (point.x >= 0)
            {
                if (point.z >= 0)
                {
                    GUI.Label(new Rect(10, 10, 100, 20), "NorthEast", style);
                }
                else // z < 0
                {
                    GUI.Label(new Rect(10, 10, 100, 20), "SouthEast", style);
                }
            }
            else // x < 0
            {
                if (point.z >= 0)
                {
                    GUI.Label(new Rect(10, 10, 100, 20), "NorthWest", style);
                }
                else // z < 0
                {
                    GUI.Label(new Rect(10, 10, 100, 20), "SouthWest", style);
                }
            }
        }
    }
}
