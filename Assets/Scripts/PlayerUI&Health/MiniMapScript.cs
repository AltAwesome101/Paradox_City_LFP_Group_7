using System.Collections.Generic;
using UnityEngine;

public class MiniMapScript : MonoBehaviour
{
    public Transform player;

    [Header("Objective pulse")]
    public bool pulseEnabled = true;
    [Tooltip("Seconds between pulses (one objective per pulse).")]
    public float pulseInterval = 10f;
    public float firstPulseDelay = 3f;
    [Tooltip("Rings per pulse, sent one after another.")]
    public int ringsPerPulse = 2;
    public float ringDuration = 2.2f;
    public float ringStagger = 0.5f;
    [Tooltip("Final ring radius as a fraction of the map's half-height.")]
    [Range(0.05f, 1f)] public float maxRadius =100f;
    [Tooltip("Ring thickness as a fraction of the map's half-height.")]
    [Range(0.005f, 0.2f)] public float ringThickness = 0.04f;
    [Range(0f, 1f)] public float peakAlpha = 0.9f;
    [Tooltip("Objectives outside the map get their ring drawn on the nearest map edge.")]
    public bool clampToMapEdge = true;
    [Tooltip("Rings are drawn this far below the camera.")]
    public float ringDepth = 2f;
    [Tooltip("Spare layer used only by the minimap camera. It is removed from every other camera's culling mask.")]
    [Range(0, 31)] public int pulseLayer = 31;

    class Ring
    {
        public GameObject go;
        public Mesh mesh;
        public Material mat;
        public Vector3[] verts;
    }

    const int Segments = 48;

