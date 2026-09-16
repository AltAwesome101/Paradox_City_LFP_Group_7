using UnityEngine;

[CreateAssetMenu(fileName ="AppleWarningData", menuName ="UIData/AppleWarningCounter")]
public class AppleWarningCounterDataSO: ScriptableObject
{
    public AnimationCurve popUpCurve;
    public Color alertColour;
    public Color baseColour;
    public Vector3 minScale;
    public Vector3 maxScale;
    public float pulseDuration;
    public float shakeMagnitude;
    public float leftAnchorPercent = 20f;
    public float rightAnchorPercent = 80f;
}