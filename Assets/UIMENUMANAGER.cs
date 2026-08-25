using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

public class UIMENUMANAGER : MonoBehaviour
{
    public GameObject MainMenuPanel;
    public GameObject GameUIPanel;
    public GameObject mainplayer;

    private void Start()
    {
        Time.timeScale = 0f;
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;
        MainMenuPanel.SetActive(false);
        GameUIPanel.SetActive(true);
        mainplayer.SetActive(true);
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
