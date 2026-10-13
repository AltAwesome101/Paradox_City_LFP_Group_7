using UnityEngine;
using UnityEngine.SceneManagement;


public class PauseMenuController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The root GameObject of the pause menu panel (buttons, dimmer background, etc.)")]
    public GameObject pauseMenuPanel;

    [Header("Level Scene Names (edit these to match Build Settings)")]
    [Tooltip("Exact scene name for Level 1 - Artist Hitler")]
    public string level1SceneName = "ArtistHitler";

    [Tooltip("Exact scene name for Level 2 - Apple Forest")]
    public string level2SceneName = "Forest_Scene";

    [Tooltip("Exact scene name for Level 3 - The Wrong Beer")]
    public string level3SceneName = "TheWrongBeer";

    [Header("Background Camera (optional)")]
    [Tooltip("Camera that shows the moving city background behind the pause menu. " +
             "Leave empty if you're not using this feature.")]
    public Camera pauseBackgroundCamera;

    public bool IsPaused { get; private set; }

    void Start()
    {
        
        SetPaused(false);
    }

    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
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


        //Time.timeScale = paused ? 0f : 1f;
        if (paused) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    

    public void OnResumeButton()
    {
        SetPaused(false);
    }

    public void OnQuitButton()
    {
        Debug.Log("Quitting game...");
        Application.Quit();

#if UNITY_EDITOR
        // Application.Quit() does nothing in the Editor, so this stops Play Mode instead.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void OnLevel1Button()
    {
        LoadLevel(level1SceneName);
    }

    public void OnLevel2Button()
    {
        LoadLevel("Forest_Scene");
    }

    public void OnLevel3Button()
    {
        LoadLevel(level3SceneName);
    }

    private void LoadLevel(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("Scene name is empty - set it in the Inspector on PauseMenuController.");
            return;
        }

       
        //Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}