using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Level1ObjectiveTracker : MonoBehaviour
{
    [Header("References")]
    public HitlerNPC hitler;
    [Tooltip("All 3 paintings the player must fix to complete the level.")]
    public List<PaintingCanvas> paintings = new List<PaintingCanvas>();

    [Header("Completion")]
    [Tooltip("Seconds to let Hitler's happy reaction play before leaving the level")]
    public float celebrationDuration = 3f;
    [Tooltip("Exact name of your Future hub scene, as it appears in File > Build Settings")]
    public string futureSceneName = "FutureScene";

    [Header("Debug")]
    public bool debugLogging = true;

    private int completedCount;

    private void Start()
    {
        if (hitler == null || paintings == null || paintings.Count == 0)
        {
            Debug.LogError("[Level1ObjectiveTracker] Missing references - assign Hitler and at least one painting.");
            return;
        }

        foreach (PaintingCanvas painting in paintings)
        {
            if (painting == null) continue;
            painting.OnPaintingComplete += HandleOnePaintingComplete;
        }

        hitler.BeginWalkingToEasel();
    }

    private void OnDestroy()
    {
        if (paintings == null) return;

        foreach (PaintingCanvas painting in paintings)
        {
            if (painting == null) continue;
            painting.OnPaintingComplete -= HandleOnePaintingComplete;
        }
    }

    private void HandleOnePaintingComplete()
    {
        completedCount++;

        if (debugLogging)
            Debug.Log($"[Level1ObjectiveTracker] Painting corrected ({completedCount}/{paintings.Count}).");

        if (completedCount >= paintings.Count)
        {
            hitler.Celebrate();
            StartCoroutine(FinishLevelAfterDelay());
        }
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