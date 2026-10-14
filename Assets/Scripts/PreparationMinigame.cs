using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PreparationMinigame : MonoBehaviour
{
    [Header("Panel")]
    public GameObject preparationPanel;

    [Header("UI References")]
    public RectTransform canvasRect;
    public RectTransform minigameArea;
    public Button redDotButton;
    public TextMeshProUGUI instructionText;

    [Header("Beer References")]
    public Beer correctBeer;
    public Beer wrongBeer;

    [Header("Minigame Settings")]
    public int requiredClicks = 4;
    public float dotVisibleTime = 1.5f;
    public float resultMessageDuration = 3f;
    public float screenPadding = 40f;

    private int successfulClicks;
    private bool preparationComplete;
    private bool waitingForClick;
    private bool preparationSuccessful;
    private Beer assignedBeer;
    private Coroutine dotRoutine;

    public bool IsPreparationComplete()
    {
        return preparationComplete;
    }

    public bool WasPreparationSuccessful()
    {
        return preparationSuccessful;
    }

    public Beer GetAssignedBeer()
    {
        return assignedBeer;
    }

    public bool CanPickUpBeer(Beer beer)
    {
        return preparationComplete &&
               beer != null &&
               beer == assignedBeer;
    }

    private void Awake()
    {
        if (redDotButton != null)
        {
            redDotButton.onClick.AddListener(OnRedDotClicked);
            redDotButton.gameObject.SetActive(false);
        }

        if (preparationPanel != null)
        {
            preparationPanel.SetActive(false);
        }
    }

    private void Start()
    {
        if (preparationPanel == null ||
            redDotButton == null ||
            instructionText == null ||
            correctBeer == null ||
            wrongBeer == null)
        {
            Debug.LogError(
                "PreparationMinigame: Assign the panel, UI, and beer references in the Inspector."
            );

            enabled = false;
            return;
        }

        if (minigameArea == null)
        {
            minigameArea =
                preparationPanel.GetComponent<RectTransform>();
        }

        if (canvasRect == null)
        {
            Canvas canvas =
                preparationPanel.GetComponentInParent<Canvas>();

            if (canvas != null)
            {
                canvasRect = canvas.GetComponent<RectTransform>();
            }
        }

        if (minigameArea == null || canvasRect == null)
        {
            Debug.LogError(
                "PreparationMinigame: Assign Minigame Area and Canvas Rect."
            );

            enabled = false;
            return;
        }

        BeginPreparation();
    }

    public void BeginPreparation()
    {
        if (preparationComplete)
            return;

        successfulClicks = 0;
        waitingForClick = false;

        // Show the preparation panel.
        if (preparationPanel != null)
        {
            preparationPanel.SetActive(true);
        }

        // Show and unlock the cursor so the player can click the dots.
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        instructionText.text =
            "Prepare the beer! Click the red dot before it disappears. (0/" +
            requiredClicks + ")";

        instructionText.gameObject.SetActive(true);

        if (dotRoutine != null)
        {
            StopCoroutine(dotRoutine);
        }

        dotRoutine = StartCoroutine(RunMinigame());
    }

    private IEnumerator RunMinigame()
    {
        while (successfulClicks < requiredClicks)
        {
            PositionRedDot();

            redDotButton.gameObject.SetActive(true);
            waitingForClick = true;

            float elapsed = 0f;

            while (elapsed < dotVisibleTime && waitingForClick)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            redDotButton.gameObject.SetActive(false);

            if (!waitingForClick)
            {
                successfulClicks++;

                instructionText.text =
                    "Prepare the beer! Click the red dot before it disappears. (" +
                    successfulClicks + "/" + requiredClicks + ")";

                yield return new WaitForSeconds(0.25f);
            }
            else
            {
                // Missing a dot means preparation fails.
                FinishPreparation(false);
                yield break;
            }
        }

        // Clicking all required dots means preparation succeeds.
        FinishPreparation(true);
    }

    private void PositionRedDot()
    {
        RectTransform dotRect =
            redDotButton.GetComponent<RectTransform>();

        float halfWidth = minigameArea.rect.width * 0.5f;
        float halfHeight = minigameArea.rect.height * 0.5f;

        float dotHalfWidth = dotRect.rect.width * 0.5f;
        float dotHalfHeight = dotRect.rect.height * 0.5f;

        float xMin =
            -halfWidth + screenPadding + dotHalfWidth;

        float xMax =
            halfWidth - screenPadding - dotHalfWidth;

        float yMin =
            -halfHeight + screenPadding + dotHalfHeight;

        float yMax =
            halfHeight - screenPadding - dotHalfHeight;

        // Keep the dot within the panel's rectangular area.
        dotRect.anchoredPosition = new Vector2(
            Random.Range(xMin, xMax),
            Random.Range(yMin, yMax)
        );
    }

    private void OnRedDotClicked()
    {
        if (!waitingForClick || preparationComplete)
            return;

        waitingForClick = false;
    }

    private void FinishPreparation(bool succeeded)
    {
        preparationComplete = true;
        preparationSuccessful = succeeded;
        waitingForClick = false;

        if (redDotButton != null)
        {
            redDotButton.gameObject.SetActive(false);
        }

        // Hide and lock the cursor after preparation ends.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (succeeded)
        {
            // Successful preparation assigns the wrong beer.
            assignedBeer = wrongBeer;

            instructionText.text =
                "PREPARATION SUCCESSFUL!\nPick up the assigned beer.";
        }
        else
        {
            // Failed preparation assigns the correct beer.
            assignedBeer = correctBeer;

            instructionText.text =
                "PREPARATION FAILED!\nPick up the assigned beer.";
        }

        Debug.Log(
            "Preparation finished. Result: " +
            (succeeded ? "SUCCESS" : "FAILURE") +
            ". Assigned beer: " +
            (assignedBeer != null
                ? assignedBeer.beerName
                : "No beer assigned")
        );

        StartCoroutine(HideResultMessage());
    }

    private IEnumerator HideResultMessage()
    {
        yield return new WaitForSeconds(resultMessageDuration);

        if (preparationPanel != null)
        {
            preparationPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (redDotButton != null)
        {
            redDotButton.onClick.RemoveListener(OnRedDotClicked);
        }
    }
}