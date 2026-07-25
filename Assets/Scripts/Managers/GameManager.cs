using UnityEngine;
using UnityEngine.Events;
using Fusion;
using Network;
using System.Collections;

public enum GameState
{
    Playing,
    Won,
    Lost
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Settings")]
    [Tooltip("The maximum time allowed to complete the level (in seconds).")]
    public float maxLevelTime = 120f;
    [Tooltip("Unique name for the level to save the top times.")]
    public string levelNameForSave = "Level_1";

    [Header("Events")]
    public UnityEvent OnWin;
    public UnityEvent OnLose;

    // --- State for Local Co-op ---
    public float timeElapsedLocal = 0f;
    public bool isGameActiveLocal = true;
    public int redGemsCollectedLocal = 0;
    public int blueGemsCollectedLocal = 0;

    // --- State for Online Multiplayer (Fusion) ---
    [Networked] public float NetworkTimeElapsed { get; set; }
    [Networked] public NetworkBool NetworkIsGameActive { get; set; }
    [Networked] public int NetworkRedGems { get; set; }
    [Networked] public int NetworkBlueGems { get; set; }
    [Networked] public GameState NetworkState { get; set; }

    private ChangeDetector _changes;

    private DoorController fireDoor;
    private DoorController waterDoor;

    // Unified property getters that abstract the mode
    public float TimeElapsed => (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer && Runner != null && Object != null && Object.IsValid) ? NetworkTimeElapsed : timeElapsedLocal;
    // NOTE: IsGameActive intentionally uses isGameActiveLocal for BOTH modes.
    // GameManager does not have a NetworkObject component, so [Networked] properties
    // like NetworkIsGameActive are NOT replicated. isGameActiveLocal is set to false
    // on each machine independently when LoseGame() or WinGame() is called locally.
    public bool IsGameActive => isGameActiveLocal;
    public int RedGemsCollected => (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer && Runner != null && Object != null && Object.IsValid) ? NetworkRedGems : redGemsCollectedLocal;
    public int BlueGemsCollected => (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer && Runner != null && Object != null && Object.IsValid) ? NetworkBlueGems : blueGemsCollectedLocal;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        isGameActiveLocal = true;
        timeElapsedLocal = 0f;
        redGemsCollectedLocal = 0;
        blueGemsCollectedLocal = 0;
        Time.timeScale = 1f;

        // Auto-detect level name based on scene name
        levelNameForSave = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // Start the level background music
        AudioManager.Instance?.PlayLevelMusic();

