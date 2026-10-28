using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuUIEvents : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
                                IPointerDownHandler, IDragHandler, IPointerClickHandler
{
    public System.Action enter, exit, click;
    public System.Action<PointerEventData> down, drag;

    public void OnPointerEnter(PointerEventData e) { if (enter != null) enter(); }
    public void OnPointerExit(PointerEventData e) { if (exit != null) exit(); }
    public void OnPointerDown(PointerEventData e) { if (down != null) down(e); }
    public void OnDrag(PointerEventData e) { if (drag != null) drag(e); }
    public void OnPointerClick(PointerEventData e) { if (click != null) click(); }
}

[System.Serializable]
public class MenuTagline
{
    [TextArea(1, 3)] public string text;
    [Tooltip("Optional. Shown under the quote.")] public string author;

    public MenuTagline() { }
    public MenuTagline(string text, string author = "") { this.text = text; this.author = author; }
}

public class UIMENUMANAGER : MonoBehaviour
{
    [Header("Scene references")]
    [Tooltip("Optional. Your OLD main menu panel. It is simply hidden, the new menu is built in code.")]
    public GameObject MainMenuPanel;
    public GameObject GameUIPanel;
    public GameObject mainplayer;

    [Header("Intro Cinematic Options")]
    public AudioClip introMusic;
    public TMP_FontAsset introFont;

    [Header("Logo - drag your RawImage here (or a texture below)")]
    [Tooltip("A RawImage from your scene. Its texture is copied into the menu and the original is hidden.")]
    public RawImage logoImage;
    [Tooltip("Alternative: drag a texture asset directly. Used if no RawImage is assigned.")]
    public Texture logoTexture;
    [SerializeField] private float logoMaxWidth = 760f;
    [SerializeField] private float logoMaxHeight = 320f;

    [Header("Tagline (wraps inside the title column and rotates)")]
    [Tooltip("Always shown first when the menu opens.")]
    [SerializeField]
    private MenuTagline firstTagline = new MenuTagline(
        "Only those who will risk going too far can possibly find out how far one can go.", "T. S. Eliot");

    [Tooltip("Picked at random, one after another, once the first has been shown.")]
    [SerializeField]
    private List<MenuTagline> otherTaglines = new List<MenuTagline>
    {
        new MenuTagline("Life can only be understood backwards; but it must be lived forwards.", "Søren Kierkegaard"),
        new MenuTagline("Of all sad words of tongue or pen, the saddest are these: “It might have been!”", "John Greenleaf Whittier"),
        new MenuTagline("The past is never dead. It’s not even past.", "William Faulkner"),
        new MenuTagline("Time is the longest distance between two places.", "Tennessee Williams"),
        new MenuTagline("Regret for the things we did can be tempered by time; it is regret for the things we did not do that is inconsolable.", "Sydney J. Harris"),
        new MenuTagline("What’s done cannot be undone.", "Shakespeare, Macbeth"),
        new MenuTagline("Lost time is never found again.", "Benjamin Franklin"),
        new MenuTagline("Every choice rewrites time."),
        new MenuTagline("You can rewrite the past. You can’t rewrite what it cost."),
        new MenuTagline("Go back far enough and you’ll meet the person you were before the mistake.")
    };

    [Tooltip("Seconds between tagline changes. 0 = never change, keep the first one.")]
    [SerializeField] private float taglineInterval = 9f;
    [Tooltip("Maximum width of the tagline. It wraps to new lines instead of running across the screen.")]
    [SerializeField] private float taglineWidth = 820f;
    [Tooltip("If the wrapped text is taller than this, the font shrinks until it fits.")]
    [SerializeField] private float taglineMaxHeight = 140f;
    [SerializeField] private int taglineFontSize = 32;
    [SerializeField] private int taglineMinFontSize = 20;
    [Tooltip("How dark the background is. Lower it to see the 3D scene behind the menu.")]
    [SerializeField] private float backgroundOpacity = 0.92f;

