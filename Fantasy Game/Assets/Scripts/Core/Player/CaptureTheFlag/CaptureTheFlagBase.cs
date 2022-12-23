using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core.Player
{
    public class CaptureTheFlagBase : MonoBehaviour
    {
        public Team team;
        public Flag flagPrefab;
        [SerializeField] private Vector3 flagLocalPosition;
        [SerializeField] private CaptureTheFlagManager gameManager;

        private Flag currentFlag;

        public void RefreshFlag()
        {
            GameObject newFlag = Instantiate(flagPrefab.gameObject, transform.transform, true);
            newFlag.transform.localPosition = flagLocalPosition;
            currentFlag = newFlag.GetComponent<Flag>();
            currentFlag.SetBase(this);
        }

        private void Start()
        {
            RefreshFlag();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody)
            {
                Flag carryingFlag = other.attachedRigidbody.GetComponentInChildren<Flag>();
                if (carryingFlag)
                {
                    if (carryingFlag.team != team)
                    {
                        gameManager.IncrementScore(team);
                        Destroy(carryingFlag.gameObject);
                    }
                }
            }
        }
    }
}