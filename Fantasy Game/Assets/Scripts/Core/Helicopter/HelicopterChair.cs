using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class HelicopterChair : MonoBehaviour
    {
        [Header("Sitting Down")]
        public Vector3 occupantPosition;
        public Vector3 occupantRotation;
        [Header("Exitting Chair")]
        public Vector3 exitPosOffset;

        Transform occupant;

        public bool TrySitting(Transform newOccupant)
        {
            if (occupant) { return false; }

            newOccupant.SetParent(transform, true);
            occupant = newOccupant;
            return true;
        }

        public bool ExitSitting()
        {
            occupant.SetParent(null, true);
            occupant.Translate(transform.rotation * exitPosOffset, Space.World);
            occupant = null;
            return false;
        }

        private void Update()
        {
            if (occupant)
            {
                occupant.localPosition = occupantPosition;
                occupant.localRotation = Quaternion.Euler(occupantRotation);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + Quaternion.Euler(occupantRotation) * occupantPosition, 0.2f);
        }
    }
}