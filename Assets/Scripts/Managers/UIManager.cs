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
    public TextMeshProUGUI rankingListText;

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

        // Display current time
        float time = GameManager.Instance.timeElapsed;
        if (currentTimeText != null)
        {
            int m = Mathf.FloorToInt(time / 60F);
            int s = Mathf.FloorToInt(time - m * 60);
            currentTimeText.text = "Your Time: " + string.Format("{0:00}:{1:00}", m, s);
        }

        // Display Rankings
        if (rankingListText != null)
        {
            LevelData data = SaveSystem.LoadLevelData(GameManager.Instance.levelNameForSave);
            rankingListText.text = "Top Times:\n";
            for (int i = 0; i < data.topTimes.Count; i++)
            {
                float t = data.topTimes[i];
                int min = Mathf.FloorToInt(t / 60F);
                int sec = Mathf.FloorToInt(t - min * 60);
                rankingListText.text += (i + 1) + ". " + string.Format("{0:00}:{1:00}", min, sec) + "\n";
            }
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
