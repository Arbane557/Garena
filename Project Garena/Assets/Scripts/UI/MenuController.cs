using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Scene")]
    public string gameplaySceneName = "Gameplay";

    public void StartGame()
    {
        if (!string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

