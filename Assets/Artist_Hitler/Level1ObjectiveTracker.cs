using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


public class Level1ObjectiveTracker : MonoBehaviour
{
    [Header("References")]
    public HitlerNPC hitler;
    public PaintingCanvas painting;

    [Header("Completion")]
    [Tooltip("Seconds to let Hitler's happy reaction play before leaving the level")]
    public float celebrationDuration = 3f;
    [Tooltip("Exact name of your Future hub scene, as it appears in File > Build Settings")]
    public string futureSceneName = "FutureScene";

    private void Start()
    {
        if (hitler == null || painting == null)
        {
            Debug.LogError("[Level1ObjectiveTracker] Missing a reference - assign both Hitler and Painting in the Inspector.");
            return;
        }

        painting.OnPaintingComplete += HandlePaintingComplete;

        
        hitler.BeginWalkingToEasel();
    }

    private void HandlePaintingComplete()
    {
        hitler.Celebrate();
        StartCoroutine(FinishLevelAfterDelay());
    }

    private IEnumerator FinishLevelAfterDelay()
    {
        yield return new WaitForSeconds(celebrationDuration);

        if (Application.CanStreamedLevelBeLoaded(futureSceneName))
        {
            SceneManager.LoadScene(futureSceneName);
        }
        else
        {
            Debug.LogError($"[Level1ObjectiveTracker] Can't find a scene called \"{futureSceneName}\". " +
                            "Check the spelling matches exactly, and that it's added under File > Build Settings > Scenes In Build.");
        }
    }
}