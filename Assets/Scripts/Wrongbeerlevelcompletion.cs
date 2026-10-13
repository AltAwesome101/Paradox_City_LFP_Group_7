using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


public class WrongBeerLevelCompletion : MonoBehaviour
{
    [Header("Scene to return to")]
    [Tooltip("EXACT name of the future scene. It must be added in File > Build Settings.")]
    [SerializeField] private string futureSceneName = "FutureScene";
    [Tooltip("Seconds to wait after completion before the scene loads (room for a sound / celebration).")]
    [SerializeField] private float returnDelay = 3f;

    [Header("Progress")]
    [Tooltip("Must match the name in LevelProgressManager's playableLevels list.")]
    [SerializeField] private string levelProgressId = "WrongBeerLevel";

    [Header("Events")]
    [Tooltip("Fires once when the correct beer is given. Hook up sounds, UI, effects here.")]
    [SerializeField] private UnityEvent onLevelCompleted;

    private bool _completed;

    
    public void WrongBeerGiven()
    {
        if (_completed) return;
        _completed = true;

        
        if (WorldStateManager.Instance != null)
            WorldStateManager.Instance.SetLevelCompleted(LevelId.WrongBeer, true);
        else
            Debug.LogWarning("[WrongBeerLevelCompletion] No WorldStateManager found, so the hub cannot change.");

        if (LevelProgressManager.Instance != null)
            LevelProgressManager.Instance.CompleteLevel(levelProgressId);

        onLevelCompleted?.Invoke();
        StartCoroutine(ReturnToFutureScene());
    }

    private IEnumerator ReturnToFutureScene()
    {
        if (returnDelay > 0f)
            yield return new WaitForSecondsRealtime(returnDelay);

        if (!Application.CanStreamedLevelBeLoaded(futureSceneName))
        {
            Debug.LogError("[WrongBeerLevelCompletion] Scene '" + futureSceneName +
                           "' can't be loaded. Check the name and add it in File > Build Settings.");
            yield break;
        }

        SceneManager.LoadScene(futureSceneName);
    }

    
    [ContextMenu("TEST: Complete Wrong Beer level")]
    private void TestComplete()
    {
        WrongBeerGiven();
    }
}