using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;

public class PaintingCanvas : MonoBehaviour
{
    public enum PaintingType { Landscape, Architecture, FigureStudy }

    [Header("Painting")]
    [Tooltip("Which procedural painting this canvas generates.")]
    public PaintingType paintingType = PaintingType.Landscape;
    [Tooltip("Resolution of the generated texture (square). 256 is fast; 512 looks sharper but generates slower at Start.")]
    public int textureResolution = 256;
    [Tooltip("Random seed for this painting's generated look. Same seed = same painting every time.")]
    public int paintingSeed = 0;
    [Tooltip("The Renderer showing the canvas - its material's texture gets replaced with the generated painting. Leave empty to use the Renderer on this GameObject.")]
    public Renderer canvasRenderer;

    [Header("Player")]
    [Tooltip("How close the player needs to be to press F and start interacting")]
    public float interactRange = 2.5f;
    private Transform player;
    private ThirdPersonController thirdPersonController;
    private CharacterController characterController;

    [Header("Interaction Key")]
    public KeyCode interactKey = KeyCode.F;

    [Header("Hitler's Vision")]
    [Tooltip("Hitler's NPCVision component - if he can see the player while they're interacting, they get kicked out")]
    public NPCVision hitlerVision;

    [Serializable]
    public class PaintingFlaw
    {
        [Tooltip("Empty child Transform positioned in 3D space over the flaw, on the canvas surface - used for click detection and the marker sprite.")]
        public Transform marker;
        [Tooltip("Normalized position of this flaw on the canvas TEXTURE. (0,0) = bottom-left corner, (1,1) = top-right. Line this up with where 'marker' visually sits on the canvas.")]
        public Vector2 textureUV = new Vector2(0.5f, 0.5f);
    }

    [Header("Correction Points")]
    [Tooltip("Each flaw needs a 3D marker (for clicking) AND a texture UV (for repainting). Add 2-4 per painting.")]
    public List<PaintingFlaw> flaws = new List<PaintingFlaw>();
    [Tooltip("How close a click needs to land to a marker (world units) to count as a hit")]
    public float clickRadius = 0.15f;
    [Tooltip("Radius of each flaw's scribble/repaint patch, in normalized UV units (fraction of canvas width)")]
    public float flawUVRadius = 0.08f;
    private readonly HashSet<int> corrected = new HashSet<int>();

    [Header("Correction Point Visuals")]
    public Color markerColor = new Color(1f, 0.85f, 0.2f, 0.9f);
    public Color correctedFlashColor = Color.white;
    public float markerSize = 0.2f;
    public float pulseSpeed = 3f;
    public float pulseAmount = 0.15f;
    [Tooltip("How long the repaint-reveal animation takes when a flaw is corrected")]
    public float correctionEffectDuration = 0.6f;

    private readonly Dictionary<int, SpriteRenderer> markers = new Dictionary<int, SpriteRenderer>();
    private static Sprite cachedMarkerSprite;

    [Header("Camera (MainCameraController-based)")]
    public MainCameraController mainCamera;
    public Transform paintingFocusPoint;
    public float paintingCameraGap = 1.5f;
    public Camera paintingCamera;

    private Transform previousCameraTarget;
    private float previousCameraGap;

    [Header("Debug")]
    public bool debugLogging = true;

    public bool IsInteracting { get; private set; }
    public bool IsComplete { get; private set; }
    public int CorrectionsCompleted => corrected.Count;
    public int CorrectionsRequired => flaws.Count;

    public bool CanInteract =>
        !IsComplete && !IsInteracting && PlayerInRange() &&
        (hitlerVision == null || !hitlerVision.CanSeeTargetRightNow);

    public event Action OnPaintingComplete;
    public event Action OnForcedExit;

    
    private Texture2D cleanTexture;
    
    private Texture2D displayTexture;
    private Material canvasMaterialInstance;
    private System.Random rng;

