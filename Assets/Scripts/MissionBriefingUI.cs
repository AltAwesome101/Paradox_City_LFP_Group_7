using UnityEngine;
using UnityEngine.UI;

public class MissionBriefingUI : MonoBehaviour
{
    [Header("Briefing UI")]
    public GameObject missionBriefingPanel;
    public Button closeBriefingButton;
    public Button openBriefingButton;

    [Header("Player Control")]
    public MonoBehaviour playerMovementScript;

    private bool isBriefingOpen = false;

    private void Start()
    {
        if (missionBriefingPanel == null)
        {
            Debug.LogError(
                "MissionBriefingUI: Mission Briefing Panel is not assigned."
            );
            return;
        }

        if (closeBriefingButton != null)
        {
            closeBriefingButton.onClick.AddListener(CloseBriefing);
        }

        if (openBriefingButton != null)
        {
            openBriefingButton.onClick.AddListener(OpenBriefing);
        }

        // Show the briefing at the beginning of the level.
        OpenBriefing();
    }

    public void OpenBriefing()
    {
        if (missionBriefingPanel == null || isBriefingOpen)
        {
            return;
        }

        missionBriefingPanel.SetActive(true);
        isBriefingOpen = true;

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseBriefing()
    {
        if (missionBriefingPanel == null || !isBriefingOpen)
        {
            return;
        }

        missionBriefingPanel.SetActive(false);
        isBriefingOpen = false;

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        if (closeBriefingButton != null)
        {
            closeBriefingButton.onClick.RemoveListener(CloseBriefing);
        }

        if (openBriefingButton != null)
        {
            openBriefingButton.onClick.RemoveListener(OpenBriefing);
        }
    }
}