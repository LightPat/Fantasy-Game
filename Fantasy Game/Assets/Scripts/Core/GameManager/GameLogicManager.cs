using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class GameLogicManager : NetworkBehaviour
    {
        public TeamSpawnPoint[] spawnPoints;

    }

    [System.Serializable]
    public class TeamSpawnPoint
    {
        public Team team;
        public Vector3 spawnPosition;
        public Vector3 spawnRotation;
    }

    public enum Team
    {
        Environment,
        Red,
        Blue
    }

    public enum GameMode
    {
        CaptureTheFlag,
        HordeMode,
        GhostInTheGraveyard
    }
}