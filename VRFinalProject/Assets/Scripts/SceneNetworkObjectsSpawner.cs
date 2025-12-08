using Unity.Netcode;
using UnityEngine;

public class SceneNetworkObjectsSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject[] sceneNetworkObjects;
    private bool hasSpawned = false;
    
    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.Log("[SceneNetworkSpawner] NetworkManager already listening, attempting spawn...");
                SpawnSceneObjects();
            }
        }
        else
        {
            Debug.LogError("[SceneNetworkSpawner] NetworkManager.Singleton is null!");
        }
    }

    private void OnServerStarted()
    {
        Debug.Log("[SceneNetworkSpawner] Server started event received");
        SpawnSceneObjects();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer && !hasSpawned)
        {
            Debug.Log($"[SceneNetworkSpawner] Client {clientId} connected, server spawning objects...");
            SpawnSceneObjects();
        }
    }

    private void SpawnSceneObjects()
    {
        if (hasSpawned)
        {
            Debug.Log("[SceneNetworkSpawner] Already spawned, skipping...");
            return;
        }

        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost)
        {
            Debug.Log("[SceneNetworkSpawner] Not server/host, skipping spawn...");
            return;
        }

        if (sceneNetworkObjects == null || sceneNetworkObjects.Length == 0)
        {
            Debug.LogWarning("[SceneNetworkSpawner] No scene network objects assigned!");
            return;
        }

        foreach (var networkObject in sceneNetworkObjects)
        {
            if (networkObject != null && !networkObject.IsSpawned)
            {
                Debug.Log($"[SceneNetworkSpawner] Spawning {networkObject.gameObject.name}...");
                try
                {
                    networkObject.Spawn();
                    Debug.Log($"[SceneNetworkSpawner] Successfully spawned {networkObject.gameObject.name}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SceneNetworkSpawner] Failed to spawn {networkObject.gameObject.name}: {e.Message}");
                }
            }
            else if (networkObject != null && networkObject.IsSpawned)
            {
                Debug.Log($"[SceneNetworkSpawner] {networkObject.gameObject.name} already spawned");
            }
        }

        hasSpawned = true;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}
