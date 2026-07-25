using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject rankingPanel;
    public GameObject customPanel;
    public GameObject lobbyPanel;
    
    public static MainMenuManager Instance;

    [Header("Ranking UI Elements")]
    public TextMeshProUGUI level1RankText;
    public TextMeshProUGUI level2RankText;
    public TextMeshProUGUI level3RankText;
    public TextMeshProUGUI level4RankText;

    private GameObject lockPanel;

    private void Start()
    {
        Instance = this;
        CreateLockPanel();

        // Ensure only the main panel is open at start
        ShowMainPanel();

        // 1. Programmatically hook up the ranking button onClick event
        GameObject rankingBtnGo = GameObject.Find("RankningButton");
        if (rankingBtnGo == null) rankingBtnGo = GameObject.Find("RankingButton");
        if (rankingBtnGo != null)
        {
            Button btn = rankingBtnGo.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(ShowRankingPanel);
                btn.onClick.AddListener(ShowRankingPanel);
            }
        }

        // 2. Programmatically configure the panels and elements
        if (rankingPanel != null)
        {
            Transform lvl1Go = null;
            Transform lvl2Go = null;
            Transform lvl3Go = null;
            Transform lvl4Go = null;

            // Find the correct row containers by their name suffixes (1, 2, 3, 4)
            for (int i = 0; i < rankingPanel.transform.childCount; i++)
            {
                Transform child = rankingPanel.transform.GetChild(i);
                string childName = child.name;

                if (childName.Contains("(1)")) lvl1Go = child;
                else if (childName.Contains("(2)")) lvl2Go = child;
                else if (childName.Contains("(3)")) lvl3Go = child;
                else if (childName.Contains("(4)") || childName.Contains("Level4")) lvl4Go = child;
            }

            // Configure each row precisely
            if (lvl1Go != null)
            {
                lvl1Go.gameObject.name = "Level1_Container";
                lvl1Go.GetComponent<TextMeshProUGUI>().text = "<color=#FFBF00>LEVEL 1</color>";
                if (lvl1Go.childCount > 0)
                {
                    lvl1Go.GetChild(0).gameObject.name = "Level1_RankText";
                    level1RankText = lvl1Go.GetChild(0).GetComponent<TextMeshProUGUI>();
                }
            }

            if (lvl2Go != null)
            {
                lvl2Go.gameObject.name = "Level2_Container";
                lvl2Go.GetComponent<TextMeshProUGUI>().text = "<color=#FFBF00>LEVEL 2</color>";
                if (lvl2Go.childCount > 0)
                {
                    lvl2Go.GetChild(0).gameObject.name = "Level2_RankText";
                    level2RankText = lvl2Go.GetChild(0).GetComponent<TextMeshProUGUI>();
                }
            }

            if (lvl3Go != null)
            {
                lvl3Go.gameObject.name = "Level3_Container";
                lvl3Go.GetComponent<TextMeshProUGUI>().text = "<color=#FFBF00>LEVEL 3</color>";
                if (lvl3Go.childCount > 0)
                {
                    lvl3Go.GetChild(0).gameObject.name = "Level3_RankText";
                    level3RankText = lvl3Go.GetChild(0).GetComponent<TextMeshProUGUI>();
                }
            }

            if (lvl4Go != null)
            {
                lvl4Go.gameObject.name = "Level4_Container";
                lvl4Go.GetComponent<TextMeshProUGUI>().text = "<color=#FFBF00>LEVEL 4</color>";
                if (lvl4Go.childCount > 0)
                {
                    lvl4Go.GetChild(0).gameObject.name = "Level4_RankText";
                    level4RankText = lvl4Go.GetChild(0).GetComponent<TextMeshProUGUI>();
                }
            }

            // 4. Hook up click listener to the manually added Back button in RankingPanel
            Button backBtn = rankingPanel.GetComponentInChildren<Button>(true);
            if (backBtn != null)
            {
                backBtn.onClick.RemoveListener(ShowMainPanel);
                backBtn.onClick.AddListener(ShowMainPanel);
            }
        }
    }

    public void ShowRankingPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (rankingPanel != null) rankingPanel.SetActive(true);
        if (customPanel != null) customPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);

        UpdateRankText(level1RankText, "Level_01");
        UpdateRankText(level2RankText, "Level_02");
        UpdateRankText(level3RankText, "Level_03");
        UpdateRankText(level4RankText, "Level_04");
    }

    public void ShowCustomPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (rankingPanel != null) rankingPanel.SetActive(false);
        if (customPanel != null) customPanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
    }

    public void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (rankingPanel != null) rankingPanel.SetActive(false);
        if (customPanel != null) customPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        UpdateMainPanelVisuals();
    }

    private void UpdateMainPanelVisuals()
    {
        if (mainPanel == null) return;
        TextMeshProUGUI[] texts = mainPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 1; i <= 4; i++)
        {
            string levelStr = "LEVEL " + i;
            string sceneName = "Level_0" + i;
            bool isUnlocked = IsLevelUnlocked(sceneName);
            
            foreach (var txt in texts)
            {
                if (txt.text.Contains(levelStr))
                {
                    if (isUnlocked)
                    {
                        txt.text = "<color=#FFBF00>" + levelStr + "</color>";
                    }
                    else
                    {
                        txt.text = "<color=#808080>" + levelStr + "</color>";
                    }
                }
            }
        }
    }

    private void UpdateRankText(TextMeshProUGUI textElement, string levelName)
    {
        if (textElement != null)
        {
            bool isUnlocked = IsLevelUnlocked(levelName);

            // Update parent container text color for visual lock feedback
            TextMeshProUGUI parentText = textElement.transform.parent.GetComponent<TextMeshProUGUI>();
            if (parentText != null)
            {
                string baseName = "LEVEL " + levelName.Substring(levelName.Length - 1);
                if (isUnlocked)
                {
                    parentText.text = "<color=#FFBF00>" + baseName + "</color>";
                }
                else
                {
                    parentText.text = "<color=#808080>" + baseName + "</color>";
                }
            }

            if (!isUnlocked)
            {
                textElement.text = "-";
                return;
            }

            LevelData data = SaveSystem.LoadLevelData(levelName);
            if (data.bestRank == 99)
            {
                textElement.text = "-"; // Unplayed
            }
            else
            {
                // Map numeric rank to Letter Grade (A, B, C, D)
                string rankLetter = "D";
                if (data.bestRank == 1) rankLetter = "A";
                else if (data.bestRank == 2) rankLetter = "B";
                else if (data.bestRank == 3) rankLetter = "C";

                textElement.text = rankLetter;
            }
        }
    }

    public void GoToLevel(string levelName)
    {
        if (!IsLevelUnlocked(levelName))
        {
            if (lockPanel != null)
            {
                lockPanel.SetActive(true);
            }
            return;
        }

        // By default, if they click a level from the level select, it's local co-op
        Network.GameModeManager.CurrentMode = Network.GameModeManager.GameMode.LocalCoop;
        SceneManager.LoadScene(levelName);
    }

    private bool IsLevelUnlocked(string levelName)
    {
        if (levelName == "Level_01") return true;

        int currentLevelNum;
        if (levelName.Contains("_"))
        {
            if (int.TryParse(levelName.Substring(levelName.IndexOf('_') + 1), out currentLevelNum))
            {
                if (currentLevelNum > 1)
                {
                    string previousLevel = string.Format("Level_{0:00}", currentLevelNum - 1);
                    LevelData prevData = SaveSystem.LoadLevelData(previousLevel);
                    if (prevData.bestRank == 99)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    private void CreateLockPanel()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            lockPanel = new GameObject("LockPanel");
            lockPanel.transform.SetParent(canvas.transform, false);
            
            // Add Image for background overlay
            Image bg = lockPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.85f);
            
            RectTransform rect = lockPanel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            // Add Window Box
            GameObject windowObj = new GameObject("WindowBox");
            windowObj.transform.SetParent(lockPanel.transform, false);
            Image windowBg = windowObj.AddComponent<Image>();
            windowBg.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            RectTransform windowRect = windowObj.GetComponent<RectTransform>();
            windowRect.sizeDelta = new Vector2(700, 400);
            windowRect.anchoredPosition = Vector2.zero;

            // Add Text
            GameObject textObj = new GameObject("LockText");
            textObj.transform.SetParent(windowObj.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "This level is locked!\nYou must complete previous levels first.";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 45;
            text.color = Color.white;
            text.enableWordWrapping = true;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(600, 200);
            textRect.anchoredPosition = new Vector2(0, 60);
            
            // Try to match font
            TextMeshProUGUI refText = canvas.GetComponentInChildren<TextMeshProUGUI>(true);
            if (refText != null)
            {
                text.font = refText.font;
                text.fontSharedMaterial = refText.fontSharedMaterial;
            }

            // Add Close Button
            GameObject btnObj = new GameObject("CloseButton");
            btnObj.transform.SetParent(windowObj.transform, false);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(250, 80);
            btnRect.anchoredPosition = new Vector2(0, -100);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => lockPanel.SetActive(false));

            GameObject btnTextObj = new GameObject("BtnText");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "OK";
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontSize = 40;
            btnText.color = Color.white;
            if (refText != null)
            {
                btnText.font = refText.font;
                btnText.fontSharedMaterial = refText.fontSharedMaterial;
            }

            lockPanel.SetActive(false);
        }
    }

    public void PlayOnline()
    {
        Network.GameModeManager.CurrentMode = Network.GameModeManager.GameMode.OnlineMultiplayer;
        if (mainPanel != null) mainPanel.SetActive(false);
        if (rankingPanel != null) rankingPanel.SetActive(false);
        if (customPanel != null) customPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
    }

    public void GoBackFromOnline()
    {
        if (Network.NetworkRunnerController.Instance != null && Network.NetworkRunnerController.Instance.Status != Network.ConnectionStatus.Disconnected)
        {
            Network.NetworkRunnerController.Instance.Shutdown();
        }
        ShowMainPanel();
    }

    // Helper methods for dynamic scene discovery
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