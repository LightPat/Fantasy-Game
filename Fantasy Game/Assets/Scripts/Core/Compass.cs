using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Compass : MonoBehaviour
    {
        public GUIStyle style = new GUIStyle();

        private void OnGUI()
        {
            Vector3 point = Camera.main.transform.forward;

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
