using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GlassCleaningProgress : MonoBehaviour
{
    public static GlassCleaningProgress Instance { get; private set; }

    [Header("Glass Progress UI")]
    public TextMeshProUGUI progressText;
    public GameObject progressTextPanel;

    [Header("Glass Requirements")]
    public int requiredGlasses = 3;

    [Header("Glass Drop-Off Point")]
    [Tooltip("The capsule marker above the glass drop-off point.")]
    public Transform dropOffArrow;

    [Tooltip("The exact point where glasses are returned.")]
    public Transform glassDropOffPoint;

    [Tooltip("Base height of the floating capsule above the counter.")]
    public float arrowHeight = 1.5f;

    [Tooltip("Capsule rotation. Keep X, Y and Z at 0 to keep it upright.")]
    public Vector3 arrowRotation = Vector3.zero;

    [Tooltip("Scale of the capsule marker.")]
    public Vector3 arrowScale = new Vector3(0.25f, 0.7f, 0.25f);

    [Header("Arrow Floating Animation")]
    [Tooltip("How far the arrow moves up and down.")]
    public float floatAmplitude = 0.25f;

    [Tooltip("How quickly the arrow floats.")]
    public float floatSpeed = 2f;

    private readonly HashSet<CleanableGlass> returnedGlasses =
        new HashSet<CleanableGlass>();

    private Vector3 arrowBasePosition;
    private bool duplicateInstance = false;

    public int GlassesReturned => returnedGlasses.Count;

    public bool AllGlassesReturned =>
        GlassesReturned >= requiredGlasses;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "Another GlassCleaningProgress already exists. " +
                "Destroying this duplicate."
            );

            duplicateInstance = true;
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (duplicateInstance)
        {
            return;
        }

        UpdateProgressUI();
        UpdateDropOffArrow();
    }

    private void Update()
    {
        if (duplicateInstance)
        {
            return;
        }

        UpdateDropOffArrow();
        AnimateArrow();
    }

    public void RegisterReturnedGlass(CleanableGlass glass)
    {
        if (duplicateInstance || glass == null)
        {
            return;
        }

        // Prevent the same glass from counting more than once.
        if (!returnedGlasses.Add(glass))
        {
            return;
        }

        UpdateProgressUI();
        UpdateDropOffArrow();

        Debug.Log(
            "Glasses returned: " +
            Mathf.Min(GlassesReturned, requiredGlasses) +
            "/" + requiredGlasses
        );
    }

    private void UpdateProgressUI()
    {
        bool showProgress = !AllGlassesReturned;

        if (progressText != null)
        {
            progressText.text =
                "Glass Cleaned: " +
                Mathf.Min(GlassesReturned, requiredGlasses) +
                "/" + requiredGlasses;

            if (progressTextPanel == null)
            {
                progressText.gameObject.SetActive(showProgress);
            }
        }

        if (progressTextPanel != null)
        {
            progressTextPanel.SetActive(showProgress);
        }
    }

    private void UpdateDropOffArrow()
    {
        if (dropOffArrow == null)
        {
            return;
        }

        if (AllGlassesReturned || glassDropOffPoint == null)
        {
            dropOffArrow.gameObject.SetActive(false);
            return;
        }

        // Keep the capsule above the counter.
        arrowBasePosition =
            glassDropOffPoint.position +
            Vector3.up * arrowHeight;

        dropOffArrow.localScale = arrowScale;
        dropOffArrow.rotation = Quaternion.Euler(arrowRotation);

        // Start at the base position before floating.
        dropOffArrow.position = arrowBasePosition;

        dropOffArrow.gameObject.SetActive(true);
    }

    private void AnimateArrow()
    {
        if (dropOffArrow == null ||
            !dropOffArrow.gameObject.activeSelf ||
            AllGlassesReturned ||
            glassDropOffPoint == null)
        {
            return;
        }

        // Smoothly float above the drop-off point.
        float verticalOffset =
            Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;

        dropOffArrow.position =
            arrowBasePosition +
            Vector3.up * verticalOffset;
    }

    public void ResetProgress()
    {
        returnedGlasses.Clear();

        UpdateProgressUI();
        UpdateDropOffArrow();
    }
}