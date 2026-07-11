using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Network
{
    /// <summary>
    /// Spawns the appropriate player character prefab when a player joins the session.
    /// Host gets Fireboy, Client gets Watergirl.
    /// </summary>
    public class PlayerSpawner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
    {
        [Header("Player Prefabs")]
        [Tooltip("The NetworkObject prefab for Fireboy (Host)")]
        public NetworkObject fireboyPrefab;
        [Tooltip("The NetworkObject prefab for Watergirl (Client)")]
        public NetworkObject watergirlPrefab;

        [Header("Spawn Points")]
        public Transform fireboySpawnPoint;
        public Transform watergirlSpawnPoint;

        private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

        public void PlayerJoined(PlayerRef player)
        {
            if (!HasStateAuthority) return;

            Debug.Log($"PlayerSpawner: Spawning character for player {player.PlayerId}");

            // The host is Player 1 (Fireboy), the first client is Player 2 (Watergirl)
            // In a 2-player game, runner.IsServer is the host. 
            // Wait, PlayerJoined is called for the host themselves, and then for clients.
            // Let's determine which prefab based on whether it's the host player.
            bool isHostPlayer = player == Runner.LocalPlayer && Runner.IsServer;
            // Also, if the joining player is the server/host.
            
            // A more robust way: If it's the server player, it's Fireboy. Else Watergirl.
            bool isHost = (player == Runner.LocalPlayer);

            NetworkObject prefabToSpawn = isHost ? fireboyPrefab : watergirlPrefab;
            Vector3 spawnPos = isHost ? 
                (fireboySpawnPoint != null ? fireboySpawnPoint.position : Vector3.zero) : 
                (watergirlSpawnPoint != null ? watergirlSpawnPoint.position : Vector3.zero);

            if (prefabToSpawn != null)
            {
                NetworkObject spawnedObj = Runner.Spawn(prefabToSpawn, spawnPos, Quaternion.identity, player);
                spawnedCharacters.Add(player, spawnedObj);
                Debug.Log($"PlayerSpawner: Spawned {prefabToSpawn.name} for Player {player.PlayerId} | isHost: {isHost} | InputAuth assigned: {spawnedObj.InputAuthority}");
            }
            else
            {
                Debug.LogError("PlayerSpawner: Prefab to spawn is null!");
            }
        }

        public void PlayerLeft(PlayerRef player)
        {
            if (!HasStateAuthority) return;

            if (spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
            {
                if (networkObject != null)
                {
                    Runner.Despawn(networkObject);
                }
                spawnedCharacters.Remove(player);
            }
        }
    }
}
