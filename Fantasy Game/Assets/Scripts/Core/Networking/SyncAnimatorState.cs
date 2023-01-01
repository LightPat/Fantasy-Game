using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class SyncAnimatorState : NetworkBehaviour
    {
        Animator animator;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
                NetworkManager.NetworkTickSystem.Tick += UpdateAnimator;
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
                NetworkManager.NetworkTickSystem.Tick -= UpdateAnimator;
        }

        private void UpdateAnimator()
        {

        }

        private void Start()
        {
            animator = GetComponent<Animator>();
        }
    }
}