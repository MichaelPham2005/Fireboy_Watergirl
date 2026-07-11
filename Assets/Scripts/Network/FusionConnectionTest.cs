using UnityEngine;
using Fusion;
using System.Threading.Tasks;

/// <summary>
/// Quick test script to verify Photon Fusion 2 connects successfully.
/// Attach to any GameObject in a test scene and press Play.
/// Check the Console for connection results.
/// </summary>
public class FusionConnectionTest : MonoBehaviour
{
    private NetworkRunner _runner;

    async void Start()
    {
        Debug.Log("[FusionTest] Starting Fusion connection test...");

        // Create a NetworkRunner
        _runner = gameObject.AddComponent<NetworkRunner>();

        // Try to start a session
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "TestRoom123",
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            Debug.Log($"<color=green>[FusionTest] ✓ SUCCESS! Connected to Photon Cloud!</color>");
            Debug.Log($"<color=green>[FusionTest] Session: {_runner.SessionInfo.Name}</color>");
            Debug.Log($"<color=green>[FusionTest] Is Host: {_runner.IsServer}</color>");
            Debug.Log($"<color=green>[FusionTest] Player Count: {_runner.SessionInfo.PlayerCount}</color>");
        }
        else
        {
            Debug.LogError($"[FusionTest] ✗ FAILED: {result.ShutdownReason}");
            Debug.LogError($"[FusionTest] Error: {result.ErrorMessage}");
        }
    }

    void OnDestroy()
    {
        if (_runner != null)
        {
            _runner.Shutdown();
        }
    }
}
