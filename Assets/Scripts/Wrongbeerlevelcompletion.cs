using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


public class WrongBeerLevelCompletion : MonoBehaviour
{
    [Header("Game Win Scene")]
    [Tooltip("Exact name of the Game Win scene in Build Settings.")]
    [SerializeField] private string gameWinSceneName = "GameWin";

    [Tooltip("How long the Game Win scene is displayed before returning.")]
    [SerializeField] private float gameWinDisplayDuration = 3f;

    [Header("Future Scene")]
    [Tooltip("Exact name of the future scene in Build Settings.")]
    [SerializeField] private string futureSceneName = "FutureScene";

    [Header("Progress")]
    [Tooltip("Must match the playable level ID.")]
    [SerializeField] private string levelProgressId = "WrongBeerLevel";

    [Header("Events")]
    [Tooltip("Runs once when the wrong beer causes the NPC to miss the meeting.")]
    [SerializeField] private UnityEvent onLevelCompleted;

    private bool completed;

    public void WrongBeerGiven()
    {
        if (completed)
            return;

        completed = true;

        Debug.Log(
            "[WrongBeerLevelCompletion] Wrong beer outcome confirmed. Game Win!"
        );

        // Mark the level as completed so the future hub can show
        // the consequences of the player's actions.
        if (WorldStateManager.Instance != null)
        {
            WorldStateManager.Instance.SetLevelCompleted(
                LevelId.WrongBeer, true
            );
        }
        else
        {
            Debug.LogWarning(
                "[WrongBeerLevelCompletion] No WorldStateManager found."
            );
        }

        if (LevelProgressManager.Instance != null)
        {
            LevelProgressManager.Instance.CompleteLevel(levelProgressId);
        }
        else
        {
            Debug.LogWarning(
                "[WrongBeerLevelCompletion] No LevelProgressManager found."
            );
        }

        onLevelCompleted?.Invoke();

        StartCoroutine(ShowGameWinThenReturnToFuture());
    }

    private IEnumerator ShowGameWinThenReturnToFuture()
    {
        Time.timeScale = 1f;

        if (!Application.CanStreamedLevelBeLoaded(gameWinSceneName))
        {
            Debug.LogError(
                "[WrongBeerLevelCompletion] Game Win scene '" +
                gameWinSceneName +
                "' cannot be loaded. Check Build Settings."
            );

            yield break;
        }

        // Show the Game Win scene first.
        SceneManager.LoadScene(gameWinSceneName);

        // Allow the player to see the ending before returning to the hub.
        if (gameWinDisplayDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                gameWinDisplayDuration
            );
        }

        if (!Application.CanStreamedLevelBeLoaded(futureSceneName))
        {
            Debug.LogError(
                "[WrongBeerLevelCompletion] Future scene '" +
                futureSceneName +
                "' cannot be loaded. Check Build Settings."
            );

            yield break;
        }

        SceneManager.LoadScene(futureSceneName);
    }

    [ContextMenu("TEST: Wrong Beer Game Win")]
    private void TestComplete()
    {
        WrongBeerGiven();
    }
}