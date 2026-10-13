using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


[Serializable]
public class WrongBeerAdBoards
{
    [Header("Wrong Beer - advert quads (drag from the Hierarchy)")]
    [Tooltip("Every quad that should show an advert. Each quad gets its own poster " +
             "(quad 1 = design 1, quad 2 = design 2, ... it loops after 6).")]
    [SerializeField] private List<Renderer> adQuads = new List<Renderer>();

    [Header("Wrong Beer - poster look")]
    [Tooltip("Optional. Leave empty to use a built-in unlit material. If posters look wrong in a BUILD, " +
             "assign any Unlit material here so its shader is included in the build.")]
    [SerializeField] private Material posterMaterial;
    [Tooltip("Pixel size of the longest side of each generated poster. Lower = faster to generate.")]
    [Range(128, 1024)]
    [SerializeField] private int posterResolution = 512;
    [Tooltip("How far (world units) the text floats in front of the quad. Raise it if text flickers.")]
    [SerializeField] private float textOffset = 0.02f;

    // ------------------------------------------------------------------------------------
    // Layout (fractions of the poster, 0 = bottom / left, 1 = top / right)
    // ------------------------------------------------------------------------------------
    private const float HeadlineCenterY = 0.80f;
    private const float HeadlineH = 0.26f;
    private const float IconCenterY = 0.45f;
    private const float IconZoneH = 0.38f;
    private const float TaglineCenterY = 0.13f;
    private const float TaglineH = 0.14f;
    private const float BandWidth = 0.84f;
    private const float TextPadding = 0.92f;

    // ------------------------------------------------------------------------------------
    // Ad designs
    // ------------------------------------------------------------------------------------
    private enum Pattern { Stripes, Sunburst, Dots, Rings }
    private enum Icon { Mug, Bottle, Clock }

    private struct AdDesign
    {
        public string headline;
        public string tagline;
        public Color top, bottom, accent, text;
        public Pattern pattern;
        public Icon icon;

        public AdDesign(string headline, string tagline,
                        int top, int bottom, int accent, int text,
                        Pattern pattern, Icon icon)
        {
            this.headline = headline;
            this.tagline = tagline;
            this.top = Hex(top);
            this.bottom = Hex(bottom);
            this.accent = Hex(accent);
            this.text = Hex(text);
            this.pattern = pattern;
            this.icon = icon;
        }
    }

