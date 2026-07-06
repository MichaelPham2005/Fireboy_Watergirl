using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

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
        if (GameManager.Instance != null && GameManager.Instance.isGameActive)
        {
            UpdateTimerUI(GameManager.Instance.timeElapsed);
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
        Time.timeScale = 0f; // Pause game
    }

    private void ShowWinScreen()
    {
        if (winPanel != null) winPanel.SetActive(true);
        Time.timeScale = 0f; // Pause game

        // Display current time (Time format only)
        float time = GameManager.Instance.timeElapsed;
        if (currentTimeText != null)
        {
            int m = Mathf.FloorToInt(time / 60F);
            int s = Mathf.FloorToInt(time - m * 60);
            currentTimeText.text = string.Format("{0:00}:{1:00}", m, s);
        }

        // Display Gems
        if (redGemCountText != null)
        {
            redGemCountText.text = "x " + GameManager.Instance.redGemsCollected;
        }
        if (blueGemCountText != null)
        {
            blueGemCountText.text = "x " + GameManager.Instance.blueGemsCollected;
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

    // Button Functions
    public void RetryLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Home"); // Assumes Home scene exists
    }

    public void NextLevel(string nextLevelName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextLevelName);
    }
}
