using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

namespace LightPat.Core.Player
{
    public class Flag : Interactable
    {
        [HideInInspector] public CaptureTheFlagBase captureTheFlagBase;
        [HideInInspector] public bool scored;

        public Team team { get; private set; }

        [SerializeField] TextMeshPro flagText;
        [SerializeField] private Renderer flagRenderer;
        [SerializeField] private Vector3 carryingLocalPos;
        [SerializeField] private Vector3 carryingLocalRot;

        HumanoidWeaponAnimationHandler carryingParent;
        Vector3 flagRestingPosition;

        public override void Invoke(GameObject invoker)
        {
            if (carryingParent) { return; }

            if (invoker.GetComponent<Attributes>().team != team)
                captureTheFlagBase.PickUpFlagServerRpc(invoker.GetComponent<NetworkObject>().NetworkObjectId);
            else if (!transform.parent)
                Destroy(gameObject);
        }

        private void OnTransformParentChanged()
        {
            carryingParent = GetComponentInParent<HumanoidWeaponAnimationHandler>();
            RefreshText();
            if (!transform.parent)
            {
                RaycastHit hit;
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
            {
                flagRenderer.material.color = Color.red;
                flagText.color = Color.red;
            }
            else if (team == Team.Blue)
            {
                flagRenderer.material.color = Color.blue;
                flagText.color = Color.blue;
            }
            
            captureTheFlagBase = newCaptureTheFlagBase;
        }

        private void Start()
        {
            RefreshText();
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

            if (Camera.main)
            {
                flagText.transform.rotation = Quaternion.LookRotation(Camera.main.transform.position - flagText.transform.position) * Quaternion.Euler(0, 180, 0);

                float minDistance = 1;
                float maxDistance = 500;
                float minScale = 1;
                float maxScale = 7;
                float scale = Mathf.Lerp(minScale, maxScale, Mathf.InverseLerp(minDistance, maxDistance, Vector3.Distance(flagText.transform.position, Camera.main.transform.position)));

                flagText.transform.localScale = new Vector3(scale, scale, scale);
            }
        }

        private void OnDestroy()
        {
            captureTheFlagBase.RefreshFlag();
        }

        private void RefreshText()
        {
            if (NetworkManager.Singleton.IsServer)
                flagText.SetText(team.ToString());

            // If the local player and flag are on the same team
            if (ClientManager.Singleton.GetClient(NetworkManager.Singleton.LocalClientId).team == team)
            {
                if (carryingParent) // parent is a player
                {
                    if (carryingParent.IsLocalPlayer)
                        flagText.SetText("");
                    else
                        flagText.SetText("Kill");
                }
                else if (transform.parent) // parent is the base
                {
                    flagText.SetText("Defend");
                }
                else // no parent
                {
                    flagText.SetText("Recover");
                }
            }
            else // If the local player and flag are not on the same team
            {
                if (carryingParent) // parent is a player
                {
                    if (carryingParent.IsLocalPlayer)
                        flagText.SetText("");
                    else
                        flagText.SetText("Protect");
                }
                else if (transform.parent) // parent is the base
                {
                    flagText.SetText("Capture");
                }
                else // no parent
                {
                    flagText.SetText("Capture");
                }
            }
        }
    }
}