using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core.Player
{
    public class PlayerCameraFollow : MonoBehaviour
    {
        public PlayerController player;
        public Transform target;
        public float rotationSpeed;
        public bool UpdateRotationWithTarget;

        private void Update()
        {
            transform.position = target.position;

            if (UpdateRotationWithTarget)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target.rotation, rotationSpeed * Time.deltaTime);
                player.rotationX = 360 - transform.eulerAngles.x;
                player.rotationY = transform.eulerAngles.y;
            }
            else
            {
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
            }
        }
    }
}