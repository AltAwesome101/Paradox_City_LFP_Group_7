using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Put this on an empty GameObject in Artist_Hittler_Level.
/// Call CompleteLevel() when the last painting is finished
/// (from your Level 1 Objective Tracker, or a UnityEvent in the Inspector).
/// No score, reward screen or message is shown - the consequence is
/// discovered in the hub, as per the hypothesis.
/// </summary>
public class ArtistHitlerLevelCompletion : MonoBehaviour
{
    [SerializeField] private string futureSceneName = "Future_Scene";
    [Tooltip("Optional pause before returning, in seconds.")]
    [SerializeField] private float delayBeforeReturn = 1f;

    private bool _completing;

    public void CompleteLevel()
    {
        if (_completing) return; // protects against being called twice
        _completing = true;

        WorldStateManager.Instance.SetLevelCompleted(LevelId.ArtistHitler);
        Invoke(nameof(LoadFutureScene), delayBeforeReturn);
    }

    private void LoadFutureScene()
    {
        SceneManager.LoadScene(futureSceneName);
    }
}