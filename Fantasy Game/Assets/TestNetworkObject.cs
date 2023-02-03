using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class TestNetworkObject : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        Debug.Log("Spawning");
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log("Despawning");
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        Debug.Log("Destroying");
    }

    public bool spawn;
    public bool despawn;
    private void Update()
    {
        if (spawn)
        {
            NetworkObject.Spawn();
            spawn = false;
        }
        if (despawn)
        {
            NetworkObject.Despawn(false);
            despawn = false;
        }
    }
}
