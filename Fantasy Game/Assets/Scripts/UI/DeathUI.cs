using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LightPat.UI
{
    public class DeathUI : MonoBehaviour
    {
        public TextMeshProUGUI respawnText;

        float startTime;

        private void Start()
        {
            startTime = Time.time;
            StartCoroutine(StartRespawnCounter());
        }

        private IEnumerator StartRespawnCounter()
        {
            yield return new WaitForSeconds(5);
            Destroy(gameObject);
        }

        private void Update()
        {
            respawnText.SetText("Respawning in " + (5-(Time.time-startTime)).ToString("F5") + "...");
        }
    }
}