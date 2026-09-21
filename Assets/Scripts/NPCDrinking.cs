using UnityEngine;
using UnityEngine.UI;

public class NPCDrinking : MonoBehaviour
{
    [Header("Drinking")]
    public float normalDrinkingTime = 8f;
    public float wrongBeerDrinkingTime = 20f;

    [Header("UI")]
    public Slider drinkingBar;

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

    private void Update()
    {
        if (!isDrinking)
        {
            return;
        }

        drinkingProgress +=
            Time.deltaTime / currentDrinkingTime;

        drinkingProgress =
            Mathf.Clamp01(drinkingProgress);

        if (drinkingBar != null)
        {
            drinkingBar.value =
                drinkingProgress;
        }

        // The NPC becomes drunk after drinking
        // 60% of the wrong beer.
        if (!isDrunk &&
            receivedWrongBeer &&
            drinkingProgress >= drunkThreshold)
        {
            BecomeDrunk();
        }

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

        // Correct beer is consumed normally.
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

        if (drinkingBar != null)
        {
            drinkingBar.value = 0f;
            drinkingBar.gameObject.SetActive(true);
        }

        Debug.Log(
            "NPC started drinking."
        );

        // WRONG BEER:
        // Manipulate time immediately when the NPC
        // starts drinking instead of waiting for
        // the drunk threshold.
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

        Debug.Log(
            "NPC IS NOW DRUNK!"
        );

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

        Debug.Log(
            "NPC finished drinking."
        );

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