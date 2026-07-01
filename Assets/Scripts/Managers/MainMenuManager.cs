using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Hàm này sẽ nhận tên scene từ nút bấm và load scene đó
    public void GoToLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }
}