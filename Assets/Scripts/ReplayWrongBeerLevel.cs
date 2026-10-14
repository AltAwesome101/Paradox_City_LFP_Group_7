using UnityEngine;
using UnityEngine.SceneManagement;

public class ReplayWrongBeerLevel : MonoBehaviour
{
    [Header("Scene Settings")]
    public string levelSceneName = "WrongBeerLevel";

    public void ReplayLevel()
    {
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(levelSceneName))
        {
            Debug.LogError(
                "ReplayWrongBeerLevel: The level scene name is empty."
            );
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(levelSceneName))
        {
            Debug.LogError(
                "ReplayWrongBeerLevel: Scene '" +
                levelSceneName +
                "' could not be loaded. Check its name and Build Settings."
            );
            return;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        SceneManager.LoadScene(levelSceneName);
    }
}