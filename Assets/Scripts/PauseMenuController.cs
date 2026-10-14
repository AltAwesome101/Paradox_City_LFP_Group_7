using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The root GameObject of the pause menu panel.")]
    public GameObject pauseMenuPanel;

    [Tooltip("The Main Menu panel. Escape is ignored while it is open.")]
    public GameObject mainMenuPanel;

    [Header("Level Scene Names")]
    public string level1SceneName = "Artist_Hitler_Level";
    public string level2SceneName = "Forest_Scene";
    public string level3SceneName = "WrongBeerLevel";

    [Header("Background Camera (optional)")]
    public Camera pauseBackgroundCamera;

    public bool IsPaused { get; private set; }

    private bool MainMenuOpen =>
        mainMenuPanel != null && mainMenuPanel.activeSelf;

    private void Start()
    {
        SetPaused(false);
    }

    private void Update()
    {
        if (MainMenuOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void TogglePause()
    {
        SetPaused(!IsPaused);
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(paused);

        if (pauseBackgroundCamera != null)
            pauseBackgroundCamera.gameObject.SetActive(paused);

        bool showCursor = paused || MainMenuOpen;

        Cursor.lockState = showCursor
            ? CursorLockMode.None
            : CursorLockMode.Locked;

        Cursor.visible = showCursor;
    }

    public void OnResumeButton()
    {
        SetPaused(false);
        Time.timeScale = 1f;
    }

    // RESTART ONLY THE CURRENT GAME SCENE.
    // Do not reset LevelProgressManager or PlayerPrefs here.
    public void OnRestartButton()
    {
        string currentSceneName =
            SceneManager.GetActiveScene().name;

        Debug.Log(
            "[PauseMenuController] Restarting current scene: " +
            currentSceneName
        );

        Time.timeScale = 1f;

        SceneManager.LoadScene(currentSceneName);
    }

    public void OnQuitButton()
    {
        Debug.Log("Quitting game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void OnLevel1Button()
    {
        LoadLevel(level1SceneName);
    }

    public void OnLevel2Button()
    {
        LoadLevel(level2SceneName);
    }

    public void OnLevel3Button()
    {
        LoadLevel(level3SceneName);
    }

    private void LoadLevel(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning(
                "Scene name is empty. Check PauseMenuController " +
                "in the Inspector."
            );
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}