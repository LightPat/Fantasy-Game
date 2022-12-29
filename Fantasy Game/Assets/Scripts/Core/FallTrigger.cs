using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class FallTrigger : MonoBehaviour
    {
        CaptureTheFlagManager gameManager;

        private void Start()
        {
            gameManager = FindObjectOfType<CaptureTheFlagManager>();
        }

        private void OnTriggerEnter(Collider other)
        {
            foreach (TeamSpawnPoint teamSpawnPoint in gameManager.spawnPoints)
            {
                if (teamSpawnPoint.team == other.GetComponentInParent<Attributes>().team)
                {
                    other.attachedRigidbody.transform.position = teamSpawnPoint.spawnPosition;
                    other.attachedRigidbody.transform.rotation = Quaternion.Euler(teamSpawnPoint.spawnRotation);
                    break;
                }
            }
        }
    }
}