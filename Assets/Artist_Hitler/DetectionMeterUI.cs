using UnityEngine;
using UnityEngine.UI;


public class DetectionMeterUI : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("Hitler's NPCVision component - Awareness (0-1) drives the bar")]
    public NPCVision hitlerVision;

    [Header("UI")]
    [Tooltip("An Image with Image Type set to 'Filled' - its fillAmount tracks Awareness")]
    public Image fillImage;
    [Tooltip("Optional label, e.g. 'DETECTED'")]
    public Text label;

    [Header("Behaviour")]
    [Tooltip("Hide the meter entirely while Awareness is at 0")]
    public bool hideWhenEmpty = true;
    [Tooltip("Color when Awareness is low")]
    public Color safeColor = Color.green;
    [Tooltip("Color when Awareness is at/near max")]
    public Color dangerColor = Color.red;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (hitlerVision == null)
        {
            Debug.LogWarning("[DetectionMeterUI] No NPCVision assigned - the meter won't update.");
        }
        if (fillImage == null)
        {
            Debug.LogWarning("[DetectionMeterUI] No fill Image assigned - nothing will render.");
        }
    }

    private void Update()
    {
        if (hitlerVision == null) return;

        float awareness = hitlerVision.Awareness; 

        if (fillImage != null)
        {
            fillImage.fillAmount = awareness;
            fillImage.color = Color.Lerp(safeColor, dangerColor, awareness);
        }

        if (label != null)
        {
            label.text = hitlerVision.CanSeeTargetRightNow ? "SPOTTED!" : "DETECTION";
        }

        if (hideWhenEmpty)
        {
            canvasGroup.alpha = awareness > 0.01f ? 1f : 0f;
        }
    }
}