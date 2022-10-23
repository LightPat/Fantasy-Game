using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat
{
    public class LineDebug : MonoBehaviour
    {
        void Update()
        {
            Debug.DrawRay(transform.position, transform.forward * 50, Color.white, Time.deltaTime);
        }
    }
}
