using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public abstract class Enemy : NetworkBehaviour
    {
        protected enum fightingState
        {
            stationary,
            roaming,
            combat,
            moveToTarget
        }

        protected virtual void OnDeath()
        {
            NetworkObject.Despawn(true);
        }
    }
}
