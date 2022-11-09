using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class HelicopterChair : MonoBehaviour
    {
        public Vector3 occupantPosition;
        public Vector3 occupantRotation;

        private void Update()
        {
            if (transform.childCount > 0)
            {
                transform.GetChild(0).localPosition = occupantPosition;
                transform.GetChild(0).localRotation = Quaternion.Euler(occupantRotation);

                //transform.GetChild(0).localPosition = Vector3.Lerp(transform.GetChild(0).localPosition, occupantPosition, Time.deltaTime * 5);
                //transform.GetChild(0).localRotation = Quaternion.Slerp(transform.GetChild(0).localRotation, Quaternion.Euler(occupantRotation), Time.deltaTime * 5);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + Quaternion.Euler(occupantRotation) * occupantPosition, 0.2f);
        }
    }
}