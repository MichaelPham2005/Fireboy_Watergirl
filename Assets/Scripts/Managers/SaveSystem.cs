using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelData
{
    public List<float> topTimes = new List<float>();
    public int bestRank = SaveSystem.UNPLAYED_RANK; // 99 means unranked/unplayed. 1 is best.
}

public static class SaveSystem
{
    public const int UNPLAYED_RANK = 99;

    public static void SaveTime(string levelName, float time)
    {
        LevelData data = LoadLevelData(levelName);
        
        data.topTimes.Add(time);
        // Sort ascending because lower time is better
        data.topTimes.Sort();
        
        // Keep only top 5 times
        if (data.topTimes.Count > 5)
        {
            data.topTimes.RemoveAt(data.topTimes.Count - 1);
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("LevelData_" + levelName, json);
        PlayerPrefs.Save();
    }

    public static void SaveRank(string levelName, int newRank)
    {
        LevelData data = LoadLevelData(levelName);
        
        // Lower number is better (Rank 1 > Rank 2)
        if (newRank < data.bestRank)
        {
            data.bestRank = newRank;
            
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString("LevelData_" + levelName, json);
            PlayerPrefs.Save();
        }
    }

    public static LevelData LoadLevelData(string levelName)
    {
        string json = PlayerPrefs.GetString("LevelData_" + levelName, "");
        if (string.IsNullOrEmpty(json))
        {
            return new LevelData();
        }

        return JsonUtility.FromJson<LevelData>(json);
    }
}
