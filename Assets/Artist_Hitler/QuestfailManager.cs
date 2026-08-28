using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using StarterAssets;


public class QuestFailManager : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("Hitler's NPCVision component - fires OnTargetSpotted when Awareness hits 1")]
    public NPCVision hitlerVision;

    [Header("UI")]
    [Tooltip("The 'Quest Failed' panel GameObject - should be disabled in the scene by default")]
    public GameObject questFailedPanel;

    [Header("Timing")]
    [Tooltip("Seconds to show the panel before the scene restarts")]
    public float delayBeforeRestart = 2f;

    [Header("Player Lock (optional)")]
    [Tooltip("If assigned, player movement is disabled the moment the quest fails")]
    public ThirdPersonController thirdPersonController;
    public CharacterController characterController;

    private bool hasFailed;

    private void Start()
    {
        if (hitlerVision == null)
        {
            Debug.LogError("[QuestFailManager] No NPCVision assigned - can't detect a fail state.");
            return;
        }

        hitlerVision.OnTargetSpotted += HandleSpotted;

        if (questFailedPanel != null)
        {
            questFailedPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (hitlerVision != null)
        {
            hitlerVision.OnTargetSpotted -= HandleSpotted;
        }
    }

    private void HandleSpotted()
    {
        if (hasFailed) return; 
        hasFailed = true;

        Debug.Log("[QuestFailManager] Hitler fully spotted the player - quest failed.");

        if (thirdPersonController != null) thirdPersonController.enabled = false;
        if (characterController != null) characterController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (questFailedPanel != null)
        {
            questFailedPanel.SetActive(true);
        }

        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeRestart);

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }
}