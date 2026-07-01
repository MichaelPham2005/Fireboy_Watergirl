using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void GoToLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }
}