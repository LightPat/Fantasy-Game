using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class Weapon : MonoBehaviour
    {
        public sbyte[] idealPersonality;
        public int baseDamage;
        //private Transform fakeParent = null;
        private Vector3 _positionOffset;
        private Quaternion _rotationOffset;

        public Transform fakeParent = null;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;

        private void Update()
        {
            if (fakeParent != null)
            {
                gizmoPoint = fakeParent.position + transform.Find("ref_right_hand_grip").localPosition;
                //transform.position = fakeParent.position + positionOffset;

                //Vector3 targetPos = fakeParent.position - _positionOffset;
                //Quaternion targetRot = fakeParent.localRotation * _rotationOffset;

                //transform.position = RotatePointAroundPivot(targetPos, fakeParent.position, targetRot);
                //transform.localRotation = fakeParent.localRotation;
            }
        }

        public void SetFakeParent(Transform newParent)
        {
            fakeParent = newParent;
            //Offset vector
            _positionOffset = newParent.position - transform.position;
            //Offset rotation
            _rotationOffset = Quaternion.Inverse(newParent.localRotation * transform.localRotation);
        }

        public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            //Get a direction from the pivot to the point
            Vector3 dir = point - pivot;
            //Rotate vector around pivot
            dir = rotation * dir;
            //Calc the rotated vector
            point = dir + pivot;
            //Return calculated vector
            return point;
        }

        private Vector3 gizmoPoint;
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(gizmoPoint, 0.1f);
        }
    }
}
