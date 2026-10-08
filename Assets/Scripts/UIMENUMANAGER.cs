using UnityEngine;

public class UIMENUMANAGER : MonoBehaviour
{
    public GameObject MainMenuPanel;
    public GameObject GameUIPanel;
    public GameObject mainplayer;

    private void Start()
    {
        Time.timeScale = 0f;

        // Main menu is showing: the mouse must be usable
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void PlayGame()
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