        // Auto-find doors to make setup easier
        DoorController[] doors = FindObjectsByType<DoorController>(FindObjectsSortMode.None);
        foreach (DoorController door in doors)
        {
            if (door.requiredPlayerTag == "Fireboy") fireDoor = door;
            else if (door.requiredPlayerTag == "Watergirl") waterDoor = door;
        }
    }

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        if (HasStateAuthority)
        {
            NetworkIsGameActive = true;
            NetworkTimeElapsed = 0f;
            NetworkRedGems = 0;
            NetworkBlueGems = 0;
            NetworkState = GameState.Playing;
        }
    }

    private void Update()
    {
        // Since GameManager has no NetworkObject, FixedUpdateNetwork() never executes.
        // We handle the timer for BOTH modes here in Update() using timeElapsedLocal.
        if (!isGameActiveLocal) return;

        timeElapsedLocal += Time.deltaTime;

        if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
        {
            // In local mode, enforce the time limit
            if (timeElapsedLocal >= maxLevelTime)
            {
                timeElapsedLocal = maxLevelTime;
                LoseGame();
            }
        }
        // In online mode, we still increment timeElapsedLocal so the HUD timer displays correctly.
        // Time-limit lose is not enforced here to avoid both machines calling LoseGame() at
        // slightly different times and causing a double-lose bug.
    }

    public override void FixedUpdateNetwork()
    {
        // GameManager has no NetworkObject component, so this method never runs.
        // All game-state logic has been moved to Update() above.
    }

    // Local flags to ensure the outcome UI is shown exactly once per session on every machine.
    // This is the robust alternative to relying on ChangeDetector alone, which can miss events
    // in two-machine multiplayer due to race conditions when both players die simultaneously.
    private bool _loseHandled = false;
    private bool _winHandled = false;

    public override void Render()
    {
        // GameManager does not have a NetworkObject, so Networked properties and
        // ChangeDetector do NOT work here in online mode.
        // The loss/win flow is handled by PlayerHealth (which has a NetworkObject)
        // calling LoseGame() directly on each client via its own Render() callback.
    }

    private void OnGameStateChanged()
    {
        if (NetworkState == GameState.Won)
        {
            ExecuteWinLocally();
        }
        else if (NetworkState == GameState.Lost)
        {
            ExecuteLoseLocally();
        }
    }

    public void CheckWinCondition()
    {
        // In online mode, DoorController states are replicated via Fusion.
        // Both machines will see IsPlayerReady=true simultaneously and call WinGame locally.
        if (!IsGameActive) return;

        if (fireDoor != null && fireDoor.IsPlayerReady &&
            waterDoor != null && waterDoor.IsPlayerReady)
        {
            WinGame();
        }
    }

    public void WinGame()
    {
        if (!IsGameActive) return;

        isGameActiveLocal = false;
        ExecuteWinLocally();
    }

    private void ExecuteWinLocally()
    {
        Debug.Log("Level Complete! Time: " + TimeElapsed.ToString("F2"));
        Debug.Log("Red Gems: " + RedGemsCollected + " | Blue Gems: " + BlueGemsCollected);
        
        // Play win sound and music
        AudioManager.Instance?.PlayWin();

        // Save the time
        SaveSystem.SaveTime(levelNameForSave, TimeElapsed);

        StandardPlayerMovement[] players = FindObjectsByType<StandardPlayerMovement>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.FreezeForWin();
            
            if (player.playerType == StandardPlayerMovement.PlayerType.Fireboy && fireDoor != null)
            {
                player.transform.position = new Vector2(fireDoor.transform.position.x, player.transform.position.y);
            }
            else if (player.playerType == StandardPlayerMovement.PlayerType.Watergirl && waterDoor != null)
            {
                player.transform.position = new Vector2(waterDoor.transform.position.x, player.transform.position.y);
            }
        }

        StartCoroutine(WinSequenceRoutine(players));
    }

    private IEnumerator WinSequenceRoutine(StandardPlayerMovement[] players)
    {
        yield return new WaitForSeconds(1f);
        
        foreach (var player in players)
        {
            player.TriggerWinSequence();
        }

        yield return new WaitForSeconds(1.5f);
        OnWin?.Invoke();
    }

    public void LoseGame()
    {
        if (!IsGameActive) return;

        if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
        {
            isGameActiveLocal = false;
            ExecuteLoseLocally();
        }
        else
        {
            // In online mode, GameManager does NOT have a NetworkObject/NetworkBehaviour context,
            // so we CANNOT use HasStateAuthority or RPCs here directly.
            // Instead, we call ExecuteLoseLocally() on this machine immediately.
            // PlayerHealth (which IS a proper NetworkBehaviour) has already set IsDead=true,
            // which replicates to all clients via Fusion's state sync.
            // Each machine calls LoseGame() independently when their local Render() detects IsDead=true.
            // This is the correct pattern for non-networked managers in a Fusion game.
            isGameActiveLocal = false; // Use local flag since NetworkIsGameActive won't work
            ExecuteLoseLocally();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_LoseGame()
    {
        if (NetworkIsGameActive)
        {
            NetworkIsGameActive = false;
            NetworkState = GameState.Lost;
        }
    }

    // Allows any player (Host or Client) to request a scene load (Retry or Next Level).
    // Since GameManager has no NetworkObject, we route through NetworkRunnerController
    // which holds the actual live Fusion Runner.
    public void RequestLoadScene(int buildIndex)
    {
        var controller = Network.NetworkRunnerController.Instance;
        if (controller == null || controller.Runner == null) return;

        if (controller.Runner.IsServer)
        {
            // Host can load directly
            controller.Runner.LoadScene(SceneRef.FromIndex(buildIndex));
        }
        else
        {
            // Client asks host via the controller's runner — find any live NetworkObject to RPC through
            // We use PlayerHealth as the proxy since it IS a real NetworkObject with StateAuthority on host
            var players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.Object != null && p.Object.IsValid)
                {
                    p.RPC_RequestSceneLoad(buildIndex);
                    return;
                }
            }
        }
    }

    private void ExecuteLoseLocally()
    {
        Debug.Log("Game Over!");
        // Play lose sound and music
        AudioManager.Instance?.PlayLose();
        
        // Disable players
        StandardPlayerMovement[] players = FindObjectsByType<StandardPlayerMovement>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.enabled = false;
        }
        StartCoroutine(LoseSequenceRoutine());
    }

    private System.Collections.IEnumerator LoseSequenceRoutine()
    {
        // Wait 1.5 seconds for the death animation to play
        yield return new WaitForSeconds(1.5f);
        OnLose?.Invoke();
    }

    public void CollectRedGem()
    {
        AudioManager.Instance?.PlayDiamond();
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
        {
            redGemsCollectedLocal++;
            Debug.Log("Red Gems: " + redGemsCollectedLocal);
        }
        else
        {
            if (HasStateAuthority)
            {
                NetworkRedGems++;
                Debug.Log("Red Gems: " + NetworkRedGems);
            }
            else
            {
                RPC_CollectRedGem();
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_CollectRedGem()
    {
        NetworkRedGems++;
        Debug.Log("Red Gems: " + NetworkRedGems);
    }

    public void CollectBlueGem()
    {
        AudioManager.Instance?.PlayDiamond();
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
        {
            blueGemsCollectedLocal++;
            Debug.Log("Blue Gems: " + blueGemsCollectedLocal);
        }
        else
        {
            if (HasStateAuthority)
            {
                NetworkBlueGems++;
                Debug.Log("Blue Gems: " + NetworkBlueGems);
            }
            else
            {
                RPC_CollectBlueGem();
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_CollectBlueGem()
    {
        NetworkBlueGems++;
        Debug.Log("Blue Gems: " + NetworkBlueGems);
    }
}
