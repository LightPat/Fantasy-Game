using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    [RequireComponent(typeof(NetworkObject))]
    public class NestedNetworkObject : NetworkBehaviour
    {
        public GameObject[] nestedNetworkObjectPrefab;

        public void NestedSpawn()
        {
            StartCoroutine(Spawn());
        }

        private IEnumerator Spawn()
        {
            yield return null;

            if (!NetworkObject.IsSpawned)
                NetworkObject.Spawn(true);

            if (nestedNetworkObjectPrefab.Length > 0)
            {
                yield return new WaitUntil(() => NetworkObject.IsSpawned);
                for (int i = 0; i < nestedNetworkObjectPrefab.Length; i++)
                {
                    GameObject g = Instantiate(nestedNetworkObjectPrefab[i]);
                    NestedNetworkObject nestedNetObj = g.GetComponent<NestedNetworkObject>();
                    nestedNetObj.NestedSpawn();

                    yield return new WaitUntil(() => nestedNetObj.NetworkObject.IsSpawned);
                    nestedNetObj.NetworkObject.TrySetParent(transform, true);
                    nestedNetObj.transform.localPosition = nestedNetworkObjectPrefab[i].transform.position;
                    nestedNetObj.transform.localEulerAngles = nestedNetworkObjectPrefab[i].transform.eulerAngles;
                    nestedNetObj.transform.localScale = nestedNetworkObjectPrefab[i].transform.localScale;
                }
            }
        }
    }
}