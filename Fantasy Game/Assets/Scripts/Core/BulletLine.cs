using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class BulletLine : MonoBehaviour
    {
        LineRenderer lineRenderer;

        private void Start()
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        private void Update()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit))
            {
                if (hit.transform.GetComponent<Attributes>())
                {
                    lineRenderer.material.color = new Color(255, 0, 0);
                    lineRenderer.SetPosition(0, transform.position);
                    lineRenderer.SetPosition(1, hit.point);
                }
                else
                {
                    lineRenderer.material.color = new Color(0, 255, 0);
                    lineRenderer.SetPosition(0, transform.position);
                    lineRenderer.SetPosition(1, hit.point);
                }
            }
            else
            {
                lineRenderer.material.color = new Color(0, 255, 0);
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, transform.position + transform.forward * 50);
            }
        }
    }
}