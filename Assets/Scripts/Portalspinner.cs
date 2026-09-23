using UnityEngine;

// Put this directly on the spinning portal object
// (as a child of the TimeMachinePortal object, not on the portal itself).
// No sprite needed - it generates its own circular swirl texture on Awake.
[RequireComponent(typeof(SpriteRenderer))]
public class PortalSpinner : MonoBehaviour
{
    [Header("Spin")]
    [Tooltip("Degrees per second.")]
    [SerializeField] float rotationSpeed = 90f;
    [Tooltip("Local axis to spin around. A sprite facing the camera usually wants Z " +
             "(spins in place like a coin); a flat sprite lying on the ground usually wants Y.")]
    [SerializeField] Vector3 rotationAxis = Vector3.forward;

    [Header("Procedural Portal Texture")]
    [Tooltip("Texture size in pixels (square). Higher = smoother edge, more expensive to generate.")]
    [SerializeField] int resolution = 256;
    [Tooltip("How many spiral arms swirl into the center.")]
    [SerializeField] int swirlArms = 6;
    [Tooltip("How tightly the swirl winds as it approaches the center.")]
    [SerializeField] float swirlTightness = 4f;
    [SerializeField] Color innerColor = new Color(1f, 0.9f, 0.35f, 1f);
    [SerializeField] Color outerColor = new Color(0.2f, 0.05f, 0.6f, 1f);
    [Tooltip("Pixels-per-unit used when building the Sprite from the generated texture " +
             "(controls how big it appears in world space).")]
    [SerializeField] float pixelsPerUnit = 100f;

    SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = GeneratePortalSprite();
    }

    void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
    }

    Sprite GeneratePortalSprite()
    {
        var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float radius = resolution * 0.5f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f) - center;
                float dist = point.magnitude;
                float normalizedDist = dist / radius;

                if (normalizedDist > 1f)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                // swirl pattern: angle around the center + distance from center
                // together decide how far toward outerColor this pixel leans
                float angle = Mathf.Atan2(point.y, point.x);
                float swirl = Mathf.Sin(angle * swirlArms + normalizedDist * swirlTightness * Mathf.PI * 2f);
                float swirlMix = Mathf.Clamp01(0.5f + 0.5f * swirl);

                Color baseColor = Color.Lerp(innerColor, outerColor, normalizedDist);
                Color pixelColor = Color.Lerp(baseColor, outerColor, swirlMix * 0.4f);

                // soft fade at the very edge so it isn't a hard-cut circle
                float edgeFade = Mathf.Clamp01((1f - normalizedDist) * resolution * 0.05f);
                pixelColor.a *= edgeFade;

                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, resolution, resolution),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit);
    }
}