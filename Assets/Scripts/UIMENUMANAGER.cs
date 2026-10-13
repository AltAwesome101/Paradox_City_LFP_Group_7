using UnityEngine;
using TMPro;

public class UIMENUMANAGER : MonoBehaviour
{
    public GameObject MainMenuPanel;
    public GameObject GameUIPanel;
    public GameObject mainplayer;

    [Header("Intro Cinematic Options")]
    public AudioClip introMusic;
    public TMP_FontAsset introFont;

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

        // Hide the main menu UI so it doesn't overlap with the cinematic
        MainMenuPanel.SetActive(false);

        // Play the intro cinematic and proceed to gameplay once complete or skipped
        ParadoxIntroCinematic.Play(StartGameplay, introMusic, introFont);
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