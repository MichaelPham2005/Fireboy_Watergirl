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
    public bool IsGameActive => (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer && Runner != null && Object != null && Object.IsValid) ? (bool)NetworkIsGameActive : isGameActiveLocal;
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
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
        {
            if (!isGameActiveLocal) return;

            timeElapsedLocal += Time.deltaTime;
            if (timeElapsedLocal >= maxLevelTime)
            {
                timeElapsedLocal = maxLevelTime;
                LoseGame();
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer && HasStateAuthority)
        {
            if (!NetworkIsGameActive) return;

            NetworkTimeElapsed += Runner.DeltaTime;
            if (NetworkTimeElapsed >= maxLevelTime)
            {
                NetworkTimeElapsed = maxLevelTime;
                LoseGame();
            }
        }
    }

    public override void Render()
    {
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer)
        {
            foreach (var change in _changes.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(NetworkState):
                        OnGameStateChanged();
                        break;
                }
            }
        }
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
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer && !HasStateAuthority)
            return;

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

        if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
        {
            isGameActiveLocal = false;
            ExecuteWinLocally();
        }
        else
        {
            if (HasStateAuthority)
            {
                NetworkIsGameActive = false;
                NetworkState = GameState.Won;
            }
        }
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
            // If called by client, we need RPC. For now, we assume authority or use an RPC.
            // Simplified: if client dies, they could tell host. But for this refactor, 
            // if we are not host, we call an RPC.
            if (HasStateAuthority)
            {
                NetworkIsGameActive = false;
                NetworkState = GameState.Lost;
            }
            else if (Object != null && Object.IsValid)
            {
                RPC_LoseGame();
            }
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
