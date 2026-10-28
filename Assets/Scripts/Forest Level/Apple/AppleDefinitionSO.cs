using UnityEngine;

[CreateAssetMenu(fileName ="AppleDefinition",menuName ="Apples/ Apple Definition")]
public class AppleDefinitionSO : ScriptableObject
{
    public Apple Prefab;
    public float AppleSize = 1f;
    public int ProgressValue = 1;
    public Color AppleColor;
    public AppleEffect[] CaughtEffects;
    public AppleEffect[] MissedEffects;
}
