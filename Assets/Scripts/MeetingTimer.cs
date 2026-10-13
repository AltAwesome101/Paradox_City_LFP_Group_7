using UnityEngine;
using TMPro;

public class MeetingTimer : MonoBehaviour
{
    [Header("Timer")]
    public float meetingTime = 120f;

    [Header("Time Manipulation")]
    public float normalTimeSpeed = 1f;
    public float drunkTimeSpeed = 3f;

    [Header("UI")]
    public TextMeshProUGUI timerText;

    private float currentTime;
    private bool timerRunning = false;
    private bool meetingMissed = false;
    private float currentTimeSpeed = 1f;

    private void Start()
    {
        currentTime = meetingTime;
        currentTimeSpeed = normalTimeSpeed;

        UpdateTimerUI();
    }

    private void Update()
    {
        if (!timerRunning)
        {
            return;
        }

        currentTime -=
            Time.deltaTime *
            currentTimeSpeed;

        if (currentTime <= 0f)
        {
            currentTime = 0f;

            meetingMissed = true;
            timerRunning = false;
            currentTimeSpeed = normalTimeSpeed;

            UpdateTimerUI();

            Debug.Log(
                "MEETING MISSED!"
            );

            if (timerText != null)
            {
                timerText.text =
                    "MEETING MISSED";
            }

            // Pause the entire game.
            Time.timeScale = 0f;

            return;
        }

        UpdateTimerUI();
    }

    public void StartMeetingTimer()
    {
        if (meetingMissed)
        {
            return;
        }

        if (timerRunning)
        {
            return;
        }

        // Make sure the game is running.
        Time.timeScale = 1f;

        timerRunning = true;

        Debug.Log(
            "Meeting timer started."
        );
    }

    public void SpeedUpTime()
    {
        if (!timerRunning)
        {
            return;
        }

        currentTimeSpeed = drunkTimeSpeed;

        Debug.Log(
            "TIME MANIPULATION ACTIVE! " +
            "Timer speed: " +
            currentTimeSpeed +
            "x"
        );
    }

    public void ReturnToNormalTime()
    {
        currentTimeSpeed = normalTimeSpeed;

        Debug.Log(
            "Time returned to normal."
        );
    }

    public bool IsMeetingMissed()
    {
        return meetingMissed;
    }

    public bool IsTimerRunning()
    {
        return timerRunning;
    }

    public float GetRemainingTime()
    {
        return currentTime;
    }

    public float GetCurrentTimeSpeed()
    {
        return currentTimeSpeed;
    }

    private void UpdateTimerUI()
    {
        if (timerText == null)
        {
            return;
        }

        int minutes =
            Mathf.FloorToInt(
                currentTime / 60f
            );

        int seconds =
            Mathf.FloorToInt(
                currentTime % 60f
            );

        timerText.text =
            "Meeting in: " +
            minutes.ToString("00") +
            ":" +
            seconds.ToString("00");
    }
}