using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GlassCleaningProgress : MonoBehaviour
{
    public static GlassCleaningProgress Instance { get; private set; }

    [Header("Progress UI")]
    public TextMeshProUGUI progressText;
    public GameObject progressTextPanel;

    [Header("Objective")]
    public int requiredGlasses = 3;

    private readonly HashSet<CleanableGlass> returnedGlasses =
        new HashSet<CleanableGlass>();

    public int GlassesReturned => returnedGlasses.Count;

    public bool AllGlassesReturned =>
        returnedGlasses.Count >= requiredGlasses;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        UpdateProgressText();
    }

    public void RegisterReturnedGlass(CleanableGlass glass)
    {
        if (glass == null || !returnedGlasses.Add(glass))
            return;

        UpdateProgressText();

        Debug.Log(
            "Glasses returned: " +
            GlassesReturned + "/" + requiredGlasses
        );
    }

    private void UpdateProgressText()
    {
        bool showProgress = !AllGlassesReturned;

        if (progressText != null)
        {
            progressText.text =
                "Glasses cleaned: " +
                GlassesReturned + "/" + requiredGlasses;
        }

        if (progressTextPanel != null)
        {
            progressTextPanel.SetActive(showProgress);
        }
        else if (progressText != null)
        {
            // Fallback if no separate panel is assigned.
            progressText.gameObject.SetActive(showProgress);
        }
    }
}