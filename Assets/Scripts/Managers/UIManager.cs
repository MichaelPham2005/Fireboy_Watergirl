using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Fusion;
using Network;
using UnityEngine.UI;

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
        // Programmatic discovery of UI elements (makes it work seamlessly on all levels without scene file edits)
        GameObject menuHandlerGo = GameObject.Find("MenuHandler");
        if (menuHandlerGo == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) menuHandlerGo = canvas.gameObject;
        }

        if (menuHandlerGo != null)
        {
            // Programmatically find or create the HUD timer text under TimerBackground
            GameObject timerBgGo = FindChildGameObject(menuHandlerGo, "TimerBackground");
            if (timerBgGo != null)
            {
                Transform hudTimerTrans = timerBgGo.transform.Find("HUDTimerText");
                GameObject hudTimerGo = null;
                if (hudTimerTrans != null)
                {
                    hudTimerGo = hudTimerTrans.gameObject;
                }
                else
                {
                    hudTimerGo = new GameObject("HUDTimerText");
                    hudTimerGo.transform.SetParent(timerBgGo.transform, false);

                    // Add RectTransform and configure it to stretch to parent size
                    RectTransform rect = hudTimerGo.AddComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.sizeDelta = Vector2.zero;
                    rect.anchoredPosition = Vector2.zero;

                    // Add TextMeshProUGUI
                    TextMeshProUGUI tmpText = hudTimerGo.AddComponent<TextMeshProUGUI>();
                    tmpText.alignment = TextAlignmentOptions.Center;
                    tmpText.fontSize = 20;

                    // Copy font and color from a reference TextMeshProUGUI in the scene
                    TextMeshProUGUI refText = menuHandlerGo.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (refText != null)
                    {
                        tmpText.font = refText.font;
                        tmpText.fontSharedMaterial = refText.fontSharedMaterial;
                        tmpText.color = refText.color;
                    }
                    else
                    {
                        tmpText.color = new Color(1f, 0.92f, 0.01f, 1f); // Yellow
                    }
                }

                timerText = hudTimerGo.GetComponent<TextMeshProUGUI>();
            }

            if (gameOverPanel == null) gameOverPanel = FindChildGameObject(menuHandlerGo, "GameOverPanel");
            if (winPanel == null) winPanel = FindChildGameObject(menuHandlerGo, "WinPanel");

            // Look for CurrentTimerText specifically inside winPanel (so it doesn't get confused with HUD timer)
            if (currentTimeText == null)
            {
                if (winPanel != null)
                {
                    currentTimeText = FindChildComponent<TextMeshProUGUI>(winPanel, "CurrentTimerText");
                }
                else
                {
                    currentTimeText = FindChildComponent<TextMeshProUGUI>(menuHandlerGo, "CurrentTimerText");
                }
            }

            if (redGemCountText == null) redGemCountText = FindChildComponent<TextMeshProUGUI>(menuHandlerGo, "RedGemCountText");
            if (blueGemCountText == null) blueGemCountText = FindChildComponent<TextMeshProUGUI>(menuHandlerGo, "BlueGemCountText");
            if (rankText == null) rankText = FindChildComponent<TextMeshProUGUI>(menuHandlerGo, "RankText");

            // Programmatically bind retry and home buttons to handle Level 3 & 4 button issues
            Button[] buttons = menuHandlerGo.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                if (btn.name.Contains("Retry"))
                {
                    btn.onClick.RemoveListener(RetryLevel);
                    btn.onClick.AddListener(RetryLevel);
                }
                else if (btn.name.Contains("Home"))
                {
                    btn.onClick.RemoveListener(GoToHome);
                    btn.onClick.AddListener(GoToHome);
                }
            }
        }

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

            // Map numeric rank to Letter Grade (A, B, C, D)
            string rankLetter = "D";
            if (rankNum == 1) rankLetter = "A";
            else if (rankNum == 2) rankLetter = "B";
            else if (rankNum == 3) rankLetter = "C";

            rankText.text = rankLetter;
            
            // Save the rank using SaveSystem
            SaveSystem.SaveRank(GameManager.Instance.levelNameForSave, rankNum);
        }

        // Configure next level and home buttons dynamically based on the current level number
        ConfigureWinPanelButtons();
    }

    private void ConfigureWinPanelButtons()
    {
        if (winPanel == null) return;

        // Parse current level number
        string currentScene = SceneManager.GetActiveScene().name;
        int levelNum = 1;
        if (currentScene.Contains("_"))
        {
            int.TryParse(currentScene.Substring(currentScene.IndexOf('_') + 1), out levelNum);
        }

        // Find NextLevelButton, Home button, and Retry button inside winPanel
        Button nextBtn = FindChildComponent<Button>(winPanel, "NextLevelButton");
        Button homeBtn = FindChildComponent<Button>(winPanel, "Home");
        if (homeBtn == null) homeBtn = FindChildComponent<Button>(winPanel, "HomeButton");
        Button retryBtn = FindChildComponent<Button>(winPanel, "Retry");
        if (retryBtn == null) retryBtn = FindChildComponent<Button>(winPanel, "RetryButton");

        if (levelNum >= 4)
        {
            // Level 4: Hide NEXT button
            if (nextBtn != null) nextBtn.gameObject.SetActive(false);
            
            // Adjust RETRY button to the middle
            if (retryBtn != null)
            {
                retryBtn.gameObject.SetActive(true);
                RectTransform rt = retryBtn.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);
            }
            
            if (homeBtn != null)
            {
                homeBtn.gameObject.SetActive(true);
                TextMeshProUGUI txt = homeBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "HOME";
            }
        }
        else
        {
            // Levels 1-3: Both NEXT, RETRY, and HOME buttons active at default positions
            if (nextBtn != null)
            {
                nextBtn.gameObject.SetActive(true);
                string nextLevelName = string.Format("Level_{0:00}", levelNum + 1);
                nextBtn.onClick.RemoveAllListeners();
                nextBtn.onClick.AddListener(() => NextLevel(nextLevelName));
            }

            if (retryBtn != null)
            {
                retryBtn.gameObject.SetActive(true);
                RectTransform rt = retryBtn.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(125f, rt.anchoredPosition.y);
            }

            if (homeBtn != null)
            {
                homeBtn.gameObject.SetActive(true);
                TextMeshProUGUI txt = homeBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "HOME";
            }
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

    private GameObject FindChildGameObject(GameObject parent, string name)
    {
        Transform[] ts = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in ts)
        {
            if (t.gameObject.name == name)
            {
                return t.gameObject;
            }
        }
        return null;
    }

    private T FindChildComponent<T>(GameObject parent, string name) where T : Component
    {
        GameObject go = FindChildGameObject(parent, name);
        if (go != null)
        {
            return go.GetComponent<T>();
        }
        return null;
    }
}
