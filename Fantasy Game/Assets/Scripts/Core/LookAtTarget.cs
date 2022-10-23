using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtTarget : MonoBehaviour
{
    public Transform aimTarget;

    void Update()
    {
        transform.LookAt(aimTarget);
    }
}
