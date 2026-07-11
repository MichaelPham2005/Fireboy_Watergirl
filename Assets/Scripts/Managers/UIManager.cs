using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Fusion;
using Network;

public class UIManager : MonoBehaviour
{
    [Header("Timer UI")]
    public TextMeshProUGUI timerText;

    [Header("Panels")]
    public GameObject gameOverPanel;
    public GameObject winPanel;

    [Header("Win UI Elements")]
    public TextMeshProUGUI currentTimeText;
    public TextMeshProUGUI redGemCountText;
    public TextMeshProUGUI blueGemCountText;
    public TextMeshProUGUI rankText;

    private void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);

        // Subscribe to GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWin.AddListener(ShowWinScreen);
            GameManager.Instance.OnLose.AddListener(ShowGameOverScreen);
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameActive)
        {
            UpdateTimerUI(GameManager.Instance.TimeElapsed);
        }
    }

    private void UpdateTimerUI(float time)
    {
        if (timerText != null)
        {
            // Format time as MM:SS
            int minutes = Mathf.FloorToInt(time / 60F);
            int seconds = Mathf.FloorToInt(time - minutes * 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    private void ShowGameOverScreen()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        // Only pause time in local co-op; online mode keeps network tick running
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
        {
            Time.timeScale = 0f;
        }
    }

    private void ShowWinScreen()
    {
        if (winPanel != null) winPanel.SetActive(true);

        // Only pause time in local co-op; online mode keeps network tick running
        if (GameModeManager.CurrentMode == GameModeManager.GameMode.LocalCoop)
        {
            Time.timeScale = 0f;
        }

        // Display current time (Time format only)
        float time = GameManager.Instance.TimeElapsed;
        if (currentTimeText != null)
        {
            int m = Mathf.FloorToInt(time / 60F);
            int s = Mathf.FloorToInt(time - m * 60);
            currentTimeText.text = string.Format("{0:00}:{1:00}", m, s);
        }

        // Display Gems
        if (redGemCountText != null)
        {
            redGemCountText.text = "x " + GameManager.Instance.RedGemsCollected;
        }
        if (blueGemCountText != null)
        {
            blueGemCountText.text = "x " + GameManager.Instance.BlueGemsCollected;
        }

        // Calculate, Display, and Save Rank
        if (rankText != null)
        {
            int rankNum = 4;
            // Simple rank calculation based on time
            if (time <= 60f) rankNum = 1;
            else if (time <= 90f) rankNum = 2;
            else if (time <= 120f) rankNum = 3;

            // Just display the number as requested
            rankText.text = rankNum.ToString();
            
            // Save the rank using SaveSystem
            SaveSystem.SaveRank(GameManager.Instance.levelNameForSave, rankNum);
        }
    }

    // Button Functions — now network-aware
    public void RetryLevel()
    {
        Time.timeScale = 1f;

        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer)
        {
            // Use Fusion's networked scene loading so both players reload together
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

    public void GoToHome()
    {
        Time.timeScale = 1f;

        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer)
        {
            // Shut down the network session, then go home
            var controller = NetworkRunnerController.Instance;
            if (controller != null) controller.Shutdown();
            GameModeManager.CurrentMode = GameModeManager.GameMode.LocalCoop;
        }

        SceneManager.LoadScene("Home");
    }

    public void NextLevel(string nextLevelName)
    {
        Time.timeScale = 1f;

        if (GameModeManager.CurrentMode == GameModeManager.GameMode.OnlineMultiplayer)
        {
            var controller = NetworkRunnerController.Instance;
            if (controller != null && controller.Runner != null && controller.Runner.IsServer)
            {
                int nextIndex = SceneUtility.GetBuildIndexByScenePath(nextLevelName);
                if (nextIndex >= 0)
                {
                    controller.Runner.LoadScene(SceneRef.FromIndex(nextIndex));
                }
                else
                {
                    // Fallback: try loading by name through Fusion
                    controller.Runner.LoadScene(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex + 1));
                }
            }
        }
        else
        {
            SceneManager.LoadScene(nextLevelName);
        }
    }
}
