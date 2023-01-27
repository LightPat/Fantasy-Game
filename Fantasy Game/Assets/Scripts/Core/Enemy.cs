using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public abstract class Enemy : NetworkBehaviour
    {
        public enum fightingState
        {
            stationary,
            roaming,
            combat,
            moveToTarget
        }
    }
}
