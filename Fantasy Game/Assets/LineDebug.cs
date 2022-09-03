using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat
{
    public class LineDebug : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            Debug.DrawLine(transform.position, transform.position + transform.forward * 8, Color.black, Time.deltaTime);
        }
    }
}
