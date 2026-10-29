using UnityEngine;
using TMPro;

public class MeetingTimer : MonoBehaviour
{
    [Header("Meeting Time")]
    [Tooltip("Available meeting time in seconds.")]
    public float meetingTime = 120f;

    [Header("Timer Speeds")]
    [Tooltip("Countdown multiplier for the correct beer.")]
    public float normalTimeSpeed = 1f;

    [Tooltip("Countdown multiplier for the wrong beer.")]
    public float drunkTimeSpeed = 5f;

    [Header("Timer UI")]
    public TextMeshProUGUI timerText;

    private float remainingTime;
    private bool timerRunning;
    private bool meetingMissed;
    private bool wrongBeerRoute;

    public float GetRemainingTime()
    {
        return remainingTime;
    }

    public bool IsMeetingMissed()
    {
        return meetingMissed;
    }

    public bool IsTimerRunning()
    {
        return timerRunning;
    }

    private void Awake()
    {
        remainingTime = Mathf.Max(0f, meetingTime);
        timerRunning = false;
        meetingMissed = false;
        wrongBeerRoute = false;
        UpdateTimerUI();
    }

    private void Update()
    {
        if (!timerRunning || meetingMissed)
            return;

        float speed = wrongBeerRoute
            ? drunkTimeSpeed
            : normalTimeSpeed;

        remainingTime -= Time.deltaTime * Mathf.Max(0f, speed);

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            meetingMissed = true;
            timerRunning = false;

            Debug.Log("[MeetingTimer] The meeting deadline has expired.");
        }

        UpdateTimerUI();
    }

    // Kept for compatibility with scripts that call StartMeetingTimer().
    public void StartMeetingTimer()
    {
        StartMeetingTimer(false);
    }

    // HistoricalFigure calls this overload to choose the correct speed.
    public void StartMeetingTimer(bool wrongBeer)
    {
        remainingTime = Mathf.Max(0f, meetingTime);
        wrongBeerRoute = wrongBeer;
        meetingMissed = false;
        timerRunning = remainingTime > 0f;

        UpdateTimerUI();

        Debug.Log(wrongBeer
            ? "[MeetingTimer] Wrong beer: countdown running at fast speed."
            : "[MeetingTimer] Correct beer: countdown running at normal speed.");
    }

    public void StopMeetingTimer()
    {
        timerRunning = false;
        UpdateTimerUI();
    }

    public void PauseMeetingTimer()
    {
        timerRunning = false;
    }

    public void ResumeMeetingTimer()
    {
        if (!meetingMissed && remainingTime > 0f)
            timerRunning = true;
    }

    public void ResetMeetingTimer()
    {
        remainingTime = Mathf.Max(0f, meetingTime);
        timerRunning = false;
        meetingMissed = false;
        wrongBeerRoute = false;
        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        int totalSeconds = Mathf.CeilToInt(remainingTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text = minutes.ToString("00") + ":" +
                         seconds.ToString("00");
    }
}