using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Rotate : MonoBehaviour
    {
        void Update()
        {
            transform.Rotate(0,0,0.5f);
        }
    }
}
