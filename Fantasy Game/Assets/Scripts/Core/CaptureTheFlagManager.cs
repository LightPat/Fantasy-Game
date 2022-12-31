using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Netcode;

namespace LightPat.Core
{
    public class CaptureTheFlagManager : NetworkBehaviour
    {
        public TeamSpawnPoint[] spawnPoints;
        public GameObject HUDPrefab;
        public int winningScore = 3;

        private NetworkList<int> scores;
        private List<Team> teams = new List<Team>();
        private GameObject HUDInstance;

        public void IncrementScore(Team team)
        {
            if (!IsServer) { return; }

            int teamIndex = teams.IndexOf(team);
            scores[teamIndex]++;
            if (scores[teamIndex] >= winningScore)
            {
                Debug.Log(team + " has won!");
            }
        }

        private void OnListChanged(NetworkListEvent<int> changeEvent)
        {
            HUDInstance.SendMessage("OnIncrementScore", new KeyValuePair<int, int>(changeEvent.Index, changeEvent.Value));
        }

        private void Awake()
        {
            scores = new NetworkList<int>();
            foreach (Team team in System.Enum.GetValues(typeof(Team)).Cast<Team>())
            {
                teams.Add(team);
            }
            HUDInstance = Instantiate(HUDPrefab);
        }

        public override void OnNetworkSpawn()
        {
            scores.OnListChanged += OnListChanged;

            if (!IsServer) { return; }

            foreach (Team team in System.Enum.GetValues(typeof(Team)).Cast<Team>())
            {
                scores.Add(-1);
                IncrementScore(team);
            }
        }

        public override void OnNetworkDespawn()
        {
            scores.OnListChanged -= OnListChanged;
        }

        private void OnDrawGizmos()
        {
            foreach (TeamSpawnPoint spawnPoint in spawnPoints)
            {
                Gizmos.color = (Color)typeof(Color).GetProperty(spawnPoint.team.ToString().ToLowerInvariant()).GetValue(null, null);
                Gizmos.DrawWireSphere(spawnPoint.spawnPosition, 2);
                Gizmos.DrawRay(spawnPoint.spawnPosition, Quaternion.Euler(spawnPoint.spawnRotation) * Vector3.forward * 5);
            }
        }
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