    Camera cam;
    readonly List<Ring> rings = new List<Ring>();
    readonly List<ArtExamDirector.PulseTarget> targets = new List<ArtExamDirector.PulseTarget>();
    ArtExamDirector.PulseTarget active;
    float nextPulse, pulseStart;
    int lastOrder = -1;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = GetComponentInChildren<Camera>();
        if (cam == null) Debug.LogWarning("[MiniMap] No Camera found on the minimap object - pulses are disabled.");
    }

    void Start()
    {
        nextPulse = Time.time + firstPulseDelay;
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 newPosition = player.position;
        newPosition.y = transform.position.y;
        transform.position = newPosition;

        transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f);

        if (pulseEnabled && cam != null) UpdatePulse();   // after the camera moved, so rings never lag
    }

    void OnDestroy()
    {
        foreach (Ring r in rings)
        {
            if (r.go != null) Destroy(r.go);
            if (r.mat != null) Destroy(r.mat);
            if (r.mesh != null) Destroy(r.mesh);
        }
    }

    // ------------------------------------------------------------ pulse logic
    void UpdatePulse()
    {
        if (active == null)
        {
            if (Time.time >= nextPulse) StartPulse();
            return;
        }

        float elapsed = Time.time - pulseStart;
        float total = ringDuration + (ringsPerPulse - 1) * ringStagger;
        Vector3 w = active.where();

        if (elapsed >= total || float.IsInfinity(w.x))
        {
            EndPulse();
            return;
        }

        float hw, hh;
        MapExtents(out hw, out hh);
        Vector3 center = MapPoint(w, hw, hh);

        for (int i = 0; i < rings.Count; i++)
        {
            Ring r = rings[i];
            float k = i < ringsPerPulse ? (elapsed - i * ringStagger) / ringDuration : -1f;

            if (k < 0f || k > 1f)
            {
                if (r.go.activeSelf) r.go.SetActive(false);
                continue;
            }

            if (!r.go.activeSelf) r.go.SetActive(true);

            float eased = 1f - (1f - k) * (1f - k);                       // fast start, slow end
            float radius = Mathf.Lerp(0.02f * hh, maxRadius * hh, eased);
            float alpha = Mathf.Clamp01(k * 6f) * (1f - k) * peakAlpha;   // quick fade-in, long fade-out

            r.go.transform.position = center;
            SetRingMesh(r, radius, ringThickness * hh);

            Color c = active.color;
            r.mat.color = new Color(c.r, c.g, c.b, alpha);
        }
    }

    void StartPulse()
    {
        ConfigureCameras();
        ArtExamDirector.CollectPulseTargets(targets);

        if (targets.Count == 0)
        {
            nextPulse = Time.time + 1f;   // nothing to show yet, check again soon
            return;
        }

        // next objective after the one we showed last (HUD order), wrapping around
        ArtExamDirector.PulseTarget pick = null, first = null;
        foreach (ArtExamDirector.PulseTarget t in targets)
        {
            if (first == null || t.order < first.order) first = t;
            if (t.order > lastOrder && (pick == null || t.order < pick.order)) pick = t;
        }
        if (pick == null) pick = first;
        lastOrder = pick.order;

        while (rings.Count < ringsPerPulse) rings.Add(MakeRing());

        active = pick;
        pulseStart = Time.time;
        nextPulse = Time.time + pulseInterval;
    }

    void EndPulse()
    {
        active = null;
        foreach (Ring r in rings) r.go.SetActive(false);
    }

    // ------------------------------------------------------------ helpers
    void ConfigureCameras()
    {
        int bit = 1 << pulseLayer;
        cam.cullingMask |= bit;
        foreach (Camera c in Camera.allCameras)
            if (c != cam) c.cullingMask &= ~bit;
    }

    void MapExtents(out float hw, out float hh)
    {
        hh = cam.orthographic ? cam.orthographicSize
                              : ringDepth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        hw = hh * cam.aspect;
    }

    // world position -> position of the ring in the minimap plane (optionally clamped to the map edge)
    Vector3 MapPoint(Vector3 world, float hw, float hh)
    {
        Vector3 camPos = transform.position;
        Vector3 off = world - camPos;
        float depthToTarget = Mathf.Max(0.1f, camPos.y - world.y);
        off.y = 0f;

        float x = Vector3.Dot(off, transform.right);
        float y = Vector3.Dot(off, transform.up);

        if (!cam.orthographic)
        {
            float s = ringDepth / depthToTarget;
            x *= s;
            y *= s;
        }

        if (clampToMapEdge)
        {
            float inset = maxRadius * hh * 0.5f;
            x = Mathf.Clamp(x, -(hw - inset), hw - inset);
            y = Mathf.Clamp(y, -(hh - inset), hh - inset);
        }

        Vector3 p = camPos + transform.right * x + transform.up * y;
        p.y = camPos.y - ringDepth;
        return p;
    }

    Ring MakeRing()
    {
        Ring r = new Ring();
        r.go = new GameObject("MiniMapPulseRing");
        r.go.layer = pulseLayer;

        r.verts = new Vector3[Segments * 2];
        r.mesh = new Mesh();
        r.mesh.name = "PulseRing";
        r.mesh.MarkDynamic();

        SetRingMesh(r, 1f, 0.1f);   // vertices first, then triangles

        int[] tris = new int[Segments * 6];
        for (int s = 0; s < Segments; s++)
        {
            int a = s * 2;                        // inner s
            int b = s * 2 + 1;                    // outer s
            int c = ((s + 1) % Segments) * 2;     // inner s+1
            int d = c + 1;                        // outer s+1
            int t = s * 6;
            tris[t] = a; tris[t + 1] = b; tris[t + 2] = d;
            tris[t + 3] = a; tris[t + 4] = d; tris[t + 5] = c;
        }
        r.mesh.triangles = tris;

        r.go.AddComponent<MeshFilter>().sharedMesh = r.mesh;
        MeshRenderer mr = r.go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        Shader sh = Shader.Find("Sprites/Default");   // unlit, transparent, double-sided
        if (sh == null) sh = Shader.Find("UI/Default");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        r.mat = new Material(sh);
        mr.sharedMaterial = r.mat;

        r.go.SetActive(false);
        return r;
    }

    // rebuilds the ring each frame so its thickness stays constant while the radius grows
    void SetRingMesh(Ring r, float radius, float thickness)
    {
        float inner = Mathf.Max(0f, radius - thickness * 0.5f);
        float outer = radius + thickness * 0.5f;

        for (int s = 0; s < Segments; s++)
        {
            float a = s / (float)Segments * Mathf.PI * 2f;
            float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
            r.verts[s * 2] = new Vector3(cs * inner, 0f, sn * inner);
            r.verts[s * 2 + 1] = new Vector3(cs * outer, 0f, sn * outer);
        }
        r.mesh.vertices = r.verts;
        r.mesh.RecalculateBounds();
    }
}