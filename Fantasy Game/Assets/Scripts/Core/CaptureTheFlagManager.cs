using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace LightPat.Core
{
    public class CaptureTheFlagManager : MonoBehaviour
    {
        public GameObject HUDPrefab;
        public int winningScore = 3;

        private Dictionary<Team, int> teamScores = new Dictionary<Team, int>();
        private GameObject HUDInstance;

        public void IncrementScore(Team team)
        {
            teamScores[team]++;
            HUDInstance.SendMessage("OnIncrementScore", new KeyValuePair<Team, int>(team, teamScores[team]));
            if (teamScores[team] >= winningScore)
            {
                Debug.Log(team + " has won!");
            }
        }

        private void Start()
        {
            HUDInstance = Instantiate(HUDPrefab);
            foreach (Team team in System.Enum.GetValues(typeof(Team)).Cast<Team>())
            {
                teamScores.Add(team, -1);
                IncrementScore(team);
            }
        }
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