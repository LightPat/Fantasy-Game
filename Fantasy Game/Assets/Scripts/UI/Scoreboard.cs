using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using TMPro;

namespace LightPat.UI
{
    public class Scoreboard : MonoBehaviour
    {
        public Transform dataParent;
        public GameObject dataPrefab;
        public int dataPrefabSpacing = 20;

        private void Update()
        {
            foreach (Transform child in dataParent)
            {
                Destroy(child.gameObject);
            }

            int i = 0;
            foreach (KeyValuePair<ulong, ClientData> clientData in ClientManager.Singleton.GetClientDataDictionary())
            {
                GameObject dataInstance = Instantiate(dataPrefab, dataParent);
                TextMeshProUGUI playerName = dataInstance.transform.Find("Player Name").GetComponent<TextMeshProUGUI>();
                playerName.SetText(clientData.Value.clientName);
                playerName.color = (Color)typeof(Color).GetProperty(clientData.Value.team.ToString().ToLowerInvariant()).GetValue(null, null);
                dataInstance.transform.Find("Kills").GetComponent<TextMeshProUGUI>().SetText(clientData.Value.kills.ToString());
                dataInstance.transform.Find("Deaths").GetComponent<TextMeshProUGUI>().SetText(clientData.Value.deaths.ToString());
                dataInstance.transform.Find("Damage Done").GetComponent<TextMeshProUGUI>().SetText(clientData.Value.damageDone.ToString());
                dataInstance.transform.localPosition -= new Vector3(0, dataPrefabSpacing * i, 0);
                i++;

                //NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject
            }
        }
    }
}