    private static bool s_menuShown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_menuShown = false;
    }

 

    enum View { Main, Settings }

    abstract class Item
    {
        public RectTransform root;
        public bool focused;
        protected float t;

        public virtual void Activate() { }
        public virtual void Adjust(int dir) { }
        public virtual void Refresh() { }
        public virtual void Tick(float dt) { t = Mathf.MoveTowards(t, focused ? 1f : 0f, dt * 9f); }
    }

    class ButtonItem : Item
    {
        public Text label, sub;
        public Image bg;
        public Color color = Color.white;
        public bool center;
        public float labelY, subY;
        public System.Action action;

        public override void Activate() { if (action != null) action(); }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            float e = Mathf.SmoothStep(0f, 1f, t);

            Color hot = new Color(color.r * 0.30f + 0.05f, color.g * 0.30f + 0.05f, color.b * 0.30f + 0.05f, 0.96f);
            bg.color = Color.Lerp(ButtonIdle, hot, e);
            label.color = Color.Lerp(new Color(1f, 1f, 1f, 0.88f), Color.white, e);
            root.localScale = Vector3.one * Mathf.Lerp(1f, 1.025f, e);

            if (!center)
            {
                float x = Mathf.Lerp(34f, 54f, e);
                label.rectTransform.anchoredPosition = new Vector2(x, labelY);
                if (sub != null) sub.rectTransform.anchoredPosition = new Vector2(x, subY);
            }
        }
    }

    abstract class RowItem : Item
    {
        public Image bg;
        public Text label;
        public System.Action sfx;

        public override void Tick(float dt)
        {
            base.Tick(dt);
            float e = Mathf.SmoothStep(0f, 1f, t);
            bg.color = Color.Lerp(RowIdle, RowHot, e);
            label.color = Color.Lerp(new Color(1f, 1f, 1f, 0.82f), Color.white, e);
        }
    }

    class CyclerItem : RowItem
    {
        public Text value;
        public string[] options;
        public System.Func<int> get;
        public System.Action<int> set;

        public override void Adjust(int dir)
        {
            int n = options.Length;
            if (n == 0) return;
            int i = ((get() + dir) % n + n) % n;
            set(i);
            Refresh();
            if (sfx != null) sfx();
        }

        public override void Activate() { Adjust(1); }

        public override void Refresh()
        {
            int n = options.Length;
            value.text = n == 0 ? "" : options[Mathf.Clamp(get(), 0, n - 1)];
        }
    }

    class SliderItem : RowItem
    {
        public Text value;
        public RectTransform track, fill, handle;
        public float trackWidth = 340f, min, max, step, keyStep;
        public System.Func<float> get;
        public System.Action<float> set;
        public System.Func<float, string> fmt;

        public void SetFromPointer(PointerEventData e)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(track, e.position, e.pressEventCamera, out local)) return;
            SetNormalized((local.x + trackWidth * track.pivot.x) / trackWidth);
        }

        public void SetNormalized(float n)
        {
            float v = Mathf.Lerp(min, max, Mathf.Clamp01(n));
            if (step > 0f) v = Mathf.Round(v / step) * step;
            set(Mathf.Clamp(v, min, max));
            Refresh();
        }

        public override void Adjust(int dir)
        {
            set(Mathf.Clamp(get() + dir * keyStep, min, max));
            Refresh();
            if (sfx != null) sfx();
        }

        public override void Refresh()
        {
            float v = get();
            float n = Mathf.InverseLerp(min, max, v);
            fill.sizeDelta = new Vector2(trackWidth * n, fill.sizeDelta.y);
            handle.anchoredPosition = new Vector2(trackWidth * n, 0f);
            value.text = fmt != null ? fmt(v) : v.ToString("0.00");
        }
    }

    class MenuPanel
    {
        public CanvasGroup group;
        public RectTransform rt, scaleRt;
        public Vector2 basePos;
        public float slide, scaleFrom = 1f, target;
    }

    class Mote
    {
        public RectTransform rt;
        public Image img;
        public float x, y, speed, drift, phase, alpha;
    }


    static readonly Color Accent = new Color(0.12f, 0.82f, 1f);
    static readonly Color Red = new Color(0.86f, 0.09f, 0.11f);
    static readonly Color PanelFace = new Color(0.04f, 0.05f, 0.07f, 0.94f);
    static readonly Color AccentBorder = new Color(Accent.r, Accent.g, Accent.b, 0.8f);
    static readonly Color ButtonIdle = new Color(1f, 1f, 1f, 0.06f);
    static readonly Color RowIdle = new Color(1f, 1f, 1f, 0.05f);
    static readonly Color RowHot = new Color(Accent.r, Accent.g, Accent.b, 0.28f);

    static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);
    static readonly Vector2 TL = new Vector2(0f, 1f);
    static readonly Vector2 TC = new Vector2(0.5f, 1f);
    static readonly Vector2 BC = new Vector2(0.5f, 0f);
    static readonly Vector2 BL = new Vector2(0f, 0f);
    static readonly Vector2 BR = new Vector2(1f, 0f);
    static readonly Vector2 ML = new Vector2(0f, 0.5f);
    static readonly Vector2 MR = new Vector2(1f, 0.5f);

    const string Prefs = "ArtPause_";   

 

    bool ready, uiVisible, starting, dirty;
    View view = View.Main;

    static bool defaultsKnown;
    static bool defVsync = true;
    static int defQuality;

    float master = 1f;
    bool mute, menuSounds = true, vsync;
    int quality;

    GameObject root;
    CanvasGroup rootGroup;
    float rootTarget;
    Text hintText;
    RectTransform logoRect;
    Vector2 logoBase;

    Text taglineText;
    Image taglineBar;
    CanvasGroup taglineGroup;
    MenuTagline currentTagline, pendingTagline;
    readonly List<MenuTagline> taglineBag = new List<MenuTagline>();
    enum TaglinePhase { Hold, FadeOut, FadeIn }
    TaglinePhase taglinePhase = TaglinePhase.Hold;
    float taglineTimer, taglineFade = 1f;
    const float TaglineFadeTime = 0.45f;

    MenuPanel titlePanel, mainPanel, settingsPanel;
    readonly List<MenuPanel> panels = new List<MenuPanel>();
    readonly List<Item> allItems = new List<Item>();
    readonly List<Item> mainItems = new List<Item>();
    readonly List<Item> settingsList = new List<Item>();
    readonly List<Mote> motes = new List<Mote>();
    ButtonItem resetBtn, backBtn;

    List<Item> focusList = new List<Item>();
    int focusIndex;

    AudioSource uiAudio;
    AudioClip sTick, sClick, sBack, sOpen;

    static Font uiFont;
    static Sprite vignetteSprite, leftShadeSprite;

    
    static FieldInfo s_blockField;
    static bool s_blockSearched;

    static void SetPauseBlocked(bool value)
    {
        if (!s_blockSearched)
        {
            s_blockSearched = true;
            System.Type t = System.Type.GetType("FuturePauseMenu");
            if (t != null) s_blockField = t.GetField("BlockPause", BindingFlags.Public | BindingFlags.Static);
        }
        if (s_blockField != null) s_blockField.SetValue(null, value);
    }



    private void Start()
    {
        if (s_menuShown)
        {
            
            StartGameplay();
            return;
        }

        Time.timeScale = 0f;

        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetPauseBlocked(true);

        if (MainMenuPanel != null) MainMenuPanel.SetActive(false);   
        if (GameUIPanel != null) GameUIPanel.SetActive(false);

        CaptureDefaults();
        LoadSettings();
        BuildSounds();
        EnsureEventSystem();
        BuildUI();
        ApplyAll();
        RefreshAllItems();

        uiVisible = true;
        rootTarget = 1f;
        ShowMainView(0);
        Play(sOpen);
        ready = true;
    }

    public void PlayGame()
    {
        if (starting) return;
        starting = true;
        s_menuShown = true;

        SaveIfDirty();

        
        SetUIVisible(false);
        if (MainMenuPanel != null) MainMenuPanel.SetActive(false);

        
        ParadoxIntroCinematic.Play(StartGameplay, introMusic, introFont);
    }

    private void StartGameplay()
    {
        Time.timeScale = 1f;
        SetUIVisible(false);
        if (MainMenuPanel != null) MainMenuPanel.SetActive(false);
        if (GameUIPanel != null) GameUIPanel.SetActive(true);
        if (mainplayer != null) mainplayer.SetActive(true);

        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetPauseBlocked(false);
    }

    public void ExitGame()
    {
        SaveIfDirty();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void SetUIVisible(bool visible)
    {
        uiVisible = visible;
        if (root != null) root.SetActive(visible);
    }

    void OnDestroy()
    {
        SaveIfDirty();
        if (uiVisible) SetPauseBlocked(false);
    }

    void OnApplicationQuit() { SaveIfDirty(); }

    void Update()
    {
        if (!ready || !uiVisible) return;
        float dt = Time.unscaledDeltaTime;   

        // keep the cursor usable ---->> I need to Double Check this
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        HandleKeys();
        AnimateUI(dt);
    }

   

    void ShowMainView(int focus)
    {
        view = View.Main;
        SetPanel(titlePanel, true);
        SetPanel(mainPanel, true);
        SetPanel(settingsPanel, false);
        SetFocusList(mainItems, focus);
        UpdateHint();
    }

    void EnterSettings()
    {
        Play(sClick);
        view = View.Settings;
        SetPanel(mainPanel, false);
        SetPanel(titlePanel, false);
        SetPanel(settingsPanel, true);
        RefreshAllItems();
        SetFocusList(settingsList, 0);
        UpdateHint();
    }

    void LeaveSettings()
    {
        SaveIfDirty();
        Play(sBack);
        view = View.Main;
        SetPanel(settingsPanel, false);
        SetPanel(titlePanel, true);
        SetPanel(mainPanel, true);
        SetFocusList(mainItems, 1);
        UpdateHint();
    }

    void SetPanel(MenuPanel p, bool on)
    {
        p.target = on ? 1f : 0f;
        p.group.blocksRaycasts = on;
        p.group.interactable = on;
    }

    void UpdateHint()
    {
        hintText.text = view == View.Settings
            ? "W / S  select        A / D  adjust        ESC  back"
            : "W / S  navigate        ENTER  select";
    }

  
    void SetFocusList(List<Item> list, int start)
    {
        focusList = list;
        foreach (Item it in allItems) it.focused = false;
        focusIndex = list.Count == 0 ? 0 : Mathf.Clamp(start, 0, list.Count - 1);
        if (list.Count > 0) list[focusIndex].focused = true;
    }

    void SetFocusIndex(int i)
    {
        if (focusList.Count == 0) return;
        if (focusIndex >= 0 && focusIndex < focusList.Count) focusList[focusIndex].focused = false;
        focusIndex = Mathf.Clamp(i, 0, focusList.Count - 1);
        focusList[focusIndex].focused = true;
    }

    void MoveFocus(int dir)
    {
        int n = focusList.Count;
        if (n == 0) return;
        SetFocusIndex((focusIndex + dir + n) % n);
        Play(sTick);
    }

    void Hover(Item it)
    {
        int i = focusList.IndexOf(it);
        if (i < 0 || i == focusIndex) return;
        SetFocusIndex(i);
        Play(sTick);
    }

    void FocusOnly(Item it)
    {
        int i = focusList.IndexOf(it);
        if (i >= 0) SetFocusIndex(i);
    }

    void ClickItem(Item it)
    {
        int i = focusList.IndexOf(it);
        if (i < 0) return;
        SetFocusIndex(i);
        ActivateItem(it);
    }

    void ActivateItem(Item it)
    {
        if (!(it is RowItem)) Play(sClick);
        it.Activate();
    }

    void HandleKeys()
    {
        if (view == View.Settings && Input.GetKeyDown(KeyCode.Escape)) { LeaveSettings(); return; }
        if (focusList.Count == 0) return;

        bool up = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
        bool down = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
        bool left = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
        bool right = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);

        if (up) MoveFocus(-1);
        if (down) MoveFocus(1);

        Item cur = focusList[focusIndex];
        if (left) cur.Adjust(-1);
        if (right) cur.Adjust(1);

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            ActivateItem(cur);
    }



    void CaptureDefaults()
    {
        if (defaultsKnown) return;
        defVsync = QualitySettings.vSyncCount > 0;
        defQuality = QualitySettings.GetQualityLevel();
        defaultsKnown = true;
    }

    void LoadSettings()
    {
        master = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefs + "Master", 1f));
        mute = PlayerPrefs.GetInt(Prefs + "Mute", 0) == 1;
        menuSounds = PlayerPrefs.GetInt(Prefs + "MenuSfx", 1) == 1;
        vsync = PlayerPrefs.GetInt(Prefs + "VSync", defVsync ? 1 : 0) == 1;
        quality = Mathf.Clamp(PlayerPrefs.GetInt(Prefs + "Quality", defQuality), 0, Mathf.Max(0, QualitySettings.names.Length - 1));
    }

    void SaveIfDirty()
    {
        if (!dirty) return;
        dirty = false;

        PlayerPrefs.SetFloat(Prefs + "Master", master);
        PlayerPrefs.SetInt(Prefs + "Mute", mute ? 1 : 0);
        PlayerPrefs.SetInt(Prefs + "MenuSfx", menuSounds ? 1 : 0);
        PlayerPrefs.SetInt(Prefs + "VSync", vsync ? 1 : 0);
        PlayerPrefs.SetInt(Prefs + "Quality", quality);
        PlayerPrefs.Save();
    }

    void ResetDefaults()
    {
        master = 1f;
        mute = false;
        menuSounds = true;
        vsync = defVsync;
        quality = Mathf.Clamp(defQuality, 0, Mathf.Max(0, QualitySettings.names.Length - 1));

        dirty = true;
        ApplyAll();
        RefreshAllItems();
        SaveIfDirty();
        Play(sClick);
    }

    void ApplyAll()
    {
        ApplyAudio();
        ApplyGraphics();
    }

    void ApplyAudio() { AudioListener.volume = mute ? 0f : master; }

    void ApplyGraphics()
    {
        if (QualitySettings.GetQualityLevel() != quality) QualitySettings.SetQualityLevel(quality, true);
        int want = vsync ? 1 : 0;
        if (QualitySettings.vSyncCount != want) QualitySettings.vSyncCount = want;
    }

    void RefreshAllItems()
    {
        foreach (Item it in allItems) it.Refresh();
    }



    void BuildSounds()
    {
        uiAudio = gameObject.AddComponent<AudioSource>();
        uiAudio.playOnAwake = false;
        uiAudio.spatialBlend = 0f;
        uiAudio.ignoreListenerPause = true;

        sTick = Tone("ui_tick", 900f, 900f, 0.04f, 0.22f);
        sClick = Tone("ui_click", 520f, 820f, 0.10f, 0.35f);
        sBack = Tone("ui_back", 480f, 300f, 0.11f, 0.30f);
        sOpen = Tone("ui_open", 300f, 640f, 0.20f, 0.30f);
    }

    static AudioClip Tone(string name, float f0, float f1, float dur, float vol)
    {
        const int rate = 44100;
        int n = Mathf.Max(1, (int)(rate * dur));
        float[] data = new float[n];
        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            float k = i / (float)n;
            phase += 2f * Mathf.PI * Mathf.Lerp(f0, f1, k) / rate;
            float env = Mathf.Min(1f, i / 120f) * Mathf.Exp(-k * 5f);
            data[i] = Mathf.Sin(phase) * env * vol;
        }

        AudioClip c = AudioClip.Create(name, n, 1, rate, false);
        c.SetData(data, 0);
        return c;
    }

    void Play(AudioClip c)
    {
        if (!menuSounds || uiAudio == null || c == null) return;
        uiAudio.PlayOneShot(c, 1f);
    }



    void AnimateUI(float dt)
    {
        rootGroup.alpha = Mathf.MoveTowards(rootGroup.alpha, rootTarget, dt * 2.5f);

        foreach (MenuPanel p in panels)
        {
            float a = Mathf.MoveTowards(p.group.alpha, p.target, dt * 6f);
            p.group.alpha = a;
            float e = Mathf.SmoothStep(0f, 1f, a);
            if (p.slide != 0f) p.rt.anchoredPosition = p.basePos + new Vector2((1f - e) * p.slide, 0f);
            if (p.scaleRt != null) p.scaleRt.localScale = Vector3.one * Mathf.Lerp(p.scaleFrom, 1f, e);
        }

        foreach (Item it in allItems) it.Tick(dt);

        UpdateTagline(dt);
        
        if (logoRect != null)
            logoRect.anchoredPosition = logoBase + new Vector2(0f, Mathf.Sin(Time.unscaledTime * 0.8f) * 6f);

        
        foreach (Mote m in motes)
        {
            m.y += m.speed * dt;
            m.x += Mathf.Sin(Time.unscaledTime * 0.4f + m.phase) * m.drift * dt;
            if (m.y > 580f)
            {
                m.y = -580f;
                m.x = Random.Range(-980f, 980f);
            }
            m.rt.anchoredPosition = new Vector2(m.x, m.y);

            float tw = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 1.3f + m.phase * 3f);
            m.img.color = new Color(Accent.r, Accent.g, Accent.b, m.alpha * tw);
        }
    }

 

    static Font GetFont()
    {
        if (uiFont != null) return uiFont;
        uiFont = ExamUtil.UIFont;   
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
        return uiFont;
    }

    static RectTransform NewRect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static RectTransform Stretch(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    static Image Img(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
    {
        RectTransform rt = NewRect(name, parent, anchor, pivot, pos, size);
        Image im = rt.gameObject.AddComponent<Image>();
        im.color = color;
        im.raycastTarget = false;
        if (sprite != null) im.sprite = sprite;
        return im;
    }

    static Image StretchImg(string name, Transform parent, Color color, Sprite sprite)
    {
        RectTransform rt = Stretch(name, parent);
        Image im = rt.gameObject.AddComponent<Image>();
        im.color = color;
        im.raycastTarget = false;
        if (sprite != null) im.sprite = sprite;
        return im;
    }

    static Text Txt(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
                    string content, int fontSize, TextAnchor align, Color color, bool bold, bool wrap)
    {
        RectTransform rt = NewRect(name, parent, anchor, pivot, pos, size);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = GetFont();
        t.supportRichText = true;
        t.text = content;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = color;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.raycastTarget = false;
        t.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static void AddOutline(Text t, float dist = 2f)
    {
        Outline o = t.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.9f);
        o.effectDistance = new Vector2(dist, -dist);
    }

    static void Frame(RectTransform parent, Color face, Color border)
    {
        RectTransform b = Stretch("Border", parent);
        Image bi = b.gameObject.AddComponent<Image>();
        bi.color = border;
        bi.raycastTarget = true;

        RectTransform f = Stretch("Face", parent);
        f.offsetMin = new Vector2(2f, 2f);
        f.offsetMax = new Vector2(-2f, -2f);
        Image fi = f.gameObject.AddComponent<Image>();
        fi.color = face;
        fi.raycastTarget = false;
    }

    MenuPanel NewPanel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, float slide, float scaleFrom)
    {
        RectTransform r = NewRect(name, parent, anchor, pivot, pos, size);
        CanvasGroup g = r.gameObject.AddComponent<CanvasGroup>();
        g.alpha = 0f;
        g.blocksRaycasts = false;
        g.interactable = false;

        MenuPanel p = new MenuPanel { group = g, rt = r, basePos = pos, slide = slide, scaleFrom = scaleFrom };
        if (scaleFrom < 1f) p.scaleRt = r;
        panels.Add(p);
        return p;
    }

    static MainMenuUIEvents AddEv(GameObject go) { return go.AddComponent<MainMenuUIEvents>(); }

    void Wire(Item it, RectTransform target)
    {
        MainMenuUIEvents ev = AddEv(target.gameObject);
        ev.enter = () => Hover(it);
        ev.click = () => ClickItem(it);
        allItems.Add(it);
    }

    static Sprite VignetteSprite()
    {
        if (vignetteSprite != null) return vignetteSprite;

        const int n = 128;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f;
                float v = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(u * u + v * v) / 1.414f;
                px[y * n + x] = new Color(0f, 0f, 0f, Mathf.SmoothStep(0.3f, 1f, d) * 0.85f);
            }
        }
        tex.SetPixels(px);
        tex.Apply();

        vignetteSprite = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f);
        return vignetteSprite;
    }

    static Sprite LeftShadeSprite()
    {
        if (leftShadeSprite != null) return leftShadeSprite;

        const int w = 64;
        Texture2D tex = new Texture2D(w, 2, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] px = new Color[w * 2];
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = new Color(0f, 0f, 0f, Mathf.Pow(1f - x / (float)(w - 1), 1.6f));
        tex.SetPixels(px);
        tex.Apply();

        leftShadeSprite = Sprite.Create(tex, new Rect(0f, 0f, w, 2f), new Vector2(0.5f, 0.5f), 100f);
        return leftShadeSprite;
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem (MainMenu)");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }



    void BuildUI()
    {
        GameObject cg = new GameObject("MainMenu_UI");
        cg.transform.SetParent(transform, false);
        Canvas canvas = cg.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;   

        CanvasScaler sc = cg.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;
        cg.AddComponent<GraphicRaycaster>();

        RectTransform rr = Stretch("Root", canvas.transform);
        root = rr.gameObject;
        rootGroup = root.AddComponent<CanvasGroup>();
        rootGroup.alpha = 0f;

        Image bg = StretchImg("Background", rr, new Color(0.015f, 0.025f, 0.04f, backgroundOpacity), null);
        bg.raycastTarget = true;   

        BuildMotes(rr);

        Image shade = Img("LeftShade", rr, ML, ML, Vector2.zero, new Vector2(1300f, 0f), new Color(1f, 1f, 1f, 0.9f), LeftShadeSprite());
        shade.rectTransform.anchorMin = new Vector2(0f, 0f);
        shade.rectTransform.anchorMax = new Vector2(0f, 1f);
        shade.rectTransform.sizeDelta = new Vector2(1300f, 0f);

        StretchImg("Vignette", rr, Color.white, VignetteSprite());

        BuildTitle(rr);
        BuildMain(rr);
        BuildSettings(rr);

        hintText = Txt("Hint", rr, BC, BC, new Vector2(0f, 30f), new Vector2(1700f, 30f), "", 20,
                       TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.5f), false, false);

        Txt("Version", rr, BL, BL, new Vector2(40f, 28f), new Vector2(300f, 26f), "v" + Application.version, 18,
            TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.3f), false, false);
    }

    void BuildMotes(RectTransform rr)
    {
        RectTransform layer = Stretch("Motes", rr);

        for (int i = 0; i < 45; i++)
        {
            float size = Random.Range(2f, 5f);
            Mote m = new Mote
            {
                x = Random.Range(-980f, 980f),
                y = Random.Range(-580f, 580f),
                speed = Random.Range(8f, 30f),
                drift = Random.Range(4f, 22f),
                phase = Random.value * 6.28f,
                alpha = Random.Range(0.25f, 0.8f)
            };
            m.img = Img("Mote", layer, Mid, Mid, new Vector2(m.x, m.y), new Vector2(size, size), Accent);
            m.rt = m.img.rectTransform;
            motes.Add(m);
        }
    }

    void BuildTitle(RectTransform rr)
    {
        titlePanel = NewPanel("TitleBlock", rr, ML, ML, new Vector2(150f, 20f), new Vector2(900f, 640f), -90f, 1f);
        RectTransform b = titlePanel.rt;

        Texture tex = logoImage != null ? logoImage.texture : logoTexture;
        float nextY;

        if (tex != null)
        {
            
            float aspect = tex.height > 0 ? tex.width / (float)tex.height : 1f;
            float w = logoMaxWidth, h = logoMaxHeight;
            if (w / h > aspect) w = h * aspect; else h = w / aspect;

            RectTransform lr = NewRect("Logo", b, TL, TL, new Vector2(0f, -40f), new Vector2(w, h));
            RawImage raw = lr.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.raycastTarget = false;

            if (logoImage != null)
            {
                raw.color = logoImage.color;
                raw.uvRect = logoImage.uvRect;
                if (logoImage.material != null) raw.material = logoImage.material;
                logoImage.gameObject.SetActive(false);   
            }

            logoRect = lr;
            logoBase = lr.anchoredPosition;
            nextY = -40f - h - 30f;
        }
        else
        {
            Text big = Txt(
    "Title",
    b,
    TL,
    TL,
    new Vector2(-6f, -40f),
    new Vector2(900f, 190f),
    "YOUR GAME TITLE",
    130,
    TextAnchor.UpperLeft,
    Color.white,
    true,
    false
);
            AddOutline(big, 3f);
            nextY = -40f - 190f;
        }

        float tw = Mathf.Min(taglineWidth, b.sizeDelta.x - 30f);   // never wider than the title column

        taglineBar = Img("TaglineBar", b, TL, TL, new Vector2(0f, nextY), new Vector2(6f, 44f), Accent);
        taglineText = Txt("Tagline", b, TL, TL, new Vector2(24f, nextY - 2f), new Vector2(tw, 44f), "", taglineFontSize,
                          TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.85f), false, true);   // true = wrap
        taglineGroup = taglineText.gameObject.AddComponent<CanvasGroup>();

        currentTagline = (firstTagline != null && !string.IsNullOrEmpty(firstTagline.text))
            ? firstTagline
            : new MenuTagline("Only those who will risk going too far can possibly find out how far one can go.", "T. S. Eliot");
        ApplyTagline(currentTagline);
    }

    void BuildMain(RectTransform rr)
    {
        mainPanel = NewPanel("MainPanel", rr, MR, MR, new Vector2(-170f, 0f), new Vector2(640f, 460f), 80f, 1f);
        RectTransform p = mainPanel.rt;
        Frame(p, PanelFace, AccentBorder);

        Txt("Header", p, TC, TC, new Vector2(0f, -30f), new Vector2(560f, 36f), "MAIN MENU", 22, TextAnchor.MiddleCenter, Accent, true, false);
        Img("Divider", p, TC, TC, new Vector2(0f, -72f), new Vector2(560f, 2f), new Color(1f, 1f, 1f, 0.15f));

        string[] names = { "PLAY", "SETTINGS", "QUIT GAME" };
        string[] subs = { "Begin your journey", "Audio and video", "Exit to desktop" };
        Color[] cols = { Accent, Accent, Red };
        System.Action[] acts = { PlayGame, EnterSettings, ExitGame };

        for (int i = 0; i < names.Length; i++)
        {
            ButtonItem b = MakeButton(p, names[i], subs[i], TC, TC, new Vector2(0f, -96f - i * 108f), new Vector2(560f, 94f),
                                      cols[i], 34, acts[i], false);
            mainItems.Add(b);
        }
    }

    ButtonItem MakeButton(Transform parent, string label, string sub, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
                          Color color, int fontSize, System.Action action, bool center)
    {
        RectTransform r = NewRect("Btn_" + label, parent, anchor, pivot, pos, size);
        Image bg = r.gameObject.AddComponent<Image>();
        bg.color = ButtonIdle;
        bg.raycastTarget = true;

        ButtonItem b = new ButtonItem { root = r, bg = bg, color = color, center = center, action = action };

        if (center)
        {
            b.label = Txt("Label", r, Mid, Mid, Vector2.zero, size, label, fontSize, TextAnchor.MiddleCenter, Color.white, true, false);
        }
        else
        {
            b.labelY = string.IsNullOrEmpty(sub) ? 0f : 11f;
            b.subY = -22f;
            b.label = Txt("Label", r, ML, ML, new Vector2(34f, b.labelY), new Vector2(size.x - 60f, fontSize + 14f), label, fontSize,
                          TextAnchor.MiddleLeft, Color.white, true, false);
            if (!string.IsNullOrEmpty(sub))
                b.sub = Txt("Sub", r, ML, ML, new Vector2(34f, b.subY), new Vector2(size.x - 60f, 26f), sub, 19,
                            TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.5f), false, false);
        }

        Wire(b, r);
        return b;
    }

    void BuildSettings(RectTransform rr)
    {
        settingsPanel = NewPanel("SettingsPanel", rr, Mid, Mid, Vector2.zero, new Vector2(1100f, 700f), 0f, 0.95f);
        RectTransform p = settingsPanel.rt;
        Frame(p, PanelFace, AccentBorder);

        Txt("Header", p, TL, TL, new Vector2(70f, -30f), new Vector2(700f, 64f), "SETTINGS", 48, TextAnchor.MiddleLeft, Color.white, true, false);
        Img("HeaderLine", p, TL, TL, new Vector2(70f, -96f), new Vector2(960f, 2f), new Color(1f, 1f, 1f, 0.15f));

        RectTransform rows = NewRect("Rows", p, TC, TC, new Vector2(0f, -130f), new Vector2(960f, 420f));
        int r = 0;

        settingsList.Add(MakeSlider(rows, r++, "Master Volume", 0f, 1f, 0.01f, 0.05f,
                                    () => master, v => { master = v; ApplyAudio(); dirty = true; }, v => Mathf.RoundToInt(v * 100f) + "%"));
        settingsList.Add(MakeToggle(rows, r++, "Mute All Audio", () => mute, b => { mute = b; ApplyAudio(); dirty = true; }));
        settingsList.Add(MakeToggle(rows, r++, "Menu Sounds", () => menuSounds, b => { menuSounds = b; dirty = true; }));
        settingsList.Add(MakeCycler(rows, r++, "Graphics Quality", QualitySettings.names,
                                    () => quality, i => { quality = i; ApplyGraphics(); dirty = true; }));
        settingsList.Add(MakeToggle(rows, r++, "VSync", () => vsync, b => { vsync = b; ApplyGraphics(); dirty = true; }));

        resetBtn = MakeButton(p, "RESET TO DEFAULTS", null, BL, BL, new Vector2(70f, 40f), new Vector2(350f, 64f),
                              Accent, 24, ResetDefaults, true);
        backBtn = MakeButton(p, "BACK", null, BR, BR, new Vector2(-70f, 40f), new Vector2(240f, 64f), Accent, 26, LeaveSettings, true);
        settingsList.Add(resetBtn);
        settingsList.Add(backBtn);
    }

    void InitRow(RowItem it, Transform parent, int index, string label, float labelWidth)
    {
        RectTransform r = NewRect("Row_" + label, parent, TC, TC, new Vector2(0f, -index * 80f), new Vector2(960f, 72f));
        it.root = r;

        it.bg = r.gameObject.AddComponent<Image>();
        it.bg.color = RowIdle;
        it.bg.raycastTarget = true;

        it.label = Txt("Label", r, ML, ML, new Vector2(30f, 0f), new Vector2(labelWidth, 50f), label, 28,
                       TextAnchor.MiddleLeft, Color.white, false, false);
        it.sfx = () => Play(sTick);

        Wire(it, r);
    }

    CyclerItem MakeCycler(Transform parent, int index, string label, string[] options, System.Func<int> get, System.Action<int> set)
    {
        CyclerItem it = new CyclerItem { options = options, get = get, set = set };
        InitRow(it, parent, index, label, 480f);
        RectTransform r = it.root;

        Image right = Img("ArrowR", r, MR, MR, new Vector2(-20f, 0f), new Vector2(48f, 48f), new Color(1f, 1f, 1f, 0.10f));
        right.raycastTarget = true;
        Text rg = Txt("Glyph", right.rectTransform, Mid, Mid, Vector2.zero, new Vector2(48f, 48f), ">", 30, TextAnchor.MiddleCenter, Accent, true, false);
        rg.supportRichText = false;
        AddEv(right.gameObject).click = () => { FocusOnly(it); it.Adjust(1); };

        Image left = Img("ArrowL", r, MR, MR, new Vector2(-372f, 0f), new Vector2(48f, 48f), new Color(1f, 1f, 1f, 0.10f));
        left.raycastTarget = true;
        Text lg = Txt("Glyph", left.rectTransform, Mid, Mid, Vector2.zero, new Vector2(48f, 48f), "<", 30, TextAnchor.MiddleCenter, Accent, true, false);
        lg.supportRichText = false;
        AddEv(left.gameObject).click = () => { FocusOnly(it); it.Adjust(-1); };

        it.value = Txt("Value", r, MR, MR, new Vector2(-72f, 0f), new Vector2(296f, 48f), "", 26, TextAnchor.MiddleCenter, Color.white, true, false);
        it.value.supportRichText = false;

        return it;
    }

    CyclerItem MakeToggle(Transform parent, int index, string label, System.Func<bool> get, System.Action<bool> set)
    {
        return MakeCycler(parent, index, label, new[] { "OFF", "ON" }, () => get() ? 1 : 0, i => set(i == 1));
    }

    SliderItem MakeSlider(Transform parent, int index, string label, float min, float max, float step, float keyStep,
                          System.Func<float> get, System.Action<float> set, System.Func<float, string> fmt)
    {
        SliderItem it = new SliderItem { min = min, max = max, step = step, keyStep = keyStep, get = get, set = set, fmt = fmt };
        InitRow(it, parent, index, label, 400f);
        RectTransform r = it.root;

        it.value = Txt("Value", r, MR, MR, new Vector2(-24f, 0f), new Vector2(110f, 48f), "", 26, TextAnchor.MiddleRight, Color.white, true, false);

        
        RectTransform hit = NewRect("Hit", r, MR, MR, new Vector2(-150f, 0f), new Vector2(380f, 56f));
        Image hitImg = hit.gameObject.AddComponent<Image>();
        hitImg.color = new Color(0f, 0f, 0f, 0f);
        hitImg.raycastTarget = true;

        Image track = Img("Track", hit, Mid, Mid, Vector2.zero, new Vector2(it.trackWidth, 12f), new Color(1f, 1f, 1f, 0.16f));
        Image fill = Img("Fill", track.rectTransform, ML, ML, Vector2.zero, new Vector2(0f, 12f), Accent);
        Image handle = Img("Handle", track.rectTransform, ML, Mid, Vector2.zero, new Vector2(24f, 30f), Color.white);

        it.track = track.rectTransform;
        it.fill = fill.rectTransform;
        it.handle = handle.rectTransform;

        MainMenuUIEvents ev = AddEv(hit.gameObject);
        ev.down = e => { FocusOnly(it); it.SetFromPointer(e); };
        ev.drag = e => it.SetFromPointer(e);

        return it;
    }

    string TaglineMarkup(MenuTagline t, int size)
    {
        string s = "<i>" + t.text + "</i>";

        if (!string.IsNullOrEmpty(t.author))
            s += "\n<size=" + Mathf.Max(14, size - 8) + "><color=#" + ColorUtility.ToHtmlStringRGB(Accent) + ">\u2014 " + t.author + "</color></size>";

        return s;
    }

    void ApplyTagline(MenuTagline t)
    {
        if (taglineText == null || t == null) return;

        int size = taglineFontSize;
        float width = taglineText.rectTransform.sizeDelta.x;

        taglineText.fontSize = size;
        taglineText.text = TaglineMarkup(t, size);

        
        while (size > taglineMinFontSize && taglineText.preferredHeight > taglineMaxHeight)
        {
            size -= 2;
            taglineText.fontSize = size;
            taglineText.text = TaglineMarkup(t, size);
        }

        float h = Mathf.Max(taglineText.preferredHeight, 30f);
        taglineText.rectTransform.sizeDelta = new Vector2(width, h);
        taglineBar.rectTransform.sizeDelta = new Vector2(6f, h);
    }

    MenuTagline NextRandomTagline()
    {
        if (otherTaglines == null) return null;

        if (taglineBag.Count == 0)
        {
            foreach (MenuTagline t in otherTaglines)
                if (t != null && !string.IsNullOrEmpty(t.text)) taglineBag.Add(t);

            for (int i = taglineBag.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                MenuTagline tmp = taglineBag[i];
                taglineBag[i] = taglineBag[j];
                taglineBag[j] = tmp;
            }

            
            if (taglineBag.Count > 1 && taglineBag[taglineBag.Count - 1] == currentTagline)
            {
                MenuTagline tmp = taglineBag[0];
                taglineBag[0] = taglineBag[taglineBag.Count - 1];
                taglineBag[taglineBag.Count - 1] = tmp;
            }
        }

        if (taglineBag.Count == 0) return null;

        MenuTagline next = taglineBag[taglineBag.Count - 1];
        taglineBag.RemoveAt(taglineBag.Count - 1);
        return next;
    }

    void UpdateTagline(float dt)
    {
        if (taglineText == null || taglineInterval <= 0f || view != View.Main) return;

        switch (taglinePhase)
        {
            case TaglinePhase.Hold:
                taglineTimer += dt;
                if (taglineTimer >= taglineInterval)
                {
                    taglineTimer = 0f;
                    pendingTagline = NextRandomTagline();
                    if (pendingTagline != null) taglinePhase = TaglinePhase.FadeOut;
                }
                break;

            case TaglinePhase.FadeOut:
                taglineFade = Mathf.MoveTowards(taglineFade, 0f, dt / TaglineFadeTime);
                if (taglineFade <= 0f)
                {
                    currentTagline = pendingTagline;
                    ApplyTagline(currentTagline);
                    taglinePhase = TaglinePhase.FadeIn;
                }
                break;

            case TaglinePhase.FadeIn:
                taglineFade = Mathf.MoveTowards(taglineFade, 1f, dt / TaglineFadeTime);
                if (taglineFade >= 1f) taglinePhase = TaglinePhase.Hold;
                break;
        }

        float a = Mathf.SmoothStep(0f, 1f, taglineFade);
        taglineGroup.alpha = a;
        taglineBar.color = new Color(Accent.r, Accent.g, Accent.b, a);
    }
}