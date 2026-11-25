using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    [Header("Optional Scene Overrides")]
    [Tooltip("Leave empty to reload the current active scene when Continue is clicked.")]
    public string continueSceneName = "";

    [Tooltip("Leave empty to close the application when Quit is clicked, or enter a Main Menu scene name to load instead.")]
    public string quitToSceneName = "";

    // Hook this up to your Continue Button's OnClick() event
    public void ContinueGame()
    {
        // Reset global time, audio, and cursor states before reloading
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        string targetScene = string.IsNullOrEmpty(continueSceneName)
            ? SceneManager.GetActiveScene().name
            : continueSceneName;

        SceneManager.LoadScene(targetScene);
    }

    // Hook this up to your Quit Button's OnClick() event
    public void QuitGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (!string.IsNullOrEmpty(quitToSceneName))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(quitToSceneName);
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}