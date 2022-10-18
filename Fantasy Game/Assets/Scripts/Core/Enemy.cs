using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public abstract class Enemy : MonoBehaviour
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
