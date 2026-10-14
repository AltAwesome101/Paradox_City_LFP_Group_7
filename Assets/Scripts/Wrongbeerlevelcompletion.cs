using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


public class WrongBeerLevelCompletion : MonoBehaviour
{
    [Header("Game Win Scene")]
    [SerializeField] private string gameWinSceneName = "GameWin";
    [SerializeField] private float gameWinDisplayDuration = 3f;

    [Header("Future Scene")]
    [SerializeField] private string futureSceneName = "FutureScene";

    [Header("Progress")]
    [SerializeField] private string levelProgressId = "WrongBeerLevel";

    [Header("Events")]
    [SerializeField] private UnityEvent onLevelCompleted;

    private bool completed;

    public void WrongBeerGiven()
    {
        if (completed) return;

        completed = true;

        // Keep this object alive when Unity loads the Game Win scene.
        DontDestroyOnLoad(gameObject);

        Debug.Log("[WrongBeerLevelCompletion] Wrong beer outcome confirmed. Game Win!");

        if (WorldStateManager.Instance != null)
        {
            WorldStateManager.Instance.SetLevelCompleted(
                LevelId.WrongBeer, true);
        }
        else
        {
            Debug.LogWarning(
                "[WrongBeerLevelCompletion] No WorldStateManager found.");
        }

        if (LevelProgressManager.Instance != null)
        {
            LevelProgressManager.Instance.CompleteLevel(levelProgressId);
        }
        else
        {
            Debug.LogWarning(
                "[WrongBeerLevelCompletion] No LevelProgressManager found.");
        }

        onLevelCompleted?.Invoke();

        StartCoroutine(ShowGameWinThenReturnToFuture());
    }

    private IEnumerator ShowGameWinThenReturnToFuture()
    {
        Time.timeScale = 1f;

        // Check both scenes before beginning the transition.
        if (!Application.CanStreamedLevelBeLoaded(gameWinSceneName))
        {
            Debug.LogError(
                "[WrongBeerLevelCompletion] Game Win scene '" +
                gameWinSceneName + "' cannot be loaded. Check its name " +
                "and add it to Build Profiles.");

            Destroy(gameObject);
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(futureSceneName))
        {
            Debug.LogError(
                "[WrongBeerLevelCompletion] Future scene '" +
                futureSceneName + "' cannot be loaded. Check its name " +
                "and add it to Build Profiles.");

            Destroy(gameObject);
            yield break;
        }

        Debug.Log("[WrongBeerLevelCompletion] Loading Game Win scene.");

        SceneManager.LoadScene(gameWinSceneName);

        yield return new WaitForSecondsRealtime(gameWinDisplayDuration);

        Debug.Log("[WrongBeerLevelCompletion] Returning to FutureScene.");

        SceneManager.LoadScene(futureSceneName);

        // Clean up this persistent object after returning to the hub.
        Destroy(gameObject);
    }

    [ContextMenu("TEST: Wrong Beer Game Win")]
    private void TestComplete()
    {
        WrongBeerGiven();
    }
}