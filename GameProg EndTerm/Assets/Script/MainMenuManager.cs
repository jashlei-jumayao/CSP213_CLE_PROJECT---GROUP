using UnityEngine;
using UnityEngine.SceneManagement; // Required to change scenes

public class MainMenuManager : MonoBehaviour
{
    // Must be public so the buttons can access it
    public void PlayGame()
    {
        // Loads the next scene in the Build Settings queue
        // You can also use a string name like: SceneManager.LoadScene("GameScene");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit!"); // This lets you know it works in the Editor
        Application.Quit();      // This only closes the actual built application
    }
}