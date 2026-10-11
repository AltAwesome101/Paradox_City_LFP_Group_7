using UnityEngine;

public class ResetProgressButton : MonoBehaviour
{
    public void ResetProgress()
    {
        if (LevelProgressManager.Instance != null)
        {
            LevelProgressManager.Instance.ResetAllProgress();
            Debug.Log("Progress reset! All levels are unplayed.");
        }
        else
        {
            Debug.LogError("LevelProgressManager not found!");
        }
    }
}
