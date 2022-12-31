using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core.Player
{
    public class Flag : Interactable
    {
        [HideInInspector] public CaptureTheFlagBase captureTheFlagBase;
        [HideInInspector] public bool scored;

        public Team team { get; private set; }

        [SerializeField] private Renderer flagRenderer;
        [SerializeField] private Vector3 carryingLocalPos;
        [SerializeField] private Vector3 carryingLocalRot;

        HumanoidWeaponAnimationHandler carryingParent;
        Vector3 flagRestingPosition;

        public override void Invoke(GameObject invoker)
        {
            //if (invoker.GetComponent<Attributes>().team != team)
            captureTheFlagBase.PickUpFlagServerRpc(invoker.GetComponent<NetworkObject>().NetworkObjectId);
            // TODO Add if teams are the same return flag to base
        }

        private void OnTransformParentChanged()
        {
            carryingParent = GetComponentInParent<HumanoidWeaponAnimationHandler>();
            RaycastHit hit;
            if (!transform.parent)
            {
                flagRestingPosition = Vector3.zero;
                if (Physics.Raycast(transform.position, Vector3.down, out hit, 3, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                    flagRestingPosition = hit.point;
                else
                    Destroy(gameObject);
            }
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
            else if (!transform.parent)
            {
                if (flagRestingPosition != Vector3.zero)
                    transform.position = Vector3.Lerp(transform.position, flagRestingPosition, Time.deltaTime * 8);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.identity, Time.deltaTime * 8);
            }
        }

        private void OnDestroy()
        {
            captureTheFlagBase.RefreshFlag();
        }
    }
}