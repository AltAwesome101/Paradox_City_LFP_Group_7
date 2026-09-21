using UnityEngine;

[CreateAssetMenu(fileName = "TutorialDataSO", menuName = "Tutorial/TutorialDataSO")]
public class TutorialDataSO : ScriptableObject
{
    public string TutorialName;
    public Texture2D tutorialImage;
    [TextArea] public string TutorialDescription;

}
