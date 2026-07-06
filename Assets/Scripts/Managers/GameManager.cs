using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Settings")]
    [Tooltip("The maximum time allowed to complete the level (in seconds).")]
    public float maxLevelTime = 120f;
    [Tooltip("Unique name for the level to save the top times.")]
    public string levelNameForSave = "Level_1";

    [Header("State")]
    public float timeElapsed = 0f;
    public bool isGameActive = true;
    public int redGemsCollected = 0;
    public int blueGemsCollected = 0;

    [Header("Events")]
    public UnityEvent OnWin;
    public UnityEvent OnLose;

    private DoorController fireDoor;
    private DoorController waterDoor;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        isGameActive = true;
        timeElapsed = 0f;
        redGemsCollected = 0;
        blueGemsCollected = 0;
        Time.timeScale = 1f;

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

    private void Update()
    {
        if (!isGameActive) return;

        timeElapsed += Time.deltaTime;

        if (timeElapsed >= maxLevelTime)
        {
            timeElapsed = maxLevelTime;
            LoseGame();
        }
    }

    public void CheckWinCondition()
    {
        if (!isGameActive) return;

        if (fireDoor != null && fireDoor.IsPlayerReady &&
            waterDoor != null && waterDoor.IsPlayerReady)
        {
            WinGame();
        }
    }

    public void WinGame()
    {
        if (!isGameActive) return;
        isGameActive = false;
        
        Debug.Log("Level Complete! Time: " + timeElapsed.ToString("F2"));
        Debug.Log("Red Gems: " + redGemsCollected + " | Blue Gems: " + blueGemsCollected);
        
        // Play win sound and music
        AudioManager.Instance?.PlayWin();
        
        // Save the time
        SaveSystem.SaveTime(levelNameForSave, timeElapsed);

        // Stop both players from moving immediately and perfectly center them on their doors
        StandardPlayerMovement[] players = FindObjectsByType<StandardPlayerMovement>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.FreezeForWin();
            
            // Snap to the exact center X position of their respective door
            if (player.playerType == StandardPlayerMovement.PlayerType.Fireboy && fireDoor != null)
            {
                player.transform.position = new Vector2(fireDoor.transform.position.x, player.transform.position.y);
            }
            else if (player.playerType == StandardPlayerMovement.PlayerType.Watergirl && waterDoor != null)
            {
                player.transform.position = new Vector2(waterDoor.transform.position.x, player.transform.position.y);
            }
        }

        // Start delay sequence before showing UI
        StartCoroutine(WinSequenceRoutine(players));
    }

    private System.Collections.IEnumerator WinSequenceRoutine(StandardPlayerMovement[] players)
    {
        // 1. Wait for 1 second to let the doors fully open
        yield return new WaitForSeconds(1f);
        
        // 2. Trigger the players to walk into the doors
        foreach (var player in players)
        {
            player.TriggerWinSequence();
        }

        // 3. Wait for 1.5 seconds for player entering animations to finish
        yield return new WaitForSeconds(1.5f);
        
        // Notify UI
        OnWin?.Invoke();
    }

    public void LoseGame()
    {
        if (!isGameActive) return;
        isGameActive = false;
        
        Debug.Log("Game Over!");
        
        // Play lose sound and music
        AudioManager.Instance?.PlayLose();
        
        // Disable players
        StandardPlayerMovement[] players = FindObjectsByType<StandardPlayerMovement>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.enabled = false;
        }

        OnLose?.Invoke();
    }

    public void CollectRedGem()
    {
        redGemsCollected++;
        AudioManager.Instance?.PlayDiamond();
        Debug.Log("Red Gems: " + redGemsCollected);
    }

    public void CollectBlueGem()
    {
        blueGemsCollected++;
        AudioManager.Instance?.PlayDiamond();
        Debug.Log("Blue Gems: " + blueGemsCollected);
    }
}
