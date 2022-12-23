using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace LightPat.Core
{
    public class CaptureTheFlagManager : MonoBehaviour
    {
        public int winningScore = 3;
        private Dictionary<Team, int> teamScores = new Dictionary<Team, int>();

        public void IncrementScore(Team team)
        {
            teamScores[team]++;
            Debug.Log(team + " " + teamScores[team]);
            if (teamScores[team] >= winningScore)
            {
                Debug.Log(team + " has won!");
            }
        }

        private void Start()
        {
            foreach (Team team in System.Enum.GetValues(typeof(Team)).Cast<Team>())
            {
                teamScores.Add(team, 0);
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