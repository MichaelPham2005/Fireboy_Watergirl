using UnityEngine;
using UnityEditor;

public class OrganizeAnimations
{
    [MenuItem("Tools/Organize Animations")]
    public static void Organize()
    {
        string baseDir = "Assets/Animation";
        
        // 1. Create folders safely
        if (!AssetDatabase.IsValidFolder(baseDir + "/Environment"))
            AssetDatabase.CreateFolder(baseDir, "Environment");
        if (!AssetDatabase.IsValidFolder(baseDir + "/Fireboy"))
            AssetDatabase.CreateFolder(baseDir, "Fireboy");
        if (!AssetDatabase.IsValidFolder(baseDir + "/Watergirl"))
            AssetDatabase.CreateFolder(baseDir, "Watergirl");

        // 2. Move Environment files
        Move(baseDir, "Blue_Door_Open.anim", "Environment/Blue_Door_Open.anim");
        Move(baseDir, "Blue_door.controller", "Environment/Blue_door.controller");
        Move(baseDir, "Red_Door_Open.anim", "Environment/Red_Door_Open.anim");
        Move(baseDir, "Red_door.controller", "Environment/Red_door.controller");
        Move(baseDir, "wind_motion.anim", "Environment/wind_motion.anim");
        Move(baseDir, "wind_visual.controller", "Environment/wind_visual.controller");

        // 3. Rename and move Fireboy files
        Move(baseDir, "Body_Visual.controller", "Fireboy/Fireboy_Body.controller");
        Move(baseDir, "Head_Visual.controller", "Fireboy/Fireboy_Head.controller");
        Move(baseDir, "Death.anim", "Fireboy/Fireboy_Body_death.anim");
        Move(baseDir, "FireboyHead_fall.anim", "Fireboy/Fireboy_Head_fall.anim");
        Move(baseDir, "FireboyHead_jump.anim", "Fireboy/Fireboy_Head_jump.anim");
        Move(baseDir, "Fireboy_idle.anim", "Fireboy/Fireboy_Body_idle.anim");
        Move(baseDir, "Fireboy_run.anim", "Fireboy/Fireboy_Body_run.anim");
        Move(baseDir, "Head_death.anim", "Fireboy/Fireboy_Head_death.anim");
        Move(baseDir, "Head_idle.anim", "Fireboy/Fireboy_Head_idle.anim");
        Move(baseDir, "Head_run.anim", "Fireboy/Fireboy_Head_run.anim");
        
        // Force Unity to save the new locations so we can duplicate from them
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 4. Duplicate everything for Watergirl
        DuplicateFireboyToWatergirl(baseDir);

        Debug.Log("Animations successfully organized and duplicated!");
    }

    private static void Move(string baseDir, string oldName, string newRelativePath)
    {
        string oldPath = baseDir + "/" + oldName;
        string newPath = baseDir + "/" + newRelativePath;
        if (AssetDatabase.LoadAssetAtPath<Object>(oldPath) != null)
        {
            string result = AssetDatabase.MoveAsset(oldPath, newPath);
            if (!string.IsNullOrEmpty(result)) Debug.LogError("Move error: " + result);
        }
    }

    private static void DuplicateFireboyToWatergirl(string baseDir)
    {
        string[] files = new string[]
        {
            "Fireboy_Body.controller",
            "Fireboy_Head.controller",
            "Fireboy_Body_death.anim",
            "Fireboy_Head_fall.anim",
            "Fireboy_Head_jump.anim",
            "Fireboy_Body_idle.anim",
            "Fireboy_Body_run.anim",
            "Fireboy_Head_death.anim",
            "Fireboy_Head_idle.anim",
            "Fireboy_Head_run.anim"
        };

        foreach (var file in files)
        {
            string oldPath = baseDir + "/Fireboy/" + file;
            string newName = file.Replace("Fireboy", "Watergirl");
            string newPath = baseDir + "/Watergirl/" + newName;
            
            if (AssetDatabase.LoadAssetAtPath<Object>(oldPath) != null && AssetDatabase.LoadAssetAtPath<Object>(newPath) == null)
            {
                AssetDatabase.CopyAsset(oldPath, newPath);
            }
        }
    }
}
