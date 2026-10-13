using UnityEngine;

public class UIMENUMANAGER : MonoBehaviour
{
    public GameObject MainMenuPanel;
    public GameObject GameUIPanel;
    public GameObject mainplayer;

    
    private static bool s_menuShown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_menuShown = false;
    }

    private void Start()
    {
        if (s_menuShown)
        {
            // Menu was already shown earlier this playthrough: go straight into gameplay.
            StartGameplay();
            return;
        }

        Time.timeScale = 0f;

        // Main menu is showing: the mouse must be usable
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        MainMenuPanel.SetActive(true);
        GameUIPanel.SetActive(false);
    }

    public void PlayGame()
    {
        s_menuShown = true;
        StartGameplay();
    }

    private void StartGameplay()
    {
        Time.timeScale = 1f;
        MainMenuPanel.SetActive(false);
        GameUIPanel.SetActive(true);
        mainplayer.SetActive(true);

        // Gameplay: hide and lock the mouse
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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