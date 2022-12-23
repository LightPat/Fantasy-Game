using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using LightPat.Core;

namespace LightPat.UI
{
    public class CaptureTheFlagHUD : MonoBehaviour
    {
        public TextMeshProUGUI redTeamScore;
        public TextMeshProUGUI blueTeamScore;

        public void OnIncrementScore(KeyValuePair<Team, int> teamScore)
        {
            if (teamScore.Key == Team.Red)
                redTeamScore.SetText(teamScore.Key + ": " + teamScore.Value);
            else if (teamScore.Key == Team.Blue)
                blueTeamScore.SetText(teamScore.Key + ": " + teamScore.Value);
        }

        private void Start()
        {
            redTeamScore.color = Color.red;
            blueTeamScore.color = Color.blue;
        }
    }
}