    private static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
    }

    
    private static readonly AdDesign[] BeforeAds =
    {
        new AdDesign("GRIM\nBREW",        "The ONLY beer. Mandatory.",
                     0x3B0A0A, 0x120404, 0xFF3B30, 0xFFFFFF, Pattern.Stripes,  Icon.Mug),
        new AdDesign("SLUDGE\nLAGER",     "Tastes like the future. Sadly.",
                     0x4A5220, 0x1C2008, 0xC8D64A, 0xF4FFD0, Pattern.Dots,     Icon.Bottle),
        new AdDesign("DRINK.\nOBEY.",     "Daily quota: 3 beers",
                     0x2A2A35, 0x0E0E14, 0xFFA500, 0xFFFFFF, Pattern.Sunburst, Icon.Clock),
        new AdDesign("FOAMCORP",          "Your thirst is our property",
                     0x35154D, 0x12061C, 0xC060FF, 0xFFFFFF, Pattern.Rings,    Icon.Mug),
        new AdDesign("HOP-O-\nMATIC",     "Beer by robots. No humans needed.",
                     0x0B3A44, 0x041419, 0x30E0FF, 0xE0FCFF, Pattern.Stripes,  Icon.Bottle),
        new AdDesign("WARM\nBEER DAY",    "Every day. Forever.",
                     0x4D3015, 0x1C1007, 0xE0A050, 0xFFF1DC, Pattern.Sunburst, Icon.Mug),
    };

    
    private static readonly AdDesign[] AfterAds =
    {
        new AdDesign("GOLDEN\nAGE ALE",     "Cheers to a brighter tomorrow",
                     0xFFD54A, 0xFF8C1A, 0xFFF3C4, 0xFFFFFF, Pattern.Sunburst, Icon.Mug),
        new AdDesign("SUNRISE\nLAGER",      "Crisp. Clear. Time-tested.",
                     0xFF9A8B, 0xFF5E7E, 0xFFE3A3, 0xFFFFFF, Pattern.Rings,    Icon.Bottle),
        new AdDesign("TIME WELL\nSPENT",    "Share a beer with a friend",
                     0x3FD6B5, 0x0E8F8A, 0xE8FFF8, 0xFFFFFF, Pattern.Dots,     Icon.Clock),
        new AdDesign("PARADOX\nPALE",       "Perfectly timed. Since forever.",
                     0x6EC6FF, 0x2E6FE0, 0xFFFFFF, 0xFFFFFF, Pattern.Stripes,  Icon.Mug),
        new AdDesign("HOP TO\nTHE FUTURE",  "Brewed by neighbours, for neighbours",
                     0x9BE15D, 0x2E9E4F, 0xF4FFD6, 0xFFFFFF, Pattern.Sunburst, Icon.Bottle),
        new AdDesign("FRIDAY\nEVERY DAY",   "The right beer at the right time",
                     0xF08BFF, 0x7A3CFF, 0xFFF0FF, 0xFFFFFF, Pattern.Dots,     Icon.Clock),
    };

    // ------------------------------------------------------------------------------------
    // Runtime state
    // ------------------------------------------------------------------------------------
    private class Board
    {
        public Renderer renderer;
        public float aspect;
        public TextMeshPro headline;
        public TextMeshPro tagline;
    }

    private readonly List<Board> _boards = new List<Board>();
    private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
    private readonly List<Texture2D> _textures = new List<Texture2D>();
    private bool _completed;
    private bool _ready;

    // ------------------------------------------------------------------------------------
    // Public API (called by HubPrefabSwitcher)
    // ------------------------------------------------------------------------------------

    
    public void Initialise(bool wrongBeerCompleted)
    {
        Dispose();
        _completed = wrongBeerCompleted;

        if (adQuads == null || adQuads.Count == 0)
        {
            Debug.LogWarning("[WrongBeerAdBoards] No ad quads assigned, so no adverts will be shown.");
            return;
        }

        foreach (Renderer r in adQuads)
        {
            if (r != null) _boards.Add(BuildBoard(r));
        }

        _ready = true;
        Apply();
    }

    
    public void SetCompleted(bool wrongBeerCompleted)
    {
        if (!_ready || wrongBeerCompleted == _completed) return;
        _completed = wrongBeerCompleted;
        Apply();
    }

    
    public void Dispose()
    {
        foreach (Board b in _boards)
        {
            if (b == null) continue;
            if (b.headline != null) UnityEngine.Object.Destroy(b.headline.gameObject);
            if (b.tagline != null) UnityEngine.Object.Destroy(b.tagline.gameObject);
        }
        _boards.Clear();

        foreach (Material m in _materials.Values)
            if (m != null) UnityEngine.Object.Destroy(m);
        _materials.Clear();

        foreach (Texture2D t in _textures)
            if (t != null) UnityEngine.Object.Destroy(t);
        _textures.Clear();

        _ready = false;
    }

    // ------------------------------------------------------------------------------------
    // Board setup
    // ------------------------------------------------------------------------------------
    private Board BuildBoard(Renderer r)
    {
        Transform t = r.transform;
        Vector3 lossy = t.lossyScale;

        
        Vector2 meshSize = Vector2.one;
        var mf = r.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Vector3 bs = mf.sharedMesh.bounds.size;
            meshSize = new Vector2(Mathf.Max(bs.x, 0.0001f), Mathf.Max(bs.y, 0.0001f));
        }

        Vector2 worldSize = new Vector2(meshSize.x * Mathf.Abs(lossy.x), meshSize.y * Mathf.Abs(lossy.y));

        var board = new Board
        {
            renderer = r,
            aspect = Mathf.Max(0.05f, worldSize.x / Mathf.Max(0.0001f, worldSize.y))
        };

        board.headline = CreateText(t, "AdHeadline", meshSize, worldSize, r.sortingOrder + 1,
                                    HeadlineCenterY, HeadlineH);
        board.tagline = CreateText(t, "AdTagline", meshSize, worldSize, r.sortingOrder + 1,
                                   TaglineCenterY, TaglineH);
        return board;
    }

    private static float Safe(float v)
    {
        return Mathf.Max(Mathf.Abs(v), 0.0001f);
    }

    private TextMeshPro CreateText(Transform quad, string objectName, Vector2 meshSize, Vector2 worldSize,
                                   int sortingOrder, float centreY, float heightFraction)
    {
        Vector3 lossy = quad.lossyScale;

        var go = new GameObject(objectName, typeof(RectTransform));
        Transform gt = go.transform;
        gt.SetParent(quad, false);
        gt.localRotation = Quaternion.identity;

        
        gt.localScale = new Vector3(1f / Safe(lossy.x), 1f / Safe(lossy.y), 1f / Safe(lossy.z));

        
        gt.localPosition = new Vector3(0f, (centreY - 0.5f) * meshSize.y, -textOffset / Safe(lossy.z));

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.rectTransform.sizeDelta = new Vector2(worldSize.x * BandWidth * TextPadding,
                                                  worldSize.y * heightFraction * TextPadding);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.5f;
        tmp.fontSizeMax = 1000f;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.sortingOrder = sortingOrder;
        tmp.text = string.Empty;
        return tmp;
    }

    // ------------------------------------------------------------------------------------
    // Applying a variant
    // ------------------------------------------------------------------------------------
    private void Apply()
    {
        AdDesign[] set = _completed ? AfterAds : BeforeAds;

        for (int i = 0; i < _boards.Count; i++)
        {
            Board b = _boards[i];
            if (b == null || b.renderer == null) continue;

            int designIndex = i % set.Length;
            AdDesign d = set[designIndex];

            Material mat = GetPosterMaterial(d, designIndex, b.aspect);
            if (mat != null) b.renderer.sharedMaterial = mat;

            if (b.headline != null)
            {
                b.headline.text = d.headline;
                b.headline.color = d.text;
            }
            if (b.tagline != null)
            {
                b.tagline.text = d.tagline;
                b.tagline.color = Color.white;
            }
        }
    }

    private Material GetPosterMaterial(AdDesign d, int designIndex, float aspect)
    {
        string key = (_completed ? "after" : "before") + "_" + designIndex + "_" + Mathf.RoundToInt(aspect * 20f);

        if (_materials.TryGetValue(key, out Material cached)) return cached;

        Material mat = CreateMaterial();
        if (mat == null) return null;

        Texture2D tex = BuildPoster(d, aspect);
        mat.mainTexture = tex;
        mat.name = "WrongBeerPoster_" + key;

        _textures.Add(tex);
        _materials[key] = mat;
        return mat;
    }

    private Material CreateMaterial()
    {
        if (posterMaterial != null) return new Material(posterMaterial);

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Texture");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Standard");

        if (sh == null)
        {
            Debug.LogError("[WrongBeerAdBoards] No shader found for the posters. Assign a Poster Material.");
            return null;
        }
        return new Material(sh);
    }

    // ------------------------------------------------------------------------------------
    // Poster texture generation
    // ------------------------------------------------------------------------------------
    private Texture2D BuildPoster(AdDesign d, float aspect)
    {
        int longSide = Mathf.Clamp(posterResolution, 128, 1024);
        int w, h;
        if (aspect >= 1f)
        {
            w = longSide;
            h = Mathf.Max(32, Mathf.RoundToInt(longSide / aspect));
        }
        else
        {
            h = longSide;
            w = Mathf.Max(32, Mathf.RoundToInt(longSide * aspect));
        }

        
        float s = Mathf.Min(w, h);
        float halfW = 0.5f * w / s;
        float halfH = 0.5f * h / s;
        float aa = 1.5f / s; 

        float iconCy = (IconCenterY - 0.5f) * h / s;
        float iconScale = Mathf.Min(IconZoneH * h / s / 0.65f, BandWidth * w / s / 0.5f);
        float iconAA = aa / iconScale;

        float plateHalfW = 0.5f * BandWidth * w / s;
        float headCy = (HeadlineCenterY - 0.5f) * h / s;
        float headHalfH = 0.5f * HeadlineH * h / s;
        float tagCy = (TaglineCenterY - 0.5f) * h / s;
        float tagHalfH = 0.5f * TaglineH * h / s;

        
        Color plate = Color.Lerp(d.bottom, Color.black, 0.65f);
        plate.a = 0.8f;

        var pixels = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            float py = (y + 0.5f - h * 0.5f) / s;
            Color rowBase = Color.Lerp(d.bottom, d.top, (y + 0.5f) / h);

            bool nearHead = Mathf.Abs(py - headCy) < headHalfH + 0.1f;
            bool nearTag = Mathf.Abs(py - tagCy) < tagHalfH + 0.1f;
            bool nearIcon = Mathf.Abs(py - iconCy) < 0.45f * iconScale;

            for (int x = 0; x < w; x++)
            {
                float px = (x + 0.5f - w * 0.5f) / s;
                Color c = rowBase;

                
                switch (d.pattern)
                {
                    case Pattern.Stripes:
                        {
                            float t = (px + py) * 9f;
                            if (t - Mathf.Floor(t) < 0.5f) c = Color.Lerp(c, d.accent, 0.10f);
                            break;
                        }
                    case Pattern.Sunburst:
                        {
                            float ang = Mathf.Atan2(py - iconCy, px);
                            int wedge = (int)((ang + Mathf.PI) / (2f * Mathf.PI) * 18f);
                            if ((wedge & 1) == 0) c = Color.Lerp(c, d.accent, 0.14f);
                            break;
                        }
                    case Pattern.Dots:
                        {
                            float gx = px / 0.08f;
                            float gy = py / 0.08f;
                            float fx = gx - Mathf.Round(gx);
                            float fy = gy - Mathf.Round(gy);
                            if (fx * fx + fy * fy < 0.09f) c = Color.Lerp(c, d.accent, 0.16f);
                            break;
                        }
                    case Pattern.Rings:
                        {
                            float dy = py - iconCy;
                            float r = Mathf.Sqrt(px * px + dy * dy) * 8f;
                            if (r - Mathf.Floor(r) < 0.5f) c = Color.Lerp(c, d.accent, 0.10f);
                            break;
                        }
                }

                
                if (nearIcon)
                    DrawIcon(ref c, d, px / iconScale, (py - iconCy) / iconScale, iconAA);

                
                if (nearHead)
                    Blend(ref c, plate, SdRoundBox(px, py, 0f, headCy, plateHalfW, headHalfH, 0.03f), aa);
                if (nearTag)
                    Blend(ref c, plate, SdRoundBox(px, py, 0f, tagCy, plateHalfW, tagHalfH, 0.03f), aa);

                
                float edge = Mathf.Min(halfW - Mathf.Abs(px), halfH - Mathf.Abs(py));
                if (edge < 0.06f)
                {
                    Blend(ref c, d.accent, edge - 0.025f, aa);
                    Blend(ref c, d.accent, Mathf.Abs(edge - 0.05f) - 0.004f, aa);
                }

                pixels[y * w + x] = c;
            }
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.anisoLevel = 4;
        tex.SetPixels32(pixels);
        tex.Apply(true, false);
        return tex;
    }

    // ------------------------------------------------------------------------------------
    // Icons (x, y are in "icon space": roughly -0.3 .. 0.3, y up)
    // ------------------------------------------------------------------------------------
    private static void DrawIcon(ref Color c, AdDesign d, float x, float y, float aa)
    {
        if (x < -0.45f || x > 0.45f || y < -0.45f || y > 0.45f) return;

        switch (d.icon)
        {
            case Icon.Mug:
                {
                    x += 0.08f; 
                    y += 0.075f;

                    Color glass = new Color(0.93f, 0.97f, 1f, 1f);
                    Color beer = new Color(1f, 0.72f, 0.10f, 1f);
                    Color beerLight = new Color(1f, 0.87f, 0.40f, 1f);
                    Color foam = new Color(1f, 0.99f, 0.92f, 1f);

                    Blend(ref c, glass, Mathf.Abs(SdCircle(x, y, 0.20f, 0.03f, 0.10f)) - 0.03f, aa);   
                    Blend(ref c, glass, SdRoundBox(x, y, 0f, 0f, 0.17f, 0.20f, 0.03f), aa);             
                    Blend(ref c, beer, SdRoundBox(x, y, 0f, -0.015f, 0.14f, 0.155f, 0.02f), aa);        
                    Blend(ref c, beerLight, SdRoundBox(x, y, -0.08f, -0.015f, 0.015f, 0.11f, 0.01f), aa); 
                    Blend(ref c, beerLight, SdCircle(x, y, 0.05f, -0.08f, 0.015f), aa);                 
                    Blend(ref c, beerLight, SdCircle(x, y, 0.08f, -0.02f, 0.010f), aa);
                    Blend(ref c, beerLight, SdCircle(x, y, 0.03f, 0.05f, 0.012f), aa);
                    Blend(ref c, foam, SdRoundBox(x, y, 0f, 0.16f, 0.14f, 0.04f, 0.01f), aa);           
                    Blend(ref c, foam, SdCircle(x, y, -0.09f, 0.21f, 0.075f), aa);
                    Blend(ref c, foam, SdCircle(x, y, 0f, 0.25f, 0.085f), aa);
                    Blend(ref c, foam, SdCircle(x, y, 0.09f, 0.21f, 0.075f), aa);
                    break;
                }

            case Icon.Bottle:
                {
                    y += 0.065f;

                    Color glassBrown = new Color(0.50f, 0.28f, 0.05f, 1f);
                    Color cream = new Color(1f, 0.95f, 0.80f, 1f);

                    Blend(ref c, glassBrown, SdRoundBox(x, y, 0f, -0.05f, 0.10f, 0.17f, 0.06f), aa);   
                    Blend(ref c, glassBrown, SdRoundBox(x, y, 0f, 0.20f, 0.042f, 0.10f, 0.02f), aa);   
                    Blend(ref c, d.accent, SdRoundBox(x, y, 0f, 0.315f, 0.055f, 0.022f, 0.01f), aa);   
                    Blend(ref c, cream, SdBox(x, y, 0f, -0.06f, 0.10f, 0.07f), aa);                    
                    Blend(ref c, d.accent, SdBox(x, y, 0f, -0.06f, 0.08f, 0.012f), aa);                
                    Blend(ref c, new Color(1f, 1f, 1f, 0.35f),
                          SdRoundBox(x, y, -0.065f, 0.05f, 0.012f, 0.06f, 0.01f), aa);                 
                    break;
                }

            case Icon.Clock:
                {
                    Color face = new Color(1f, 0.97f, 0.88f, 1f);
                    Color ink = new Color(0.12f, 0.10f, 0.10f, 1f);

                    Blend(ref c, face, SdCircle(x, y, 0f, 0f, 0.26f), aa);
                    Blend(ref c, d.accent, Mathf.Abs(SdCircle(x, y, 0f, 0f, 0.26f)) - 0.03f, aa);

                    float r = Mathf.Sqrt(x * x + y * y);
                    if (r > 0.17f && r < 0.25f)
                    {
                        float t = Mathf.Atan2(y, x) / (2f * Mathf.PI) * 12f;
                        float f = Mathf.Abs(t - Mathf.Round(t));
                        Blend(ref c, ink, f * (2f * Mathf.PI * r / 12f) - 0.008f, aa); 
                    }

                    Blend(ref c, ink, SdSegment(x, y, 0f, 0f, -0.113f, 0.065f) - 0.014f, aa); 
                    Blend(ref c, ink, SdSegment(x, y, 0f, 0f, 0.165f, 0.095f) - 0.010f, aa);  
                    Blend(ref c, d.accent, SdCircle(x, y, 0f, 0f, 0.022f), aa);               
                    break;
                }
        }
    }

    // ------------------------------------------------------------------------------------
    // Drawing helpers 
    // ------------------------------------------------------------------------------------
    private static void Blend(ref Color c, Color shape, float sd, float aa)
    {
        float a = Mathf.Clamp01(0.5f - sd / aa) * shape.a;
        if (a <= 0f) return;
        c = Color.Lerp(c, new Color(shape.r, shape.g, shape.b, 1f), a);
    }

    private static float SdCircle(float px, float py, float cx, float cy, float radius)
    {
        float dx = px - cx;
        float dy = py - cy;
        return Mathf.Sqrt(dx * dx + dy * dy) - radius;
    }

    private static float SdBox(float px, float py, float cx, float cy, float hw, float hh)
    {
        float dx = Mathf.Abs(px - cx) - hw;
        float dy = Mathf.Abs(py - cy) - hh;
        float ox = Mathf.Max(dx, 0f);
        float oy = Mathf.Max(dy, 0f);
        return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(dx, dy), 0f);
    }

    private static float SdRoundBox(float px, float py, float cx, float cy, float hw, float hh, float round)
    {
        return SdBox(px, py, cx, cy, hw - round, hh - round) - round;
    }

    private static float SdSegment(float px, float py, float ax, float ay, float bx, float by)
    {
        float pax = px - ax, pay = py - ay;
        float bax = bx - ax, bay = by - ay;
        float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
        float dx = pax - bax * h;
        float dy = pay - bay * h;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
}