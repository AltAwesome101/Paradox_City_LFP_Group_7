using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class ParadoxLoadingScreen : MonoBehaviour
{
    public static ParadoxLoadingScreen Instance { get; private set; }

    
    public static bool IsShowing => Instance != null && Instance._canvas != null;

    public enum ParticleStyle { DataStreaks, Dust, Fireflies, Embers }

    [Serializable]
    public class LoadingTheme
    {
        [Tooltip("Any of these words appearing in the scene name (case-insensitive) selects this theme.")]
        public string[] sceneKeywords;
        public string title;
        public string subtitle;
        [Tooltip("A number rolls to it. Anything else (e.g. ????) is revealed like a slot machine.")]
        public string year;
        [Tooltip("Past destinations spin the clock backwards.")]
        public bool isPast = true;
        public Color backgroundTop = Color.black;
        public Color backgroundBottom = Color.black;
        public Color accent = Color.white;
        public Color secondary = Color.white;
        public ParticleStyle particles;
        public string[] statusMessages;
        public string[] tips;
    }

    [Header("Timing")]
    [Tooltip("How long the loading screen stays up on every scene load.")]
    public float loadDuration = 5f;
    [Tooltip("The last part of the duration is spent fading out.")]
    public float fadeOutDuration = 0.6f;
    [Tooltip("Only used by ParadoxLoadingScreen.Load(): fade in before the old scene disappears.")]
    public float fadeInDuration = 0.35f;
    [Tooltip("Show the loading screen for the very first scene too (when the game starts).")]
    public bool showOnFirstScene = true;

    [Header("While the screen is up")]
    [Tooltip("Pauses the game behind the overlay (Time.timeScale = 0) so nothing happens unseen.")]
    public bool freezeGameWhileLoading = true;
    public bool muteAudioWhileLoading = true;

    [Header("Look")]
    [Tooltip("Optional. Leave empty to use TextMeshPro's default font.")]
    public TMP_FontAsset fontOverride;
    public int sortingOrder = 32000;
    [Range(0, 80)] public int particleCount = 40;

    [Header("Themes (leave empty for the built-in four; the LAST theme is the fallback)")]
    public List<LoadingTheme> themes = new List<LoadingTheme>();

    [Header("Preview (Play Mode): right-click this component > Preview loading screen")]
    public string previewSceneName = "Forest_Scene";

    // ------------------------------------------------------------------
    // State
    // ------------------------------------------------------------------
    private class Particle
    {
        public RectTransform rt;
        public Image img;
        public Vector2 pos;
        public Vector2 vel;
        public float sway;
        public float pulse;
        public float phase;
        public float speedMul;
        public Color baseColor;
    }

    private static readonly string[] CommonStatus =
    {
        "Reversing the polarity of the timeline",
        "Calculating the paradox (it is a big one)",
        "Checking that history is still there",
        "Asking the universe to hold on a second"
    };

    private static readonly float[] ProgressTimes = { 0f, 0.22f, 0.38f, 0.55f, 0.70f, 0.86f, 1f };
    private static readonly float[] ProgressValues = { 0f, 0.28f, 0.31f, 0.66f, 0.70f, 0.93f, 1f };

    private const float BarWidth = 900f;
    private const float BarY = -330f;
    private const float ClockRadius = 180f;

    private Canvas _canvas;
    private CanvasGroup _group;
    private LoadingTheme _th;
    private float _t0;
    private bool _preCovered;
    private bool _handledAnyScene;
    private bool _isFirstScene = true;
    private Coroutine _sequence;

    private bool _frozen;
    private float _savedTimeScale = 1f;
    private bool _savedAudioPause;

    // generated art
    private Sprite _spWhite, _spCircle, _spRing, _spGlow, _spVignette;
    private readonly List<Texture2D> _sharedTextures = new List<Texture2D>();
    private Texture2D _bgTexture;
    private Sprite _bgSprite;

    // screen elements
    private RectTransform _clockRoot, _minuteHand, _hourHand, _scanBand, _barFill, _barHead;
    private readonly List<RectTransform> _orbiters = new List<RectTransform>();
    private readonly List<Particle> _particles = new List<Particle>();
    private TextMeshProUGUI _titleMain, _titleRed, _titleCyan, _yearText, _statusText, _tipText, _percentText;
    private Vector2 _titleBasePos;

    // animation state
    private float _handAngle;
    private List<string> _statuses = new List<string>();
    private int _lastStatusKey = -1, _lastPercent = -1, _tipSlot = -1, _tipStart;
    private bool _yearNumeric, _yearLocked;
    private int _startYear = 1000, _targetYear, _lastYear = 1000;
    private float _lockTime, _nextScramble;
    private string _scrambleText = "";

    // ------------------------------------------------------------------
    // Lifetime
    // ------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        if (themes == null || themes.Count == 0) themes = BuildDefaultThemes();

        BuildSharedSprites();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        
        if (Instance == this && !_handledAnyScene)
        {
            _handledAnyScene = true;
            if (showOnFirstScene) BeginSequence(SceneManager.GetActiveScene().name);
            _isFirstScene = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_frozen) UnfreezeGame();
        Instance = null;

        foreach (var t in _sharedTextures) if (t != null) Destroy(t);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;

        _handledAnyScene = true;

        if (_isFirstScene)
        {
            _isFirstScene = false;
            if (!showOnFirstScene) return;
        }

        BeginSequence(scene.name);
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------
    
    public static void Load(string sceneName)
    {
        if (Instance == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        Instance.StartCoroutine(Instance.CoverThenLoad(sceneName));
    }

    [ContextMenu("Preview loading screen (Play Mode)")]
    private void Preview()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("[ParadoxLoadingScreen] Enter Play Mode first, then use the preview.");
            return;
        }
        BeginSequence(string.IsNullOrEmpty(previewSceneName) ? SceneManager.GetActiveScene().name : previewSceneName);
    }

    private IEnumerator CoverThenLoad(string sceneName)
    {
        if (_sequence != null) { StopCoroutine(_sequence); _sequence = null; }

        BuildScreen(ResolveTheme(sceneName), sceneName);
        _preCovered = true;
        _group.alpha = 0f;

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Clamp01(t / Mathf.Max(0.01f, fadeInDuration));
            Tick(Time.unscaledTime - _t0, Time.unscaledDeltaTime);
            yield return null;
        }

        _group.alpha = 1f;
        SceneManager.LoadScene(sceneName);
    }

    // ------------------------------------------------------------------
    // Sequence
    // ------------------------------------------------------------------
    private void BeginSequence(string sceneName)
    {
        if (_sequence != null) StopCoroutine(_sequence);

        bool alreadyVisible = _preCovered && _canvas != null;
        _preCovered = false;

        LoadingTheme theme = ResolveTheme(sceneName);
        if (!alreadyVisible) BuildScreen(theme, sceneName);

        _sequence = StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        
        yield return new WaitForEndOfFrame();
        FreezeGame();

        bool unfrozen = false;
        float fadeStart = Mathf.Max(0.5f, loadDuration - fadeOutDuration);

        while (true)
        {
            float elapsed = Time.unscaledTime - _t0;
            if (elapsed >= loadDuration) break;

            Tick(elapsed, Time.unscaledDeltaTime);

            if (elapsed >= fadeStart)
            {
                if (!unfrozen)
                {
                    UnfreezeGame(); 
                    unfrozen = true;
                }
                _group.alpha = 1f - Mathf.Clamp01((elapsed - fadeStart) / Mathf.Max(0.01f, loadDuration - fadeStart));
            }
            else
            {
                _group.alpha = 1f;
            }

            yield return null;
        }

        UnfreezeGame();
        _lastYear = _yearNumeric ? _targetYear : 1900;
        DestroyScreen();
        _sequence = null;
    }

    private void FreezeGame()
    {
        if (!freezeGameWhileLoading || _frozen) return;

        _savedTimeScale = Time.timeScale;
        _savedAudioPause = AudioListener.pause;

        Time.timeScale = 0f;
        if (muteAudioWhileLoading) AudioListener.pause = true;
        _frozen = true;
    }

    private void UnfreezeGame()
    {
        if (!_frozen) return;

        Time.timeScale = _savedTimeScale;
        AudioListener.pause = _savedAudioPause;
        _frozen = false;
    }

    // ------------------------------------------------------------------
    // Themes
    // ------------------------------------------------------------------
    private LoadingTheme ResolveTheme(string sceneName)
    {
        foreach (var theme in themes)
        {
            if (theme.sceneKeywords == null) continue;
            foreach (var keyword in theme.sceneKeywords)
            {
                if (!string.IsNullOrEmpty(keyword) &&
                    sceneName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return theme;
            }
        }
        return themes[themes.Count - 1];
    }

    private static Color C(float r, float g, float b) { return new Color(r, g, b, 1f); }
    private static Color A(Color c, float a) { c.a = a; return c; }

    private List<LoadingTheme> BuildDefaultThemes()
    {
        return new List<LoadingTheme>
        {
            new LoadingTheme
            {
                sceneKeywords = new[] { "hitler", "hittler", "artist", "vienna" },
                title = "VIENNA ACADEMY OF FINE ARTS",
                subtitle = "THE ENTRANCE EXAMINATION",
                year = "1907",
                isPast = true,
                backgroundTop = C(0.09f, 0.06f, 0.04f),
                backgroundBottom = C(0.27f, 0.18f, 0.10f),
                accent = C(0.96f, 0.82f, 0.55f),
                secondary = C(0.70f, 0.30f, 0.22f),
                particles = ParticleStyle.Dust,
                statusMessages = new[]
                {
                    "Sharpening the pencils", "Stretching the canvases",
                    "Rehearsing the stealth walk", "Practising looking casual near paintings"
                },
                tips = new[]
                {
                    "Corrections only work while nobody is watching. Stay out of sight.",
                    "His routine is unpredictable. Never trust a pattern for long.",
                    "Walls and rooftops are your friends. Climb what you cannot walk around.",
                    "Small edits only. The canvas has limits, and so does his patience."
                }
            },
            new LoadingTheme
            {
                sceneKeywords = new[] { "forest", "apple", "newton", "orchard" },
                title = "THE ORCHARD AT WOOLSTHORPE",
                subtitle = "A VERY SLEEPY SCIENTIST",
                year = "1666",
                isPast = true,
                backgroundTop = C(0.02f, 0.08f, 0.06f),
                backgroundBottom = C(0.06f, 0.20f, 0.09f),
                accent = C(0.65f, 1.00f, 0.45f),
                secondary = C(1.00f, 0.82f, 0.30f),
                particles = ParticleStyle.Fireflies,
                statusMessages = new[]
                {
                    "Teaching apples about gravity", "Locating one very sleepy scientist",
                    "Muffling the crunchy leaves", "Checking the wind. It is windy."
                },
                tips = new[]
                {
                    "Catch the apples before they land. Every thud is noise.",
                    "Noise fills the Wake Meter. Keep it low.",
                    "The wind changes. Watch the branches, not the ground.",
                    "Stay quiet and keep moving. He sleeps lightly."
                }
            },
            new LoadingTheme
            {
                sceneKeywords = new[] { "beer", "tavern", "pub" },
                title = "THE TAVERN",
                subtitle = "A MEETING THAT CHANGES EVERYTHING",
                year = "1970",
                isPast = true,
                backgroundTop = C(0.12f, 0.05f, 0.02f),
                backgroundBottom = C(0.30f, 0.13f, 0.04f),
                accent = C(1.00f, 0.72f, 0.22f),
                secondary = C(0.85f, 0.32f, 0.12f),
                particles = ParticleStyle.Embers,
                statusMessages = new[]
                {
                    "Polishing the tankards", "Memorising the barman's routine",
                    "Locating the wrong beer", "Watering down history"
                },
                tips = new[]
                {
                    "Everyone has a routine. Learn it before you act.",
                    "Create a distraction, then make the swap.",
                    "Keep an eye on the Suspicion Meter.",
                    "Never run in a tavern. Walk like you belong."
                }
            },
            new LoadingTheme
            {
                sceneKeywords = new[] { "future", "hub", "city", "paradox" },
                title = "PARADOX CITY",
                subtitle = "THE FUTURE HUB",
                year = "2099",
                isPast = false,
                backgroundTop = C(0.02f, 0.03f, 0.12f),
                backgroundBottom = C(0.12f, 0.02f, 0.22f),
                accent = C(0.20f, 0.90f, 1.00f),
                secondary = C(1.00f, 0.25f, 0.80f),
                particles = ParticleStyle.DataStreaks,
                statusMessages = new[]
                {
                    "Recalibrating the skyline", "Charging the time machines",
                    "Asking BIBO to stop humming", "Defragmenting the paradox"
                },
                tips = new[]
                {
                    "Three time machines. One city. No pressure.",
                    "Talk to BIBO. He will not stop otherwise.",
                    "Look up sometimes. There is a blimp, and it is judging you.",
                    "The city is bigger than the time machines. Go exploring."
                }
            }
        };
    }

    // ------------------------------------------------------------------
    // Generated art (no image files needed)
    // ------------------------------------------------------------------
    private void BuildSharedSprites()
    {
        _spWhite = MakeSprite(MakeTexture(4, (x, y, n) => Color.white));

        _spCircle = MakeSprite(MakeTexture(128, (x, y, n) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f));
            return new Color(1f, 1f, 1f, Mathf.Clamp01(n / 2f - 1f - d + 0.5f));
        }));

        _spRing = MakeSprite(MakeTexture(256, (x, y, n) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f));
            float outer = n / 2f - 1f;
            float inner = outer - 5f;
            return new Color(1f, 1f, 1f, Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f));
        }));

        _spGlow = MakeSprite(MakeTexture(128, (x, y, n) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
            float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
            return new Color(1f, 1f, 1f, a);
        }));

        _spVignette = MakeSprite(MakeTexture(128, (x, y, n) =>
        {
            float nx = (x / (n - 1f) - 0.5f) * 2f;
            float ny = (y / (n - 1f) - 0.5f) * 2f;
            float d = Mathf.Sqrt(nx * nx + ny * ny) / 1.414f;
            return new Color(0f, 0f, 0f, Mathf.Pow(d, 2.4f) * 0.9f);
        }));
    }

    private Texture2D MakeTexture(int size, Func<int, int, int, Color> pixel)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = pixel(x, y, size);

        tex.SetPixels(pixels);
        tex.Apply();
        _sharedTextures.Add(tex);
        return tex;
    }

    private static Sprite MakeSprite(Texture2D tex)
    {
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    // ------------------------------------------------------------------
    // UI factory helpers
    // ------------------------------------------------------------------
    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void SetRect(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private Image NewImage(string name, Transform parent, Sprite sprite, Color color, bool raycast = false)
    {
        var rt = NewRect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    private TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color,
                                    Vector2 pos, Vector2 box, FontStyles style = FontStyles.Normal,
                                    TextAlignmentOptions align = TextAlignmentOptions.Center, float spacing = 0f)
    {
        var rt = NewRect(name, parent);
        SetRect(rt, pos, box);

        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (fontOverride != null) t.font = fontOverride;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = align;
        t.characterSpacing = spacing;
        t.raycastTarget = false;
        return t;
    }

    // ------------------------------------------------------------------
    // Build the screen
    // ------------------------------------------------------------------
    private void BuildScreen(LoadingTheme th, string sceneName)
    {
        DestroyScreen();

        _th = th;
        _t0 = Time.unscaledTime;
        _handAngle = Random.Range(0f, 360f);
        _lastStatusKey = _lastPercent = _tipSlot = -1;
        _yearLocked = false;
        _startYear = _lastYear;
        _yearNumeric = int.TryParse(th.year, out _targetYear);

        
        _statuses = new List<string>();
        if (th.statusMessages != null) _statuses.AddRange(th.statusMessages);
        _statuses.AddRange(CommonStatus);
        for (int i = _statuses.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string tmp = _statuses[i]; _statuses[i] = _statuses[j]; _statuses[j] = tmp;
        }
        _tipStart = (th.tips != null && th.tips.Length > 0) ? Random.Range(0, th.tips.Length) : 0;

        
        var canvasGo = new GameObject("ParadoxLoadingCanvas");
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = sortingOrder;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        _group = canvasGo.AddComponent<CanvasGroup>();
        _group.alpha = 1f;
        _group.blocksRaycasts = true;

        Transform root = canvasGo.transform;

        
        _bgTexture = new Texture2D(2, 256, TextureFormat.RGBA32, false);
        _bgTexture.wrapMode = TextureWrapMode.Clamp;
        _bgTexture.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < 256; y++)
        {
            Color c = Color.Lerp(th.backgroundBottom, th.backgroundTop, y / 255f);
            _bgTexture.SetPixel(0, y, c);
            _bgTexture.SetPixel(1, y, c);
        }
        _bgTexture.Apply();
        _bgSprite = Sprite.Create(_bgTexture, new Rect(0, 0, 2, 256), new Vector2(0.5f, 0.5f), 100f);

        var bg = NewImage("Background", root, _bgSprite, Color.white, true);
        Stretch(bg.rectTransform);

        
        var glow = NewImage("ClockGlow", root, _spGlow, A(th.accent, 0.22f));
        SetRect(glow.rectTransform, new Vector2(0f, 80f), new Vector2(1100f, 1100f));

        
        var particleLayer = NewRect("Particles", root);
        Stretch(particleLayer);
        BuildParticles(particleLayer, th);

        
        var band = NewImage("ScanBand", root, _spWhite, A(th.accent, 0.035f));
        _scanBand = band.rectTransform;
        SetRect(_scanBand, Vector2.zero, new Vector2(2200f, 150f));

        
        BuildClock(root, th);

        
        _titleBasePos = new Vector2(0f, 395f);
        Vector2 titleBox = new Vector2(1700f, 90f);

        NewText("DestinationLabel", root, "- DESTINATION -", 24f, A(th.accent, 0.8f),
                new Vector2(0f, 460f), new Vector2(800f, 40f), FontStyles.Normal, TextAlignmentOptions.Center, 14f);

        _titleRed = NewText("TitleRed", root, th.title, 66f, A(new Color(1f, 0.1f, 0.25f), 0f),
                            _titleBasePos, titleBox, FontStyles.Bold, TextAlignmentOptions.Center, 6f);
        _titleCyan = NewText("TitleCyan", root, th.title, 66f, A(new Color(0f, 1f, 1f), 0f),
                             _titleBasePos, titleBox, FontStyles.Bold, TextAlignmentOptions.Center, 6f);
        _titleMain = NewText("Title", root, th.title, 66f, Color.Lerp(Color.white, th.accent, 0.35f),
                             _titleBasePos, titleBox, FontStyles.Bold, TextAlignmentOptions.Center, 6f);

        NewText("Subtitle", root, th.subtitle, 28f, new Color(1f, 1f, 1f, 0.72f),
                new Vector2(0f, 335f), new Vector2(1400f, 44f), FontStyles.Normal, TextAlignmentOptions.Center, 8f);

        
        NewText("YearLabel", root, "TARGET YEAR", 20f, A(th.accent, 0.7f),
                new Vector2(0f, -130f), new Vector2(500f, 30f), FontStyles.Normal, TextAlignmentOptions.Center, 12f);

        _yearText = NewText("Year", root, "", 118f, th.secondary,
                            new Vector2(0f, -210f), new Vector2(1000f, 150f), FontStyles.Bold);

        
        var barBg = NewImage("BarBackground", root, _spWhite, new Color(1f, 1f, 1f, 0.12f));
        SetRect(barBg.rectTransform, new Vector2(0f, BarY), new Vector2(BarWidth, 10f));

        var fill = NewImage("BarFill", root, _spWhite, th.accent);
        _barFill = fill.rectTransform;
        _barFill.anchorMin = _barFill.anchorMax = new Vector2(0.5f, 0.5f);
        _barFill.pivot = new Vector2(0f, 0.5f);
        _barFill.anchoredPosition = new Vector2(-BarWidth / 2f, BarY);
        _barFill.sizeDelta = new Vector2(0f, 10f);

        var head = NewImage("BarHead", root, _spGlow, A(th.accent, 0.95f));
        _barHead = head.rectTransform;
        SetRect(_barHead, new Vector2(-BarWidth / 2f, BarY), new Vector2(90f, 90f));

        NewText("SyncLabel", root, "TEMPORAL SYNC", 20f, A(th.accent, 0.85f),
                new Vector2(-570f, BarY), new Vector2(230f, 30f), FontStyles.Normal, TextAlignmentOptions.Right, 6f);
        _percentText = NewText("Percent", root, "0%", 26f, Color.white,
                               new Vector2(555f, BarY), new Vector2(140f, 36f), FontStyles.Bold, TextAlignmentOptions.Left);

        _statusText = NewText("Status", root, "", 24f, A(Color.Lerp(Color.white, th.accent, 0.6f), 0.95f),
                              new Vector2(0f, -372f), new Vector2(1200f, 36f));

        
        var tipPanel = NewImage("TipPanel", root, _spWhite, new Color(0f, 0f, 0f, 0.38f));
        SetRect(tipPanel.rectTransform, new Vector2(0f, -450f), new Vector2(1240f, 64f));
        var tipBar = NewImage("TipAccent", tipPanel.transform, _spWhite, th.accent);
        SetRect(tipBar.rectTransform, new Vector2(-618f, 0f), new Vector2(4f, 64f));

        _tipText = NewText("Tip", tipPanel.transform, "", 24f, Color.white,
                           Vector2.zero, new Vector2(1180f, 60f), FontStyles.Italic);

        
        NewText("Brand", root, "PARADOX CITY", 28f, A(th.accent, 0.9f),
                new Vector2(-590f, 490f), new Vector2(620f, 40f), FontStyles.Bold, TextAlignmentOptions.Left, 10f);
        NewText("Transit", root, "TEMPORAL TRANSIT IN PROGRESS", 18f, new Color(1f, 1f, 1f, 0.45f),
                new Vector2(-590f, -508f), new Vector2(620f, 30f), FontStyles.Normal, TextAlignmentOptions.Left, 6f);
        NewText("Sector", root, "SECTOR // " + sceneName.ToUpperInvariant(), 18f, new Color(1f, 1f, 1f, 0.45f),
                new Vector2(590f, -508f), new Vector2(620f, 30f), FontStyles.Normal, TextAlignmentOptions.Right, 6f);

        
        var vig = NewImage("Vignette", root, _spVignette, Color.white);
        Stretch(vig.rectTransform);

        Tick(0f, 0f); 
    }

    private void BuildClock(Transform parent, LoadingTheme th)
    {
        _clockRoot = NewRect("Clock", parent);
        SetRect(_clockRoot, new Vector2(0f, 80f), new Vector2(ClockRadius * 2f, ClockRadius * 2f));

        var ring = NewImage("Ring", _clockRoot, _spRing, th.accent);
        SetRect(ring.rectTransform, Vector2.zero, new Vector2(ClockRadius * 2f, ClockRadius * 2f));

        var inner = NewImage("InnerRing", _clockRoot, _spRing, A(th.secondary, 0.65f));
        SetRect(inner.rectTransform, Vector2.zero, new Vector2(ClockRadius * 1.55f, ClockRadius * 1.55f));

        
        for (int i = 0; i < 12; i++)
        {
            bool major = i % 3 == 0;
            var holder = NewRect("Tick" + i, _clockRoot);
            SetRect(holder, Vector2.zero, new Vector2(ClockRadius * 2f, ClockRadius * 2f));
            holder.localRotation = Quaternion.Euler(0f, 0f, -i * 30f);

            var tick = NewImage("Mark", holder, _spWhite, A(th.accent, major ? 1f : 0.65f));
            SetRect(tick.rectTransform, new Vector2(0f, ClockRadius - (major ? 24f : 18f)),
                    new Vector2(major ? 6f : 3f, major ? 26f : 14f));
        }

        
        var hour = NewImage("HourHand", _clockRoot, _spWhite, Color.Lerp(Color.white, th.accent, 0.4f));
        _hourHand = hour.rectTransform;
        SetRect(_hourHand, Vector2.zero, new Vector2(9f, ClockRadius * 0.45f));
        _hourHand.pivot = new Vector2(0.5f, 0f);

        var minute = NewImage("MinuteHand", _clockRoot, _spWhite, Color.white);
        _minuteHand = minute.rectTransform;
        SetRect(_minuteHand, Vector2.zero, new Vector2(5f, ClockRadius * 0.68f));
        _minuteHand.pivot = new Vector2(0.5f, 0f);

        var dot = NewImage("Hub", _clockRoot, _spCircle, th.accent);
        SetRect(dot.rectTransform, Vector2.zero, new Vector2(22f, 22f));

        
        _orbiters.Clear();
        for (int i = 0; i < 3; i++)
        {
            var o = NewImage("Orbiter" + i, _clockRoot, _spGlow, A(i == 1 ? th.secondary : th.accent, 0.95f));
            SetRect(o.rectTransform, Vector2.zero, new Vector2(34f, 34f));
            _orbiters.Add(o.rectTransform);
        }
    }

    private void BuildParticles(RectTransform parent, LoadingTheme th)
    {
        _particles.Clear();

        for (int i = 0; i < particleCount; i++)
        {
            var p = new Particle();
            p.pos = new Vector2(Random.Range(-1000f, 1000f), Random.Range(-600f, 600f));
            p.phase = Random.Range(0f, 6.28f);
            p.speedMul = Random.Range(0.6f, 1.4f);

            Sprite sp;
            Vector2 size;
            Color col;

            switch (th.particles)
            {
                case ParticleStyle.DataStreaks:
                    sp = _spWhite;
                    size = new Vector2(Random.Range(2f, 4f), Random.Range(40f, 150f));
                    col = A(Random.value < 0.7f ? th.accent : th.secondary, Random.Range(0.15f, 0.55f));
                    p.vel = new Vector2(0f, Random.Range(260f, 620f));
                    break;

                case ParticleStyle.Dust:
                    sp = _spCircle;
                    size = Vector2.one * Random.Range(3f, 9f);
                    col = A(th.accent, Random.Range(0.12f, 0.4f));
                    p.vel = new Vector2(Random.Range(-12f, 12f), Random.Range(6f, 26f));
                    p.sway = 18f;
                    break;

                case ParticleStyle.Fireflies:
                    sp = _spGlow;
                    size = Vector2.one * Random.Range(22f, 50f);
                    col = A(Random.value < 0.6f ? th.accent : th.secondary, 0.9f);
                    p.vel = new Vector2(Random.Range(-25f, 25f), Random.Range(-10f, 25f));
                    p.sway = 40f;
                    p.pulse = Random.Range(1.2f, 2.8f);
                    break;

                default: // Embers
                    sp = _spGlow;
                    size = Vector2.one * Random.Range(8f, 18f);
                    col = A(Random.value < 0.6f ? th.accent : th.secondary, 0.9f);
                    p.vel = new Vector2(Random.Range(-10f, 10f), Random.Range(50f, 140f));
                    p.sway = 30f;
                    p.pulse = Random.Range(3f, 6f);
                    break;
            }

            var img = NewImage("P" + i, parent, sp, col);
            SetRect(img.rectTransform, p.pos, size);
            p.rt = img.rectTransform;
            p.img = img;
            p.baseColor = col;
            _particles.Add(p);
        }
    }

    private void DestroyScreen()
    {
        if (_canvas != null) Destroy(_canvas.gameObject);
        _canvas = null;
        _group = null;
        _particles.Clear();
        _orbiters.Clear();

        if (_bgSprite != null) Destroy(_bgSprite);
        if (_bgTexture != null) Destroy(_bgTexture);
        _bgSprite = null;
        _bgTexture = null;
    }

    // ------------------------------------------------------------------
    // Per-frame animation
    // ------------------------------------------------------------------
    private static float EaseOutCubic(float x) { return 1f - Mathf.Pow(1f - Mathf.Clamp01(x), 3f); }

    private static float FakeProgress(float t)
    {
        t = Mathf.Clamp01(t);
        for (int i = 0; i < ProgressTimes.Length - 1; i++)
        {
            if (t <= ProgressTimes[i + 1])
            {
                float local = Mathf.InverseLerp(ProgressTimes[i], ProgressTimes[i + 1], t);
                return Mathf.Lerp(ProgressValues[i], ProgressValues[i + 1], Mathf.SmoothStep(0f, 1f, local));
            }
        }
        return 1f;
    }

    private void Tick(float elapsed, float dt)
    {
        if (_th == null || _canvas == null) return;

        float fadeStart = Mathf.Max(0.5f, loadDuration - fadeOutDuration);
        float norm = Mathf.Clamp01(elapsed / fadeStart);

        
        float p = FakeProgress(norm);
        _barFill.sizeDelta = new Vector2(BarWidth * p, 10f);
        _barHead.anchoredPosition = new Vector2(-BarWidth / 2f + BarWidth * p, BarY);

        int pct = Mathf.FloorToInt(p * 100f);
        if (pct != _lastPercent)
        {
            _lastPercent = pct;
            _percentText.text = pct + "%";
        }

        
        float yearEnd = Mathf.Max(1f, fadeStart - 0.4f);
        float yt = Mathf.Clamp01(elapsed / yearEnd);
        UpdateYear(yt, elapsed);

        float punch = _yearLocked ? Mathf.Exp(-(elapsed - _lockTime) * 7f) : 0f;
        _yearText.rectTransform.localScale = Vector3.one * (1f + 0.22f * punch);
        _yearText.color = Color.Lerp(_th.secondary, _th.accent, Mathf.SmoothStep(0f, 1f, yt));

        
        float spin = Mathf.Lerp(1500f, 60f, EaseOutCubic(norm));
        float dir = _th.isPast ? 1f : -1f; 
        _handAngle += dir * spin * dt;
        _minuteHand.localRotation = Quaternion.Euler(0f, 0f, _handAngle);
        _hourHand.localRotation = Quaternion.Euler(0f, 0f, _handAngle / 12f);
        _clockRoot.localScale = Vector3.one * (1f + 0.05f * punch);

        for (int i = 0; i < _orbiters.Count; i++)
        {
            float a = elapsed * (0.9f + i * 0.55f) * dir + i * 2.1f;
            float r = ClockRadius + 22f + i * 16f;
            _orbiters[i].anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }

        
        bool burst = elapsed < 0.4f || (elapsed % 1.7f) < 0.16f;
        if (burst)
        {
            _titleRed.rectTransform.anchoredPosition = _titleBasePos + new Vector2(Random.Range(-12f, 4f), Random.Range(-4f, 4f));
            _titleCyan.rectTransform.anchoredPosition = _titleBasePos + new Vector2(Random.Range(-4f, 12f), Random.Range(-4f, 4f));
            _titleRed.alpha = 0.75f;
            _titleCyan.alpha = 0.75f;
        }
        else
        {
            _titleRed.alpha = 0f;
            _titleCyan.alpha = 0f;
        }

        // ---- status line ----
        int statusSlot = (int)(elapsed / 0.9f);
        int dots = (int)(elapsed * 3f) % 4;
        int statusKey = statusSlot * 4 + dots;
        if (statusKey != _lastStatusKey && _statuses.Count > 0)
        {
            _lastStatusKey = statusKey;
            _statusText.text = _statuses[statusSlot % _statuses.Count] + new string('.', dots);
        }

        
        if (_th.tips != null && _th.tips.Length > 0)
        {
            const float slotLength = 2.6f;
            int slot = (int)(elapsed / slotLength);
            if (slot != _tipSlot)
            {
                _tipSlot = slot;
                _tipText.text = "<b>BIBO TIP</b>   " + _th.tips[(_tipStart + slot) % _th.tips.Length];
            }
            _tipText.alpha = Mathf.Clamp01((elapsed % slotLength) / 0.3f);
        }

        
        _scanBand.anchoredPosition = new Vector2(0f, 760f - Mathf.Repeat(elapsed * 280f, 1520f));
        UpdateParticles(elapsed, dt);
    }

    private void UpdateYear(float yt, float elapsed)
    {
        if (_yearNumeric)
        {
            int v = Mathf.RoundToInt(Mathf.Lerp(_startYear, _targetYear, EaseOutCubic(yt)));
            _yearText.text = "<mspace=0.62em>" + v.ToString("0000") + "</mspace>";
        }
        else
        {
            
            string final = _th.year ?? "";
            if (Time.unscaledTime >= _nextScramble || yt >= 1f)
            {
                _nextScramble = Time.unscaledTime + 0.05f;
                var chars = new char[final.Length];
                for (int i = 0; i < chars.Length; i++)
                {
                    bool locked = yt >= (i + 1f) / (chars.Length + 1f);
                    chars[i] = locked ? final[i] : (char)('0' + Random.Range(0, 10));
                }
                _scrambleText = "<mspace=0.62em>" + new string(chars) + "</mspace>";
            }
            _yearText.text = _scrambleText;
        }

        if (yt >= 1f && !_yearLocked)
        {
            _yearLocked = true;
            _lockTime = elapsed;
        }
    }

    private void UpdateParticles(float elapsed, float dt)
    {
        foreach (var p in _particles)
        {
            p.pos += p.vel * p.speedMul * dt;
            if (p.sway > 0f)
                p.pos.x += Mathf.Sin(elapsed * 0.8f * p.speedMul + p.phase) * p.sway * dt;

            if (p.pos.y > 640f) { p.pos.y = -640f; p.pos.x = Random.Range(-1000f, 1000f); }
            else if (p.pos.y < -640f) { p.pos.y = 640f; }

            if (p.pos.x > 1060f) p.pos.x = -1060f;
            else if (p.pos.x < -1060f) p.pos.x = 1060f;

            p.rt.anchoredPosition = p.pos;

            if (p.pulse > 0f)
            {
                Color c = p.baseColor;
                c.a = p.baseColor.a * (0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(elapsed * p.pulse + p.phase)));
                p.img.color = c;
            }
        }
    }
}