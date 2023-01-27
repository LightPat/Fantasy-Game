using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using LightPat.Core;
using System.Linq;

namespace LightPat.UI
{
    public class CaptureTheFlagHUD : MonoBehaviour
    {
        private TextMeshProUGUI[] teamScoreDisplays;
        private List<Team> teams = new List<Team>();

        public void OnIncrementScore(KeyValuePair<int, int> teamScore)
        {
            if (teamScore.Key < teams.Count)
                teamScoreDisplays[teamScore.Key].SetText(teams[teamScore.Key] + ": " + teamScore.Value);
        }

        private void Awake()
        {
            teamScoreDisplays = GetComponentsInChildren<TextMeshProUGUI>();
            int i = 0;
            foreach (Team team in System.Enum.GetValues(typeof(Team)).Cast<Team>())
            {
                if (team == Team.Environment) { continue; }
                teams.Add(team);
                teamScoreDisplays[i].SetText(team + ": " + 0);
                i++;
            }
        }
    }
}