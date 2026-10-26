using UnityEngine;

[CreateAssetMenu(fileName ="AppleDefinition",menuName ="Apples/ Apple Definition")]
public class AppleDefinitionSO : ScriptableObject
{
    public Apple Prefab;
    public int ProgressValue = 1;
    public Color AppleColor;
    public AppleEffect[] CaughtEffects;
    public AppleEffect[] MissedEffects;


}
