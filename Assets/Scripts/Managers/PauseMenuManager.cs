using UnityEngine;
using TMPro; 
using UnityEngine.SceneManagement;
using Fusion;
using Network;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    // We use a private variable here, and the script will find the object automatically
    [SerializeField] private GameObject pauseMenuPanel;
    public TextMeshProUGUI levelTitleText; 

    private bool isPausedLocally = false;

    void Awake()
    {
        // Find the object even if it's disabled in the hierarchy
        // We use a helper method to find the child regardless of active state
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child.name == "PausePanel")
            {
                pauseMenuPanel = child.gameObject;
                break;
            }
        }

        if (pauseMenuPanel == null)
        {
            Debug.LogError("Error: Cannot find 'PausePanel' in the hierarchy!");
        }
        else
        {
            pauseMenuPanel.SetActive(false); // Ensure it's hidden at start
        }
    }

    void Start()
    {
        // Automatically set the title text when the scene starts
        if (levelTitleText != null)
        {
            // Get current scene name and replace underscore with a space for better formatting
            string currentScene = SceneManager.GetActiveScene().name;
            levelTitleText.text = currentScene.Replace("_", " "); 
        }
    }

    // Function to pause the game
    public void PauseGame()
    {
        Debug.Log("Pause button clicked");
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true); // Show the menu

            if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
            {
                // Local co-op: freeze time as before
                Time.timeScale = 0f;
            }
            else
            {
                // Online: suppress local input instead of freezing time
                // (Network tick must keep running for the other player)
                isPausedLocally = true;
                SuppressLocalInput(true);
            }
        }
    }

    // Function to resume the game
    public void ContinueGame()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false); // Hide the menu

            if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
            {
                Time.timeScale = 1f;
            }
            else
            {
                isPausedLocally = false;
                SuppressLocalInput(false);
            }
        }
    }

    // Function to restart the current level
    public void RetryLevel()
    {
        Time.timeScale = 1f;
        isPausedLocally = false;
        SuppressLocalInput(false);

        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer)
        {
            var controller = NetworkRunnerController.Instance;
            if (controller != null && controller.Runner != null && controller.Runner.IsServer)
            {
                controller.Runner.LoadScene(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));
            }
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
        }
    }

    // Function to return to the Home menu
    public void GoToHome()
    {
        Time.timeScale = 1f;
        isPausedLocally = false;
        SuppressLocalInput(false);

        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer)
        {
            var controller = NetworkRunnerController.Instance;
            if (controller != null) controller.Shutdown();
            GameModeManager.CurrentMode = GameModeManager.GameMode.LocalCoop;
        }

        SceneManager.LoadScene("Home");
    }

    /// <summary>
    /// When paused in online mode, we suppress the local player's input
    /// so they stop moving, but the network tick continues for the other player.
    /// </summary>
    private void SuppressLocalInput(bool suppress)
    {
        // Find the local player's movement script and disable/enable it
        StandardPlayerMovement[] players = FindObjectsByType<StandardPlayerMovement>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer)
            {
                // Only suppress the LOCAL player's input
                if (player.HasInputAuthority)
                {
                    player.enabled = !suppress;
                }
            }
        }
    }
}