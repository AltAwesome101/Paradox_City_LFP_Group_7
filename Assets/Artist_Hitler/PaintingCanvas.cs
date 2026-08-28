using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;


public class PaintingCanvas : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("How close the player needs to be to press F and start interacting")]
    public float interactRange = 2.5f;
    private Transform player;
    private ThirdPersonController thirdPersonController;
    private CharacterController characterController;

    [Header("Interaction Key")]
    [Tooltip("Key used to enter/exit the painting edit mode")]
    public KeyCode interactKey = KeyCode.F;

    [Header("Hitler's Vision")]
    [Tooltip("Hitler's NPCVision component - if he can see the player while they're interacting, they get kicked out")]
    public NPCVision hitlerVision;

    [Header("Correction Points")]
    [Tooltip("Small empty child objects placed over the flaws on the canvas. The player clicks these one at a time, in any order.")]
    public List<Transform> correctionPoints = new List<Transform>();
    [Tooltip("How close a click needs to land to a correction point (world units) to count as a hit")]
    public float clickRadius = 0.15f;
    private readonly HashSet<Transform> corrected = new HashSet<Transform>();

    [Header("Correction Point Visuals")]
    [Tooltip("Color of the pulsing marker shown over each flaw while editing")]
    public Color markerColor = new Color(1f, 0.85f, 0.2f, 0.9f);
    [Tooltip("Color flashed on a marker the instant its flaw is corrected")]
    public Color correctedFlashColor = Color.white;
    [Tooltip("World-space diameter of each marker")]
    public float markerSize = 0.2f;
    [Tooltip("How fast the markers pulse while waiting to be clicked")]
    public float pulseSpeed = 3f;
    [Tooltip("How much the markers grow/shrink while pulsing (fraction of markerSize)")]
    public float pulseAmount = 0.15f;
    [Tooltip("How long the little burst effect plays when a flaw is corrected")]
    public float correctionEffectDuration = 0.4f;

    private readonly Dictionary<Transform, SpriteRenderer> markers = new Dictionary<Transform, SpriteRenderer>();
    private static Sprite cachedMarkerSprite;

    [Header("Camera (MainCameraController-based)")]
    [Tooltip("Drag your Main Camera (the one with MainCameraController on it) here")]
    public MainCameraController mainCamera;
    [Tooltip("Empty Transform positioned to nicely frame the canvas - the camera orbits this while interacting")]
    public Transform paintingFocusPoint;
    [Tooltip("How close the camera sits from the painting while interacting (temporarily overrides the normal follow distance)")]
    public float paintingCameraGap = 1.5f;
    [Tooltip("Leave empty to just use Camera.main for raycasting clicks - correct as long as MainCameraController is on your Main Camera")]
    public Camera paintingCamera;

    private Transform previousCameraTarget;
    private float previousCameraGap;

    [Header("Debug")]
    public bool debugLogging = true;

    public bool IsInteracting { get; private set; }
    public bool IsComplete { get; private set; }
    public int CorrectionsCompleted => corrected.Count;
    public int CorrectionsRequired => correctionPoints.Count;

    
    public bool CanInteract =>
        !IsComplete && !IsInteracting && PlayerInRange() &&
        (hitlerVision == null || !hitlerVision.CanSeeTargetRightNow);

    public event Action OnPaintingComplete;
    public event Action OnForcedExit; 

    private void Start()
    {
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

        CreateCorrectionMarkers();
    }

    

    private void CreateCorrectionMarkers()
    {
        Sprite sprite = GetOrCreateMarkerSprite();

        foreach (Transform point in correctionPoints)
        {
            if (point == null) continue;

            GameObject markerObj = new GameObject($"Marker_{point.name}");
            markerObj.transform.SetParent(point, false);
            markerObj.transform.localPosition = Vector3.zero;
            markerObj.transform.localScale = Vector3.one * markerSize;

            SpriteRenderer sr = markerObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = markerColor;
            sr.sortingOrder = 10;

            markerObj.SetActive(false); 
            markers[point] = sr;
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
        {
            if (kvp.Value != null) kvp.Value.gameObject.SetActive(false);
        }
    }

    private void UpdateMarkerVisuals()
    {
        Camera cam = paintingCamera != null ? paintingCamera : Camera.main;
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        foreach (var kvp in markers)
        {
            Transform point = kvp.Key;
            SpriteRenderer sr = kvp.Value;
            if (sr == null || !sr.gameObject.activeSelf) continue;
            if (corrected.Contains(point)) continue; 

            sr.transform.localScale = Vector3.one * markerSize * pulse;

            if (cam != null)
            {
                sr.transform.rotation = Quaternion.LookRotation(sr.transform.position - cam.transform.position);
            }
        }
    }

    private IEnumerator PlayCorrectionEffect(Transform point)
    {
        if (!markers.TryGetValue(point, out SpriteRenderer sr) || sr == null) yield break;

        sr.gameObject.SetActive(true); 
        float t = 0f;

        while (t < correctionEffectDuration)
        {
            t += Time.deltaTime;
            float p = t / correctionEffectDuration;

            sr.color = Color.Lerp(correctedFlashColor, Color.clear, p);
            sr.transform.localScale = Vector3.one * markerSize * (1f + p * 1.5f); 

            yield return null;
        }

        sr.gameObject.SetActive(false);
    }

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
            {
                TryClickCorrectionPoint();
            }

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

        Transform closestHit = null;
        float closestDist = clickRadius;

        foreach (Transform point in correctionPoints)
        {
            if (point == null || corrected.Contains(point)) continue;

            
            Vector3 toPoint = point.position - ray.origin;
            float alongRay = Vector3.Dot(toPoint, ray.direction);
            if (alongRay < 0) continue;
            Vector3 closestOnRay = ray.origin + ray.direction * alongRay;
            float dist = Vector3.Distance(closestOnRay, point.position);

            if (dist < closestDist)
            {
                closestDist = dist;
                closestHit = point;
            }
        }

        if (closestHit != null)
        {
            corrected.Add(closestHit);
            StartCoroutine(PlayCorrectionEffect(closestHit));
            if (debugLogging) Debug.Log($"[PaintingCanvas] Corrected {closestHit.name} ({CorrectionsCompleted}/{CorrectionsRequired})");

            if (CorrectionsCompleted >= CorrectionsRequired)
            {
                CompletePainting();
            }
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