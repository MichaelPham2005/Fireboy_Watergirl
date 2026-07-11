using UnityEngine;
using TMPro; 
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    // We use a private variable here, and the script will find the object automatically
    [SerializeField] private GameObject pauseMenuPanel;
    public TextMeshProUGUI levelTitleText; 

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
        // Programmatically find components if not assigned
        if (pauseMenuPanel != null)
        {
            if (levelTitleText == null)
            {
                levelTitleText = pauseMenuPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            // Bind Continue Button
            Transform continueTrans = pauseMenuPanel.transform.Find("Continue");
            if (continueTrans != null)
            {
                Button btn = continueTrans.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveListener(ContinueGame);
                    btn.onClick.AddListener(ContinueGame);
                }
                
                // Ensure the button image is raycast-enabled so clicks are registered
                Image img = continueTrans.GetComponent<Image>();
                if (img != null)
                {
                    img.raycastTarget = true;
                }
            }

            // Bind Retry Button
            Transform retryTrans = pauseMenuPanel.transform.Find("Retry");
            if (retryTrans != null)
            {
                Button btn = retryTrans.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveListener(RetryLevel);
                    btn.onClick.AddListener(RetryLevel);
                }
            }

            // Bind Home Button
            Transform homeTrans = pauseMenuPanel.transform.Find("Home");
            if (homeTrans != null)
            {
                Button btn = homeTrans.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveListener(GoToHome);
                    btn.onClick.AddListener(GoToHome);
                }
            }
        }

        // Bind the main HUD settings/pause button
        GameObject settingsBtnGo = GameObject.Find("Btn_Settings");
        if (settingsBtnGo == null) settingsBtnGo = GameObject.Find("SettingsButton");
        if (settingsBtnGo != null)
        {
            Button btn = settingsBtnGo.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(PauseGame);
                btn.onClick.AddListener(PauseGame);
            }
        }

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
            Time.timeScale = 0f;            // Stop game time
        }
    }

    // Function to resume the game
    public void ContinueGame()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false); // Hide the menu
            Time.timeScale = 1f;             // Resume game time
        }
    }

    // Function to restart the current level
    public void RetryLevel()
    {
        Time.timeScale = 1f; // Must resume time before loading the new scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }

    // Function to return to the Home menu
    public void GoToHome()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene("Home"); // Ensure the "Home" scene is added to Build Settings
    }
}