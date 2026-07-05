using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject rankingPanel;

    [Header("Ranking UI Elements")]
    public TextMeshProUGUI level1RankText;
    public TextMeshProUGUI level2RankText;
    public TextMeshProUGUI level3RankText;

    private void Start()
    {
        // Ensure only the main panel is open at start
        ShowMainPanel();
    }

    public void ShowRankingPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (rankingPanel != null) rankingPanel.SetActive(true);

        UpdateRankText(level1RankText, "Level_01");
        UpdateRankText(level2RankText, "Level_02");
        UpdateRankText(level3RankText, "Level_03");
    }

    public void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (rankingPanel != null) rankingPanel.SetActive(false);
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
                textElement.text = data.bestRank.ToString();
            }
        }
    }

    public void GoToLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }
}