using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

namespace LightPat.Core
{
    [RequireComponent(typeof(Weapon))]
    public class NetworkedWeapon : NetworkBehaviour
    {
        public GameObject prefabReference;
        public int maxPickups = 1;

        int currentPickups;

        public Weapon GenerateLocalInstance()
        {
            if (currentPickups >= maxPickups) { return null; }
            currentPickups++;
            GameObject g = Instantiate(prefabReference, transform.position, transform.rotation);
            Destroy(g.GetComponent<NetworkedWeapon>());
            Destroy(g.GetComponent<NetworkTransform>());
            Destroy(g.GetComponent<NetworkObject>());
            g.transform.position = transform.position;
            g.transform.rotation = transform.rotation;
            return g.GetComponent<Weapon>();
        }
    }
}