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
        
        // Save the time
        SaveSystem.SaveTime(levelNameForSave, timeElapsed);

        // Tell players to play win animation and stop
        StandardPlayerMovement[] players = FindObjectsByType<StandardPlayerMovement>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.TriggerWinSequence();
        }

        // Notify UI
        OnWin?.Invoke();
    }

    public void LoseGame()
    {
        if (!isGameActive) return;
        isGameActive = false;
        
        Debug.Log("Game Over!");
        
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
        Debug.Log("Red Gems: " + redGemsCollected);
    }

    public void CollectBlueGem()
    {
        blueGemsCollected++;
        Debug.Log("Blue Gems: " + blueGemsCollected);
    }
}
