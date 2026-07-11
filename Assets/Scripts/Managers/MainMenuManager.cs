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

    [Header("Ranking UI Elements")]
    public TextMeshProUGUI level1RankText;
    public TextMeshProUGUI level2RankText;
    public TextMeshProUGUI level3RankText;
    public TextMeshProUGUI level4RankText;

    private void Start()
    {
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
    }

    public void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (rankingPanel != null) rankingPanel.SetActive(false);
        if (customPanel != null) customPanel.SetActive(false);
    }

    private void UpdateRankText(TextMeshProUGUI textElement, string levelName)
    {
        if (textElement != null)
        {
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
        // By default, if they click a level from the level select, it's local co-op
        Network.GameModeManager.CurrentMode = Network.GameModeManager.GameMode.LocalCoop;
        SceneManager.LoadScene(levelName);
    }

    public void PlayOnline()
    {
        Network.GameModeManager.CurrentMode = Network.GameModeManager.GameMode.OnlineMultiplayer;
        SceneManager.LoadScene("LobbyScene");
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