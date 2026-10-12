using UnityEngine;
using UnityEngine.UI;

public class LevelCircleUI : MonoBehaviour
{
    [Header("Level Settings")]
    [Tooltip("Must match the playable scene name exactly.")]
    public string levelID = "WrongBeerLevel";

    [Header("Circle Image")]
    public Image circleImage;

    [Header("Colours")]
    public Color unplayedColour =
        new Color32(55, 55, 55, 255);

    public Color activeColour =
        new Color32(212, 167, 44, 255);

    public Color completedColour =
        new Color32(150, 150, 150, 255);

    private void OnEnable()
    {
        LevelProgressManager.OnLevelProgressChanged
            += OnProgressChanged;
    }

    private void OnDisable()
    {
        LevelProgressManager.OnLevelProgressChanged
            -= OnProgressChanged;
    }

    private void Start()
    {
        if (circleImage == null)
        {
            circleImage = GetComponent<Image>();
        }

        RefreshCircle();
    }

    private void OnProgressChanged(string changedLevelID)
    {
        if (changedLevelID == levelID)
        {
            RefreshCircle();
        }
    }

    public void RefreshCircle()
    {
        if (circleImage == null)
        {
            circleImage = GetComponent<Image>();
        }

        if (circleImage == null)
        {
            Debug.LogError(
                "LevelCircleUI: No Image assigned on " +
                gameObject.name
            );
            return;
        }

        if (LevelProgressManager.Instance == null)
        {
            Debug.LogWarning(
                "LevelProgressManager was not found for " +
                gameObject.name
            );
            return;
        }

        LevelProgressManager.LevelState state =
            LevelProgressManager.Instance.GetLevelState(levelID);

        switch (state)
        {
            case LevelProgressManager.LevelState.Unplayed:
                circleImage.color = unplayedColour;
                break;

            case LevelProgressManager.LevelState.Active:
                circleImage.color = activeColour;
                break;

            case LevelProgressManager.LevelState.Completed:
                circleImage.color = completedColour;
                break;

            default:
                circleImage.color = unplayedColour;
                Debug.LogWarning(
                    "Unknown level state for " + levelID
                );
                break;
        }

        Debug.Log(
            "Circle: " + gameObject.name +
            " | Level ID: " + levelID +
            " | State: " + state
        );
    }
}