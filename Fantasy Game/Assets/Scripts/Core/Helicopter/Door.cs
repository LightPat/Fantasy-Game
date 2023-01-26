using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class Door : NetworkBehaviour
    {
        public string parameterName;
        public float automaticDoorCloseDelay = 5;

        private NetworkVariable<bool> doorOpen = new NetworkVariable<bool>();

        Animator animator;
        float lastDoorOpenTime;

        [ServerRpc(RequireOwnership = false)]
        public void ToggleDoorServerRpc()
        {
            doorOpen.Value = !doorOpen.Value;
        }

        public override void OnNetworkSpawn()
        {
            doorOpen.OnValueChanged += OnDoorOpenChange;
        }

        public override void OnNetworkDespawn()
        {
            doorOpen.OnValueChanged -= OnDoorOpenChange;
        }

        void OnDoorOpenChange(bool previous, bool current)
        {
            animator.SetBool(parameterName, current);
            if (current)
                lastDoorOpenTime = Time.time;
        }

        private void Start()
        {
            animator = GetComponentInParent<Animator>();
        }

        private void Update()
        {
            if (!IsServer) { return; }

            if (doorOpen.Value)
                if (Time.time - lastDoorOpenTime > automaticDoorCloseDelay)
                    doorOpen.Value = false;
        }
    }
}