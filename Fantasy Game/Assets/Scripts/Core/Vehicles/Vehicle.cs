using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public abstract class Vehicle : NetworkBehaviour
    {
        [Header("All Vehicles")]
        public Transform leftFootGrip;
        public Transform rightFootGrip;
        public Transform rightHandGrip;
        public Transform leftHandGrip;
        public float xAxisRootBoneRotation;
        public bool rotateX;
        public bool rotateY;

        public abstract void OnDriverEnter(ulong networkObjectId);
        public abstract void OnDriverExit();
        protected abstract void OnVehicleMove(Vector2 newMoveInput);
        protected abstract void OnVehicleLook(Vector2 newLookInput);
        protected abstract void OnVehicleJump(bool pressed);
        protected abstract void OnVehicleCrouch(bool pressed);
        protected abstract void OnVehicleSprint(bool pressed);
    }
}