    private void Start()
    {
        if (canvasRenderer == null)
            canvasRenderer = GetComponent<Renderer>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            thirdPersonController = p.GetComponent<ThirdPersonController>();
            characterController = p.GetComponent<CharacterController>();
        }
        else if (debugLogging)
        {
            Debug.LogWarning("[PaintingCanvas] No GameObject tagged 'Player' found.");
        }

        if (hitlerVision != null)
        {
            hitlerVision.OnTargetSpotted += HandleHitlerSpottedPlayer;
        }
        else if (debugLogging)
        {
            Debug.LogWarning("[PaintingCanvas] 'Hitler Vision' isn't assigned - the forced-exit-when-spotted behaviour won't run.");
        }

        GeneratePainting();
        CreateCorrectionMarkers();
    }

    // ==================================================================
    // PAINTING GENERATION
    // ==================================================================

    private void GeneratePainting()
    {
        rng = new System.Random(paintingSeed);

        cleanTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
        cleanTexture.wrapMode = TextureWrapMode.Clamp;
        cleanTexture.filterMode = FilterMode.Bilinear;

        switch (paintingType)
        {
            case PaintingType.Landscape: PaintLandscape(cleanTexture); break;
            case PaintingType.Architecture: PaintArchitecture(cleanTexture); break;
            case PaintingType.FigureStudy: PaintFigureStudy(cleanTexture); break;
        }
        cleanTexture.Apply();

        displayTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
        displayTexture.wrapMode = TextureWrapMode.Clamp;
        displayTexture.filterMode = FilterMode.Bilinear;
        displayTexture.SetPixels32(cleanTexture.GetPixels32());

        foreach (PaintingFlaw flaw in flaws)
        {
            PaintFlaw(displayTexture, flaw.textureUV);
        }
        displayTexture.Apply();

        if (canvasRenderer != null)
        {
            canvasMaterialInstance = canvasRenderer.material; 
            canvasMaterialInstance.mainTexture = displayTexture;
        }
        else if (debugLogging)
        {
            Debug.LogWarning("[PaintingCanvas] No Renderer found to display the generated painting on.");
        }
    }

    private void PaintLandscape(Texture2D tex)
    {
        int res = tex.width;
        Color skyTop = new Color(0.55f, 0.75f, 0.92f);
        Color skyBottom = new Color(0.85f, 0.9f, 0.8f);
        Color groundNear = new Color(0.25f, 0.45f, 0.2f);
        Color groundFar = new Color(0.45f, 0.55f, 0.3f);
        Color mountain = new Color(0.4f, 0.42f, 0.5f);

        int horizon = (int)(res * 0.55f);

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                Color c = y > horizon
                    ? Color.Lerp(skyBottom, skyTop, (float)(y - horizon) / (res - horizon))
                    : Color.Lerp(groundNear, groundFar, (float)y / horizon);
                tex.SetPixel(x, y, c);
            }
        }

        float seedOffset = (float)rng.NextDouble() * 100f;
        for (int x = 0; x < res; x++)
        {
            float n = Mathf.PerlinNoise(x * 0.01f + seedOffset, 0f);
            int peakHeight = horizon + (int)(n * res * 0.18f);
            for (int y = horizon; y < peakHeight; y++)
            {
                float shade = Mathf.InverseLerp(horizon, peakHeight, y);
                tex.SetPixel(x, y, Color.Lerp(mountain, groundFar, shade * 0.3f));
            }
        }

        DrawSoftEllipse(tex, new Vector2(res * 0.75f, res * 0.8f), res * 0.06f, res * 0.06f, new Color(1f, 0.95f, 0.7f));
    }

    private void PaintArchitecture(Texture2D tex)
    {
        int res = tex.width;
        Color sky = new Color(0.7f, 0.78f, 0.85f);
        Color wall = new Color(0.7f, 0.6f, 0.5f);
        Color wallShadow = new Color(0.5f, 0.42f, 0.36f);
        Color roof = new Color(0.4f, 0.22f, 0.2f);
        Color ground = new Color(0.5f, 0.48f, 0.42f);

        int horizon = (int)(res * 0.35f);

        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
                tex.SetPixel(x, y, y < horizon ? ground : sky);

        int buildingCount = 5;
        int buildingWidth = res / buildingCount;

        for (int b = 0; b < buildingCount; b++)
        {
            int bx = b * buildingWidth;
            int height = horizon + (int)(res * (0.15f + (float)rng.NextDouble() * 0.35f));

            for (int x = bx + 4; x < bx + buildingWidth - 4 && x < res; x++)
            {
                for (int y = horizon; y < height; y++)
                {
                    bool isEdge = x < bx + 8 || x > bx + buildingWidth - 8;
                    tex.SetPixel(x, y, isEdge ? wallShadow : wall);
                }

                int roofPeak = height + buildingWidth / 6;
                for (int y = height; y < roofPeak; y++)
                {
                    float halfWidth = Mathf.Lerp(buildingWidth / 2f - 4f, 0f, (float)(y - height) / (roofPeak - height));
                    float centerX = bx + buildingWidth / 2f;
                    if (Mathf.Abs(x - centerX) <= halfWidth)
                        tex.SetPixel(x, y, roof);
                }
            }
        }
    }

    private void PaintFigureStudy(Texture2D tex)
    {
        int res = tex.width;
        Color background = new Color(0.82f, 0.78f, 0.7f);
        Color figure = new Color(0.75f, 0.6f, 0.5f);
        Color shade = new Color(0.55f, 0.42f, 0.35f);

        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
                tex.SetPixel(x, y, background);

        DrawSoftEllipse(tex, new Vector2(res * 0.5f, res * 0.78f), res * 0.08f, res * 0.1f, figure);
        DrawSoftEllipse(tex, new Vector2(res * 0.5f, res * 0.52f), res * 0.16f, res * 0.22f, figure);
        DrawSoftEllipse(tex, new Vector2(res * 0.5f, res * 0.22f), res * 0.12f, res * 0.2f, shade);
    }

    private void DrawSoftEllipse(Texture2D tex, Vector2 center, float radiusX, float radiusY, Color color)
    {
        int minX = Mathf.Max(0, (int)(center.x - radiusX));
        int maxX = Mathf.Min(tex.width - 1, (int)(center.x + radiusX));
        int minY = Mathf.Max(0, (int)(center.y - radiusY));
        int maxY = Mathf.Min(tex.height - 1, (int)(center.y + radiusY));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float nx = (x - center.x) / radiusX;
                float ny = (y - center.y) / radiusY;
                float d = nx * nx + ny * ny;

                if (d <= 1f)
                {
                    float edgeSoftness = Mathf.SmoothStep(1f, 0.7f, d);
                    Color existing = tex.GetPixel(x, y);
                    tex.SetPixel(x, y, Color.Lerp(existing, color, edgeSoftness));
                }
            }
        }
    }

    
    private void PaintFlaw(Texture2D tex, Vector2 uv)
    {
        int res = tex.width;
        Vector2 center = new Vector2(uv.x * res, uv.y * res);
        float radius = flawUVRadius * res;
        Color flawColor = new Color(0.15f, 0.1f, 0.1f, 1f);

        int minX = Mathf.Max(0, (int)(center.x - radius));
        int maxX = Mathf.Min(res - 1, (int)(center.x + radius));
        int minY = Mathf.Max(0, (int)(center.y - radius));
        int maxY = Mathf.Min(res - 1, (int)(center.y + radius));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist > radius) continue;

                float noise = Mathf.PerlinNoise(x * 0.15f, y * 0.15f);
                float falloff = 1f - dist / radius;

                if (noise * falloff > 0.35f)
                {
                    Color existing = tex.GetPixel(x, y);
                    float strength = Mathf.Clamp01(falloff * 1.3f);
                    tex.SetPixel(x, y, Color.Lerp(existing, flawColor, strength));
                }
            }
        }
    }

    // ==================================================================
    // CORRECTION MARKERS
    // ==================================================================

    private void CreateCorrectionMarkers()
    {
        Sprite sprite = GetOrCreateMarkerSprite();

        for (int i = 0; i < flaws.Count; i++)
        {
            Transform marker = flaws[i].marker;
            if (marker == null)
            {
                if (debugLogging)
                    Debug.LogWarning($"[PaintingCanvas] Flaw index {i} has no marker Transform assigned - it won't be clickable.");
                continue;
            }

            GameObject markerObj = new GameObject($"Marker_{marker.name}");
            markerObj.transform.SetParent(marker, false);
            markerObj.transform.localPosition = Vector3.zero;
            markerObj.transform.localScale = Vector3.one * markerSize;

            SpriteRenderer sr = markerObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = markerColor;
            sr.sortingOrder = 10;

            markerObj.SetActive(false);
            markers[i] = sr;
        }
    }

    private static Sprite GetOrCreateMarkerSprite()
    {
        if (cachedMarkerSprite != null) return cachedMarkerSprite;

        const int res = 64;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(res / 2f, res / 2f);
        float radius = res / 2f;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - dist / radius);
                alpha = Mathf.SmoothStep(0f, 1f, alpha * 1.8f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        cachedMarkerSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
        return cachedMarkerSprite;
    }

    private void ShowMarkers()
    {
        foreach (var kvp in markers)
        {
            if (corrected.Contains(kvp.Key)) continue;
            if (kvp.Value != null) kvp.Value.gameObject.SetActive(true);
        }
    }

    private void HideMarkers()
    {
        foreach (var kvp in markers)
            if (kvp.Value != null) kvp.Value.gameObject.SetActive(false);
    }

    private void UpdateMarkerVisuals()
    {
        Camera cam = paintingCamera != null ? paintingCamera : Camera.main;
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        foreach (var kvp in markers)
        {
            if (corrected.Contains(kvp.Key)) continue;
            SpriteRenderer sr = kvp.Value;
            if (sr == null || !sr.gameObject.activeSelf) continue;

            sr.transform.localScale = Vector3.one * markerSize * pulse;

            if (cam != null)
                sr.transform.rotation = Quaternion.LookRotation(sr.transform.position - cam.transform.position);
        }
    }

    // ==================================================================
    // FIXING A FLAW - reveal the clean texture underneath
    // ==================================================================

    private IEnumerator PlayCorrectionEffect(int index)
    {
        markers.TryGetValue(index, out SpriteRenderer sr);
        if (sr != null) sr.gameObject.SetActive(true);

        Vector2 uv = flaws[index].textureUV;
        int res = displayTexture.width;
        Vector2 center = new Vector2(uv.x * res, uv.y * res);
        float maxRadius = flawUVRadius * res * 1.4f; 

        int minX = Mathf.Max(0, (int)(center.x - maxRadius));
        int maxX = Mathf.Min(res - 1, (int)(center.x + maxRadius));
        int minY = Mathf.Max(0, (int)(center.y - maxRadius));
        int maxY = Mathf.Min(res - 1, (int)(center.y + maxRadius));

        float t = 0f;
        while (t < correctionEffectDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / correctionEffectDuration);
            float revealRadius = maxRadius * progress;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist > revealRadius + 2f) continue;

                    float blend = Mathf.Clamp01(Mathf.SmoothStep(1f, 0f, (dist - revealRadius) / 3f + 0.5f));
                    Color clean = cleanTexture.GetPixel(x, y);
                    Color current = displayTexture.GetPixel(x, y);
                    displayTexture.SetPixel(x, y, Color.Lerp(current, clean, blend));
                }
            }
            displayTexture.Apply(false);

            if (sr != null)
            {
                sr.color = Color.Lerp(correctedFlashColor, Color.clear, progress);
                sr.transform.localScale = Vector3.one * markerSize * (1f + progress * 1.5f);
            }

            yield return null;
        }

        
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                displayTexture.SetPixel(x, y, cleanTexture.GetPixel(x, y));
        displayTexture.Apply(false);

        if (sr != null) sr.gameObject.SetActive(false);
    }

    // ==================================================================
    // INTERACTION
    // ==================================================================

    private void Update()
    {
        if (IsComplete) return;

        if (!IsInteracting)
        {
            if (Input.GetKeyDown(interactKey))
            {
                if (CanInteract)
                {
                    EnterInteraction();
                }
                else if (debugLogging)
                {
                    if (player == null)
                        Debug.LogWarning("[PaintingCanvas] Pressed interact key but no Player was found - check the Player GameObject is tagged 'Player'.");
                    else if (!PlayerInRange())
                        Debug.Log($"[PaintingCanvas] Pressed interact key but out of range (distance {Vector3.Distance(player.position, transform.position):F2}, need <= {interactRange}).");
                    else if (hitlerVision != null && hitlerVision.CanSeeTargetRightNow)
                        Debug.Log("[PaintingCanvas] Pressed interact key but Hitler can see the player right now.");
                }
            }
        }
        else
        {
            if (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.Escape))
            {
                ExitInteraction();
                return;
            }

            if (Input.GetMouseButtonDown(0))
                TryClickCorrectionPoint();

            UpdateMarkerVisuals();
        }
    }

    private bool PlayerInRange()
    {
        if (player == null) return false;
        return Vector3.Distance(player.position, transform.position) <= interactRange;
    }

    private void EnterInteraction()
    {
        IsInteracting = true;

        if (thirdPersonController != null) thirdPersonController.enabled = false;
        if (characterController != null) characterController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (mainCamera != null)
        {
            previousCameraTarget = mainCamera.target;
            previousCameraGap = mainCamera.gap;
            mainCamera.inputEnabled = false;
            if (paintingFocusPoint != null) mainCamera.target = paintingFocusPoint;
            mainCamera.gap = paintingCameraGap;
        }

        ShowMarkers();

        if (debugLogging) Debug.Log("[PaintingCanvas] Entered painting interaction.");
    }

    private void ExitInteraction()
    {
        IsInteracting = false;

        if (thirdPersonController != null) thirdPersonController.enabled = true;
        if (characterController != null) characterController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (mainCamera != null)
        {
            mainCamera.target = previousCameraTarget;
            mainCamera.gap = previousCameraGap;
            mainCamera.inputEnabled = true;
        }

        HideMarkers();

        if (debugLogging) Debug.Log("[PaintingCanvas] Exited painting interaction.");
    }

    private void HandleHitlerSpottedPlayer()
    {
        if (!IsInteracting) return;

        if (debugLogging) Debug.Log("[PaintingCanvas] Hitler spotted the player mid-interaction - forcing exit!");
        ExitInteraction();
        OnForcedExit?.Invoke();
    }

    private void TryClickCorrectionPoint()
    {
        Camera cam = paintingCamera != null ? paintingCamera : Camera.main;
        if (cam == null)
        {
            if (debugLogging) Debug.LogWarning("[PaintingCanvas] No camera available to raycast the click from.");
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        int closestHit = -1;
        float closestDist = clickRadius;

        for (int i = 0; i < flaws.Count; i++)
        {
            Transform point = flaws[i].marker;
            if (point == null || corrected.Contains(i)) continue;

            Vector3 toPoint = point.position - ray.origin;
            float alongRay = Vector3.Dot(toPoint, ray.direction);
            if (alongRay < 0) continue;

            Vector3 closestOnRay = ray.origin + ray.direction * alongRay;
            float dist = Vector3.Distance(closestOnRay, point.position);

            if (dist < closestDist)
            {
                closestDist = dist;
                closestHit = i;
            }
        }

        if (closestHit >= 0)
        {
            corrected.Add(closestHit);
            StartCoroutine(PlayCorrectionEffect(closestHit));
            if (debugLogging) Debug.Log($"[PaintingCanvas] Corrected flaw {closestHit} ({CorrectionsCompleted}/{CorrectionsRequired})");

            if (CorrectionsCompleted >= CorrectionsRequired)
                CompletePainting();
        }
    }

    private void CompletePainting()
    {
        IsComplete = true;
        ExitInteraction();
        if (debugLogging) Debug.Log("[PaintingCanvas] Painting complete!");
        OnPaintingComplete?.Invoke();
    }
}