using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPCDrinking : MonoBehaviour
{
    [Header("Drinking")]
    public float normalDrinkingTime = 8f;
    public float wrongBeerDrinkingTime = 20f;

    [Header("UI")]
    public Slider drinkingBar;

    [Tooltip("Text that displays the drinking percentage.")]
    public TextMeshProUGUI drinkingPercentageText;

    [Header("Drunk State")]
    public float drunkThreshold = 0.6f;

    [Header("Meeting Timer")]
    public MeetingTimer meetingTimer;

    private float drinkingProgress = 0f;
    private float currentDrinkingTime;

    private bool isDrinking = false;
    private bool finishedDrinking = false;
    private bool isDrunk = false;
    private bool receivedWrongBeer = false;

    private void Start()
    {
        // Make sure the percentage text and bar
        // start hidden before the NPC receives beer.
        if (drinkingBar != null)
        {
            drinkingBar.minValue = 0f;
            drinkingBar.maxValue = 1f;
            drinkingBar.value = 0f;
            drinkingBar.gameObject.SetActive(false);
        }

        if (drinkingPercentageText != null)
        {
            drinkingPercentageText.text = "0%";
            drinkingPercentageText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isDrinking)
        {
            return;
        }

        // Update drinking progress over time.
        drinkingProgress +=
            Time.deltaTime / currentDrinkingTime;

        drinkingProgress =
            Mathf.Clamp01(drinkingProgress);

        // Update the slider.
        if (drinkingBar != null)
        {
            drinkingBar.value = drinkingProgress;
        }

        // Convert progress into a percentage.
        int percentage = Mathf.RoundToInt(
            drinkingProgress * 100f
        );

        // Update the percentage text.
        if (drinkingPercentageText != null)
        {
            drinkingPercentageText.text =
                percentage + "%";
        }

        // The NPC becomes drunk after drinking
        // 60% of the wrong beer.
        if (!isDrunk &&
            receivedWrongBeer &&
            drinkingProgress >= drunkThreshold)
        {
            BecomeDrunk();
        }

        // Finish when the bar reaches 100%.
        if (drinkingProgress >= 1f)
        {
            FinishDrinking();
        }
    }

    public void StartDrinking(bool wrongBeer)
    {
        if (finishedDrinking)
        {
            return;
        }

        drinkingProgress = 0f;
        isDrinking = true;
        receivedWrongBeer = wrongBeer;
        isDrunk = false;

        // Choose the drinking duration.
        if (receivedWrongBeer)
        {
            currentDrinkingTime =
                wrongBeerDrinkingTime;
        }
        else
        {
            currentDrinkingTime =
                normalDrinkingTime;
        }

        // Reset and display the slider.
        if (drinkingBar != null)
        {
            drinkingBar.minValue = 0f;
            drinkingBar.maxValue = 1f;
            drinkingBar.value = 0f;
            drinkingBar.gameObject.SetActive(true);
        }

        // Reset and display the percentage.
        if (drinkingPercentageText != null)
        {
            drinkingPercentageText.text = "0%";
            drinkingPercentageText.gameObject.SetActive(true);
        }

        Debug.Log("NPC started drinking.");

        // Wrong beer speeds up the meeting timer.
        if (receivedWrongBeer &&
            meetingTimer != null)
        {
            meetingTimer.SpeedUpTime();

            Debug.Log(
                "WRONG BEER: Time manipulation started immediately."
            );
        }
    }

    private void BecomeDrunk()
    {
        isDrunk = true;

        Debug.Log("NPC IS NOW DRUNK!");

        Debug.Log(
            "NPC has reached the drunk threshold."
        );
    }

    private void FinishDrinking()
    {
        isDrinking = false;
        finishedDrinking = true;
        drinkingProgress = 1f;

        if (drinkingBar != null)
        {
            drinkingBar.value = 1f;
        }

        if (drinkingPercentageText != null)
        {
            drinkingPercentageText.text = "100%";
        }

        Debug.Log("NPC finished drinking.");

        if (isDrunk)
        {
            Debug.Log(
                "NPC finished the wrong beer and is drunk."
            );
        }
        else
        {
            Debug.Log(
                "NPC finished the correct beer."
            );
        }
    }

    public bool IsDrinking()
    {
        return isDrinking;
    }

    public bool HasFinishedDrinking()
    {
        return finishedDrinking;
    }

    public bool IsDrunk()
    {
        return isDrunk;
    }
}