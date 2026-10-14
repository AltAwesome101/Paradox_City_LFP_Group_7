using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("Pause Menu UI")]
    public GameObject pauseMenu;

    [Header("Player")]
    public PlayerScript player;

    [Header("Preparation Minigame")]
    public PreparationMinigame preparationMinigame;

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

        UpdateCursorState();
    }

    private void Update()
    {
        // Keep the cursor state correct even if another script
        // changes it while the preparation panel is open.
        UpdateCursorState();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Escape belongs to the painting if one is open (or was just closed this frame)
            if (PaintingCanvas.AnyInteracting || PaintingCanvas.LastExitFrame == Time.frameCount)
                return;

            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    private bool IsPreparationActive()
    {
        return preparationMinigame != null &&
               !preparationMinigame.IsPreparationComplete();
    }

    private void UpdateCursorState()
    {
        // The pause menu and preparation minigame both need
        // a visible, unlocked cursor.
        if (isPaused || IsPreparationActive() || PaintingCanvas.AnyInteracting)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pauseMenu != null)
            pauseMenu.SetActive(true);

        UpdateCursorState();

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

        UpdateCursorState();

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

        if (LevelProgressManager.Instance != null)
        {
            LevelProgressManager.Instance.ResetAllProgress();

            if (debugLogging)
                Debug.Log(
                    "[PauseMenu] All level progress reset."
                );
        }
        else
        {
            Debug.LogWarning(
                "[PauseMenu] LevelProgressManager not found."
            );
        }

        if (debugLogging)
            Debug.Log(
                "[PauseMenu] Restarting: " + currentScene
            );

        SceneManager.LoadScene(currentScene);
    }

    public void ReturnToFuture()
    {
        Time.timeScale = 1f;
        isPaused = false;

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
                "[PauseMenu] Can't find scene \"" +
                futureSceneName +
                "\". Make sure it is added to Build Settings."
            );
            return;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.LoadScene(futureSceneName);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}