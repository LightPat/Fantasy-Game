using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Util
{
    public class LineDebug : MonoBehaviour
    {
        void Update()
        {
            Debug.DrawLine(transform.position, transform.position + transform.forward * 8, Color.black, Time.deltaTime);
        }
    }
}
