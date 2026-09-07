using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("Pause Menu UI")]
    public GameObject pauseMenu;

    [Header("Player")]
    [Tooltip("Optional. Drag the PlayerScript here so player movement is disabled while paused.")]
    public PlayerScript player;

    [Header("Future Scene")]
    public string futureSceneName = "FutureScene";

    [Header("Debug")]
    public bool debugLogging = true;

    private bool isPaused = false;

    private void Start()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;

        Time.timeScale = 0f;

        if (pauseMenu != null)
            pauseMenu.SetActive(true);

        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        
        if (player != null)
            player.SetControl(false);

        if (debugLogging)
            Debug.Log("[PauseMenu] Game Paused.");
    }

    public void ResumeGame()
    {
        isPaused = false;

        Time.timeScale = 1f;

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        
        if (player != null)
            player.SetControl(true);

        if (debugLogging)
            Debug.Log("[PauseMenu] Game Resumed.");
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        isPaused = false;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        string currentScene =
            SceneManager.GetActiveScene().name;

        SceneManager.LoadScene(currentScene);
    }

    public void ReturnToFuture()
    {
        Time.timeScale = 1f;
        isPaused = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (string.IsNullOrEmpty(futureSceneName))
        {
            Debug.LogError(
                "[PauseMenu] Future Scene Name is empty!"
            );

            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(futureSceneName))
        {
            Debug.LogError(
                $"[PauseMenu] Can't find scene \"{futureSceneName}\". " +
                "Make sure it is added to Build Settings."
            );

            return;
        }

        SceneManager.LoadScene(futureSceneName);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}