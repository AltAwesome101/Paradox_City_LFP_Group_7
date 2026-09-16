using UnityEngine;
using UnityEngine.UIElements;
/// <summary>
/// Approaching Apple Warning counter UI: 
/// the idea is that it should scale up on each proper integer value and scale down showing animation. 
/// Maybe to accompany the scale up to show the current time there should be a erratic shake to emphasize the warning to the player 
/// The whole numbers should change from alert to white.
/// so transition for example warning duration of 3 seconds
/// (int)3(alert colour,shake, enlargen) -> float(return to smaller, base colour) ->(int)2(alert colour,shake, enlargen)-> etc... until countdown over
/// 
/// </summary>
public class AppleWarningCounterUI
{
    readonly Label _counter;
    readonly AppleWarningCounterDataSO data;

    int _lastShownInt = int.MinValue;
    float _pulseElapsed;
    float _previousRemaining = -1f;

    public AppleWarningCounterUI(Label counterLabel, in AppleWarningCounterDataSO data)
    {
        _counter = counterLabel;
        this.data = data;
        // _counter.style.position = Position.Absolute;
    }

    public void UpdateDisplay(float remaining)
    {
        _counter.style.display = DisplayStyle.Flex;
        // Debug.Log("Warning Counter ON!");

        int currentInt = Mathf.CeilToInt(remaining);
        if (currentInt != _lastShownInt)
        {
            _lastShownInt = currentInt;
            _counter.text = currentInt > 0 ? currentInt.ToString() : "";
            // Debug.Log($"Counter value:{currentInt.ToString().WithBold()}");
            _pulseElapsed = 0f; // crossing into a new integer restarts the pop
        }

        float deltaTime = _previousRemaining < 0f ? 0f : Mathf.Max(0f, _previousRemaining - remaining);
        _previousRemaining = remaining;
        _pulseElapsed += deltaTime;

        float t = Mathf.Clamp01(_pulseElapsed / data.pulseDuration);
        float curveT = data.popUpCurve.Evaluate(t);

        Vector3 scale = Vector3.Lerp(data.maxScale, data.minScale, curveT);
        _counter.style.scale = new StyleScale(new Scale(scale));
        _counter.style.color = new StyleColor(Color.Lerp(data.alertColour, data.baseColour, curveT));

        float shake = Mathf.Lerp(data.shakeMagnitude, 0f, curveT);
        Vector2 jitter = Random.insideUnitCircle * shake;
        _counter.style.translate = new StyleTranslate(new Translate(jitter.x, jitter.y));
    }

    public void Hide() => _counter.style.display = DisplayStyle.None;
}
