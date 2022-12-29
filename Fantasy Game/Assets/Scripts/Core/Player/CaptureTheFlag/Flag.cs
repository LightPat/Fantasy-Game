using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core.Player
{
    public class Flag : Interactable
    {
        public CaptureTheFlagBase captureTheFlagBase;

        public Team team { get; private set; }

        [SerializeField] private Renderer flagRenderer;
        [SerializeField] private Vector3 carryingLocalPos;
        [SerializeField] private Vector3 carryingLocalRot;

        HumanoidWeaponAnimationHandler carryingParent;

        public override void Invoke(GameObject invoker)
        {
            captureTheFlagBase.PickUpFlagServerRpc(invoker.GetComponent<NetworkObject>().NetworkObjectId);
        }

        private void OnTransformParentChanged()
        {
            carryingParent = GetComponentInParent<HumanoidWeaponAnimationHandler>();
        }

        public void SetBase(CaptureTheFlagBase newCaptureTheFlagBase)
        {
            team = newCaptureTheFlagBase.team;

            if (team == Team.Red)
                flagRenderer.material.color = Color.red;
            else if (team == Team.Blue)
                flagRenderer.material.color = Color.blue;

            captureTheFlagBase = newCaptureTheFlagBase;
        }

        private void Update()
        {
            if (carryingParent)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, carryingLocalPos, Time.deltaTime * 8);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(carryingLocalRot), Time.deltaTime * 8);
            }
        }

        private void OnDestroy()
        {
            captureTheFlagBase.RefreshFlag();
        }
    }
}