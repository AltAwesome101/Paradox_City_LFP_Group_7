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

    [Header("Player Detection")]
    public Transform player;
    public float detectionDistance = 3f;

    [Header("Minigame Settings")]
    public int requiredClicks = 3;
    public float preparationStartDelay = 5f;
    public float dotVisibleTime = 1.5f;
    public float resultMessageDuration = 3f;
    public float screenPadding = 40f;

    private int successfulClicks;
    private bool preparationComplete;
    private bool waitingForClick;
    private bool preparationSuccessful;
    private bool preparationStarted;
    private bool minigameRunning;

    private Beer assignedBeer;
    private Coroutine dotRoutine;

    public bool IsPreparationComplete()
    {
        return preparationComplete;
    }

    // Used by FPController to stop movement only
    // while the preparation sequence is running.
    public bool IsMinigameRunning()
    {
        return minigameRunning;
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
            wrongBeer == null ||
            player == null)
        {
            Debug.LogError(
                "PreparationMinigame: Assign the panel, UI, " +
                "both beers, and Player in the Inspector."
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
                "PreparationMinigame: Assign Minigame Area " +
                "and Canvas Rect."
            );

            enabled = false;
            return;
        }

        preparationPanel.SetActive(false);
        redDotButton.gameObject.SetActive(false);
        instructionText.gameObject.SetActive(false);

        preparationComplete = false;
        preparationStarted = false;
        minigameRunning = false;
    }

    private void Update()
    {
        if (preparationComplete || preparationStarted)
        {
            return;
        }

        if (PlayerIsNearBothBeers())
        {
            BeginPreparation();
        }
    }

    private bool PlayerIsNearBothBeers()
    {
        if (player == null ||
            correctBeer == null ||
            wrongBeer == null)
        {
            return false;
        }

        float distanceToCorrectBeer =
            Vector3.Distance(
                player.position,
                correctBeer.transform.position
            );

        float distanceToWrongBeer =
            Vector3.Distance(
                player.position,
                wrongBeer.transform.position
            );

        return distanceToCorrectBeer <= detectionDistance &&
               distanceToWrongBeer <= detectionDistance;
    }

    public void BeginPreparation()
    {
        if (preparationComplete || preparationStarted)
        {
            return;
        }

        preparationStarted = true;
        minigameRunning = true;
        successfulClicks = 0;
        waitingForClick = false;
        assignedBeer = null;

        preparationPanel.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        instructionText.gameObject.SetActive(true);
        instructionText.text =
            "Prepare the beer! Get ready to click the red dot. " +
            "(0/" + requiredClicks + ")";

        if (dotRoutine != null)
        {
            StopCoroutine(dotRoutine);
        }

        dotRoutine = StartCoroutine(RunMinigame());
    }

    private IEnumerator RunMinigame()
    {
        // Wait 10 seconds after showing the panel.
        yield return new WaitForSeconds(preparationStartDelay);

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
                    "Prepare the beer! " +
                    "" +
                    "" +
                    "Click the red dot before " +
                    "it disappears. (" + successfulClicks +
                    "/" + requiredClicks + ")";

                yield return new WaitForSeconds(0.25f);
            }
            else
            {
                FinishPreparation(false);
                yield break;
            }
        }

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

        if (xMin > xMax || yMin > yMax)
        {
            Debug.LogWarning(
                "PreparationMinigame: The minigame area is too " +
                "small for the red dot and screen padding."
            );
        }

        dotRect.anchoredPosition = new Vector2(
            Random.Range(Mathf.Min(xMin, xMax), Mathf.Max(xMin, xMax)),
            Random.Range(Mathf.Min(yMin, yMax), Mathf.Max(yMin, yMax))
        );
    }

    private void OnRedDotClicked()
    {
        if (!waitingForClick || preparationComplete)
        {
            return;
        }

        waitingForClick = false;
    }

    private void FinishPreparation(bool succeeded)
    {
        preparationComplete = true;
        preparationSuccessful = succeeded;
        waitingForClick = false;
        minigameRunning = false;

        if (redDotButton != null)
        {
            redDotButton.gameObject.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (succeeded)
        {
            assignedBeer = wrongBeer;

            instructionText.text =
                "PREPARATION SUCCESSFUL!\n" +
                "Pick up the assigned beer.";
        }
        else
        {
            assignedBeer = correctBeer;

            instructionText.text =
                "PREPARATION FAILED!\n" +
                "Pick up the assigned beer.";
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