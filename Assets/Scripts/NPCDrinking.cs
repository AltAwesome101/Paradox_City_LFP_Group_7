using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPCDrinking : MonoBehaviour
{
    [Header("Drinking Speeds")]
    [Tooltip("Seconds for Gandhi to finish the correct beer.")]
    public float normalDrinkingTime = 8f;

    [Tooltip("Seconds for Gandhi to reach the drunk threshold on wrong beer.")]
    public float wrongBeerDrinkingTime = 20f;

    [Header("Drunk Threshold")]
    [Range(0.01f, 1f)]
    [Tooltip("Wrong beer makes Gandhi drunk at this percentage.")]
    public float drunkThreshold = 0.6f;

    [Header("Drinking UI")]
    public Slider drinkingBar;
    public TextMeshProUGUI drinkingPercentageText;
    public TextMeshProUGUI drinkingLabelText;

    private float drinkingProgress;
    private bool isDrinking;
    private bool isWrongBeer;
    private bool isDrunk;
    private bool finishedDrinking;
    private bool drinkingStoppedAtOutcome;

    public float DrinkingProgress => drinkingProgress;
    public bool IsDrinking => isDrinking;

    private void Start()
    {
        drinkingProgress = 0f;
        isDrinking = false;
        isWrongBeer = false;
        isDrunk = false;
        finishedDrinking = false;
        drinkingStoppedAtOutcome = false;

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

        if (drinkingLabelText != null)
        {
            drinkingLabelText.text = "Drinking";
            drinkingLabelText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isDrinking || drinkingStoppedAtOutcome)
            return;

        float duration = isWrongBeer
            ? wrongBeerDrinkingTime
            : normalDrinkingTime;

        duration = Mathf.Max(0.01f, duration);

        // Correct beer fills quickly. Wrong beer fills slowly.
        drinkingProgress += Time.deltaTime / duration;
        drinkingProgress = Mathf.Clamp01(drinkingProgress);

        // Only the wrong beer can make Gandhi drunk.
        if (isWrongBeer && drinkingProgress >= drunkThreshold)
        {
            drinkingProgress = Mathf.Clamp01(drunkThreshold);
            BecomeDrunk();
            UpdateDrinkingUI();
            return;
        }

        // Correct beer reaches 100% and ends the drinking sequence.
        if (!isWrongBeer && drinkingProgress >= 1f)
        {
            drinkingProgress = 1f;
            FinishDrinking();
        }

        UpdateDrinkingUI();
    }

    public void StartDrinking(bool wrongBeer)
    {
        if (isDrinking || finishedDrinking || drinkingStoppedAtOutcome)
            return;

        isWrongBeer = wrongBeer;
        drinkingProgress = 0f;
        isDrunk = false;
        finishedDrinking = false;
        drinkingStoppedAtOutcome = false;
        isDrinking = true;

        if (drinkingBar != null)
            drinkingBar.gameObject.SetActive(true);

        if (drinkingPercentageText != null)
            drinkingPercentageText.gameObject.SetActive(true);

        if (drinkingLabelText != null)
        {
            drinkingLabelText.text = wrongBeer
                ? "Drinking"
                : "Drinking";

            drinkingLabelText.gameObject.SetActive(true);
        }

        UpdateDrinkingUI();

        Debug.Log(wrongBeer
            ? "[NPCDrinking] Wrong beer: slow drinking started."
            : "[NPCDrinking] Correct beer: fast drinking started.");
    }

    private void BecomeDrunk()
    {
        isDrunk = true;
        isDrinking = false;
        drinkingStoppedAtOutcome = true;

        Debug.Log(
            "[NPCDrinking] Gandhi reached the drunk threshold. " +
            "The drinking bar is now frozen."
        );
    }

    private void FinishDrinking()
    {
        isDrinking = false;
        finishedDrinking = true;
        drinkingStoppedAtOutcome = true;

        Debug.Log(
            "[NPCDrinking] Gandhi finished the correct beer. " +
            "The drinking bar is frozen at 100%."
        );
    }

    private void UpdateDrinkingUI()
    {
        if (drinkingBar != null)
            drinkingBar.value = drinkingProgress;

        if (drinkingPercentageText != null)
        {
            drinkingPercentageText.text =
                Mathf.RoundToInt(drinkingProgress * 100f) + "%";
        }
    }

    public bool IsDrunk()
    {
        return isDrunk;
    }

    public bool HasFinishedDrinking()
    {
        return finishedDrinking;
    }

    public bool HasReachedDrinkingOutcome()
    {
        return drinkingStoppedAtOutcome;
    }

    public void StopDrinking()
    {
        isDrinking = false;
        drinkingStoppedAtOutcome = true;
        UpdateDrinkingUI();
    }

    public void ResetDrinking()
    {
        drinkingProgress = 0f;
        isDrinking = false;
        isWrongBeer = false;
        isDrunk = false;
        finishedDrinking = false;
        drinkingStoppedAtOutcome = false;

        UpdateDrinkingUI();

        if (drinkingBar != null)
            drinkingBar.gameObject.SetActive(false);

        if (drinkingPercentageText != null)
            drinkingPercentageText.gameObject.SetActive(false);

        if (drinkingLabelText != null)
            drinkingLabelText.gameObject.SetActive(false);
    }
}