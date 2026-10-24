using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class ArtPauseEvents : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
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

[DisallowMultipleComponent]
public class ArtistPauseMenu : MonoBehaviour
{
    
    
    public static bool ResetAllProgressOnRestart = true;
    
    public static string FallbackFutureScene = "FutureScene";
    
    public static bool PauseWhenWindowLosesFocus = true;

    
    public static bool IsPaused { get; private set; }
    public static float SensitivityScale = 1f;
    public static bool InvertLookY;

    const float FovMin = 50f, FovMax = 110f;
    const string Prefs = "ArtPause_";

    static readonly string[] Tips =
    {
        "Wrong tool, no effect. Match the tool to the task.",
        "Ring the gongs in order to lure Hitler to the registration desk.",
        "Every rival splash costs Hitler composure. Keep it above zero.",
        "Press SPACE to vault over obstacles and catch rivals early.",
        "Out of ammo? A supply drop appears if you ever run dry.",
        "Press E to inspect things and pick up ammo crates.",
        "Your minimap pulses one objective at a time. Watch it for your next task.",
        "The bazooka is the only thing that can bring the Wall of Rejection down."
    };

    
    static readonly string[,] Bindings =
    {
        { "Move",                "W  A  S  D" },
        { "Look",                "MOUSE" },
        { "Use tool",            "LEFT CLICK" },
        { "Switch tool",         "1 - 4  /  SCROLL" },
        { "Vault over obstacles","SPACE" },
        { "Inspect / pick up",   "E" },
        { "Pause",               "ESC" }
    };

    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterHooks()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootCurrentScene() { TryCreate(); }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { TryCreate(); }

    static void TryCreate()
    {
        if (Object.FindObjectOfType<ArtistPauseMenu>() != null) return;
        if (Object.FindObjectOfType<ArtistLevelPlayer>() == null) return;
        new GameObject("ArtistPauseMenu").AddComponent<ArtistPauseMenu>();
    }

    
    enum View { Main, Settings, Confirm }

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
        public float height = 80f, labelY, subY;
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

    class TabButton
    {
        public Image bg, line;
        public Text label;
        public bool active, hover;
        float h, a;

        public void Tick(float dt)
        {
            h = Mathf.MoveTowards(h, hover ? 1f : 0f, dt * 8f);
            a = Mathf.MoveTowards(a, active ? 1f : 0f, dt * 8f);
            label.color = active ? Accent : Color.Lerp(new Color(1f, 1f, 1f, 0.5f), Color.white, h);
            line.color = new Color(Accent.r, Accent.g, Accent.b, a);
            bg.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.04f, 0.11f, Mathf.Max(a, h * 0.6f)));
        }
    }

    class MenuPanel
    {
        public CanvasGroup group;
        public RectTransform rt, scaleRt;
        public Vector2 basePos;
        public float slide, scaleFrom = 1f, target;
    }

    
    static readonly Color Accent = new Color(0.86f, 0.09f, 0.11f);   
    static readonly Color Red = new Color(0.86f, 0.09f, 0.11f);
    static readonly Color PanelFace = new Color(0.05f, 0.05f, 0.05f, 0.94f);
    static readonly Color AccentBorder = new Color(0.86f, 0.09f, 0.11f, 0.8f);
    static readonly Color ButtonIdle = new Color(1f, 1f, 1f, 0.06f);
    static readonly Color RowIdle = new Color(1f, 1f, 1f, 0.05f);
    static readonly Color RowHot = new Color(0.86f, 0.09f, 0.11f, 0.28f);

    static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);
    static readonly Vector2 TL = new Vector2(0f, 1f);
    static readonly Vector2 TC = new Vector2(0.5f, 1f);
    static readonly Vector2 BC = new Vector2(0.5f, 0f);
    static readonly Vector2 BL = new Vector2(0f, 0f);
    static readonly Vector2 BR = new Vector2(1f, 0f);
    static readonly Vector2 ML = new Vector2(0f, 0.5f);
    static readonly Vector2 MR = new Vector2(1f, 0.5f);

    
    bool ready, paused, dirty;
    View view = View.Main;
    float prevTimeScale = 1f;

    ArtistLevelPlayer player;
    MainCameraController camCtrl;
    bool playerWasEnabled, camInputWas;
    Coroutine restoreRoutine;

    
    static bool defaultsKnown;
    static float defFov = 60f;
    static bool defVsync = true;
    static int defQuality;

    float master = 1f, fov = 60f, sens = 1f;
    bool mute, menuSounds = true, invertY, showFps, vsync;
    int quality, displayMode, resIndex;
    readonly List<Vector2Int> resList = new List<Vector2Int>();

    FieldInfo sensField, invertField;
    float sensBase = 1f;

    
    Canvas canvas;
    GameObject root;
    CanvasGroup rootGroup;
    float rootTarget;
    Text timeText, tipText, statusText, hintText, fpsText, confirmTitle, confirmMsg;
    float statusUntil, fpsAccum;
    int fpsFrames;

    MenuPanel titlePanel, mainPanel, settingsPanel, confirmPanel;
    readonly List<MenuPanel> panels = new List<MenuPanel>();
    readonly List<Item> allItems = new List<Item>();
    readonly List<Item> mainItems = new List<Item>();
    readonly List<Item> confirmItems = new List<Item>();
    readonly List<Item> settingsList = new List<Item>();
    readonly List<TabButton> tabButtons = new List<TabButton>();
    readonly List<GameObject> tabRoots = new List<GameObject>();
    readonly List<List<Item>> tabItems = new List<List<Item>>();
    ButtonItem confirmYes, confirmNo, resetBtn, backBtn;
    System.Action confirmAction;
    int activeTab, mainFocusBeforeConfirm;

    List<Item> focusList = new List<Item>();
    int focusIndex;

    
    AudioSource uiAudio;
    AudioClip sTick, sClick, sBack, sOpen;

    
    void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;
        DisableLegacyPauseMenu();
    }

    
    void DisableLegacyPauseMenu()
    {
        PauseMenu old = Object.FindObjectOfType<PauseMenu>();
        if (old == null) return;
        old.enabled = false;
        if (old.pauseMenu != null) old.pauseMenu.SetActive(false);
    }

    void Start() { StartCoroutine(Init()); }

    IEnumerator Init()
    {
        yield return null;   

        player = Object.FindObjectOfType<ArtistLevelPlayer>();
        camCtrl = Object.FindObjectOfType<MainCameraController>();

        CaptureDefaults();
        LoadSettings();
        FindLookFields();
        BuildSounds();
        EnsureEventSystem();
        BuildUI();
        ApplyAll();
        RefreshAllItems();

        ready = true;
    }

    void OnDestroy()
    {
        if (paused)
        {
            Time.timeScale = prevTimeScale;
            AudioListener.pause = false;
            RestoreNow();
        }
        IsPaused = false;
        SaveIfDirty();
    }

    void OnApplicationQuit() { SaveIfDirty(); }

    void OnApplicationFocus(bool focus)
    {
        if (focus || !ready || paused || Application.isEditor || !PauseWhenWindowLosesFocus) return;
        if (PaintingCanvas.AnyInteracting) return;
        if (Object.FindObjectOfType<LevelIntroSequence>() != null) return;
        Pause();
    }

    void Update()
    {
        if (!ready) return;
        float udt = Time.unscaledDeltaTime;

        UpdateFps(udt);

        if (Input.GetKeyDown(KeyCode.Escape)) HandleEscape();
        if (!ready) return;   

        if (paused) HandleKeys();
        if (root.activeSelf) AnimateUI(udt);

        UpdateCursor();
    }

    void HandleEscape()
    {
        if (!paused)
        {
            
            if (PaintingCanvas.AnyInteracting || PaintingCanvas.LastExitFrame == Time.frameCount) return;
            
            if (Object.FindObjectOfType<LevelIntroSequence>() != null) return;
            Pause();
            return;
        }

        switch (view)
        {
            case View.Confirm: CloseConfirm(); break;
            case View.Settings: LeaveSettings(); break;
            default: Resume(); break;
        }
    }

    void UpdateCursor()
    {
        bool free = paused || PaintingCanvas.AnyInteracting;
        Cursor.visible = free;
        Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
    }

    
    void Pause()
    {
        if (paused || !ready) return;

        
        if (restoreRoutine != null)
        {
            StopCoroutine(restoreRoutine);
            restoreRoutine = null;
            RestoreNow();
        }

        paused = true;
        IsPaused = true;

        if (player == null) player = Object.FindObjectOfType<ArtistLevelPlayer>();
        if (camCtrl == null) camCtrl = Object.FindObjectOfType<MainCameraController>();

        
        playerWasEnabled = player != null && player.enabled;
        if (player != null) player.enabled = false;
        if (camCtrl != null)
        {
            camInputWas = camCtrl.inputEnabled;
            camCtrl.inputEnabled = false;   
        }

        prevTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        foreach (MenuPanel p in panels)
        {
            p.group.alpha = 0f;
            p.target = 0f;
            p.group.blocksRaycasts = false;
            p.group.interactable = false;
        }

        timeText.text = FormatTime(Time.timeSinceLevelLoad);
        tipText.text = "<color=#E8171B><b>TIP</b></color>    " + Tips[Random.Range(0, Tips.Length)];
        statusText.text = "";

        root.SetActive(true);
        rootTarget = 1f;
        rootGroup.blocksRaycasts = true;

        ShowMainView(0);
        Play(sOpen);
    }

    void Resume()
    {
        if (!paused) return;

        SaveIfDirty();
        paused = false;
        IsPaused = false;

        Time.timeScale = prevTimeScale;
        AudioListener.pause = false;

        rootTarget = 0f;
        rootGroup.blocksRaycasts = false;
        Play(sBack);

        restoreRoutine = StartCoroutine(RestoreControl());
    }

    
    IEnumerator RestoreControl()
    {
        yield return null;
        RestoreNow();
        restoreRoutine = null;
    }

    void RestoreNow()
    {
        if (player != null) player.enabled = playerWasEnabled;
        if (camCtrl != null) camCtrl.inputEnabled = camInputWas;
    }

    
    void AskRestart()
    {
        string msg = ResetAllProgressOnRestart
            ? "The exam starts over and your level progress is reset."
            : "The exam starts over from the beginning.";
        ShowConfirm("RESTART LEVEL?", msg, "RESTART", Red, DoRestart);
    }

    void AskReturn()
    {
        ShowConfirm("RETURN TO THE FUTURE?", "Leave Vienna, 1907 and head back to the present.", "LEAVE", Accent, DoReturnToFuture);
    }

    void AskQuit()
    {
        ShowConfirm("QUIT GAME?", "Anything unsaved will be lost.", "QUIT", Red, DoQuit);
    }

    void DoRestart()
    {
        PrepareSceneChange(false);

        if (ResetAllProgressOnRestart)
        {
            if (LevelProgressManager.Instance != null) LevelProgressManager.Instance.ResetAllProgress();
            else Debug.LogWarning("[ArtistPause] LevelProgressManager not found.");
        }

        MarkRetry();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    
    static void MarkRetry()
    {
        FieldInfo f = typeof(ArtExamDirector).GetField("retryPending", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        if (f != null && f.FieldType == typeof(bool)) f.SetValue(null, true);
    }

    void DoReturnToFuture()
    {
        string scene = FallbackFutureScene;
        Level1ObjectiveTracker tr = Object.FindObjectOfType<Level1ObjectiveTracker>();
        if (tr != null && !string.IsNullOrEmpty(tr.futureSceneName)) scene = tr.futureSceneName;

        if (string.IsNullOrEmpty(scene) || !Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("[ArtistPause] Can't find scene \"" + scene + "\". Add it to File > Build Settings > Scenes In Build.");
            CloseConfirm();
            ShowStatus("Can't find scene \"" + scene + "\" - add it to Build Settings.");
            return;
        }

        PrepareSceneChange(true);
        SceneManager.LoadScene(scene);
    }

    void DoQuit()
    {
        SaveIfDirty();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void PrepareSceneChange(bool cursorFree)
    {
        SaveIfDirty();
        ready = false;
        paused = false;
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = cursorFree;
        Cursor.lockState = cursorFree ? CursorLockMode.None : CursorLockMode.Locked;
    }

    
    void ShowMainView(int focus)
    {
        view = View.Main;
        SetPanel(titlePanel, true);
        SetPanel(mainPanel, true);
        SetPanel(settingsPanel, false);
        SetPanel(confirmPanel, false);
        SetFocusList(mainItems, focus);
        UpdateHint();
    }

    void EnterSettings()
    {
        Play(sClick);
        view = View.Settings;
        SetPanel(mainPanel, false);
        SetPanel(titlePanel, false);
        SetPanel(confirmPanel, false);
        SetPanel(settingsPanel, true);
        foreach (TabButton tb in tabButtons) tb.hover = false;
        RefreshAllItems();
        SelectTab(activeTab);
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
        SetFocusList(mainItems, 2);   
        UpdateHint();
    }

    void ShowConfirm(string title, string msg, string yes, Color yesColor, System.Action onYes)
    {
        confirmTitle.text = title;
        confirmMsg.text = msg;
        confirmYes.label.text = yes;
        confirmYes.color = yesColor;
        confirmAction = onYes;

        mainFocusBeforeConfirm = focusIndex;
        view = View.Confirm;
        SetPanel(confirmPanel, true);
        SetFocusList(confirmItems, 1);   
        UpdateHint();
    }

    void CloseConfirm()
    {
        Play(sBack);
        SetPanel(confirmPanel, false);
        view = View.Main;
        SetFocusList(mainItems, mainFocusBeforeConfirm);
        UpdateHint();
    }

    void ConfirmYes()
    {
        System.Action a = confirmAction;
        confirmAction = null;
        if (a != null) a();
    }

    void SetPanel(MenuPanel p, bool on)
    {
        p.target = on ? 1f : 0f;
        p.group.blocksRaycasts = on;
        p.group.interactable = on;
    }

    void UpdateHint()
    {
        switch (view)
        {
            case View.Settings:
                hintText.text = "W / S  select        A / D  adjust        TAB  next tab        ESC  back";
                break;
            case View.Confirm:
                hintText.text = "A / D  choose        ENTER  confirm        ESC  cancel";
                break;
            default:
                hintText.text = "W / S  navigate        ENTER  select        ESC  resume";
                break;
        }
    }

    void ShowStatus(string msg)
    {
        statusText.text = msg;
        statusUntil = Time.unscaledTime + 5f;
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
        if (focusList.Count == 0) return;

        bool up = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
        bool down = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
        bool left = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
        bool right = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);

        if (view == View.Confirm)
        {
            up = up || left;
            down = down || right;
            left = right = false;
        }

        if (view == View.Settings)
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (Input.GetKeyDown(KeyCode.Tab)) CycleTab(shift ? -1 : 1);
            if (Input.GetKeyDown(KeyCode.PageDown)) CycleTab(1);
            if (Input.GetKeyDown(KeyCode.PageUp)) CycleTab(-1);
        }

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
        Camera c = Camera.main;
        defFov = c != null ? c.fieldOfView : 60f;
        defVsync = QualitySettings.vSyncCount > 0;
        defQuality = QualitySettings.GetQualityLevel();
        defaultsKnown = true;
    }

    void LoadSettings()
    {
        master = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefs + "Master", 1f));
        mute = PlayerPrefs.GetInt(Prefs + "Mute", 0) == 1;
        menuSounds = PlayerPrefs.GetInt(Prefs + "MenuSfx", 1) == 1;
        fov = Mathf.Clamp(PlayerPrefs.GetFloat(Prefs + "Fov", defFov), FovMin, FovMax);
        sens = Mathf.Clamp(PlayerPrefs.GetFloat(Prefs + "Sens", 1f), 0.25f, 2.5f);
        invertY = PlayerPrefs.GetInt(Prefs + "InvertY", 0) == 1;
        showFps = PlayerPrefs.GetInt(Prefs + "Fps", 0) == 1;
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
        PlayerPrefs.SetFloat(Prefs + "Fov", fov);
        PlayerPrefs.SetFloat(Prefs + "Sens", sens);
        PlayerPrefs.SetInt(Prefs + "InvertY", invertY ? 1 : 0);
        PlayerPrefs.SetInt(Prefs + "Fps", showFps ? 1 : 0);
        PlayerPrefs.SetInt(Prefs + "VSync", vsync ? 1 : 0);
        PlayerPrefs.SetInt(Prefs + "Quality", quality);
        PlayerPrefs.Save();
    }

    void ResetDefaults()
    {
        master = 1f;
        mute = false;
        menuSounds = true;
        fov = Mathf.Clamp(defFov, FovMin, FovMax);
        sens = 1f;
        invertY = false;
        showFps = false;
        vsync = defVsync;
        quality = Mathf.Clamp(defQuality, 0, Mathf.Max(0, QualitySettings.names.Length - 1));

        dirty = true;
        ApplyAll();
        RefreshAllItems();
        SaveIfDirty();
        Play(sClick);
    }

    
    void FindLookFields()
    {
        if (camCtrl == null) return;

        sensField = FindField(camCtrl, typeof(float), "sensitiv", "mousespeed", "lookspeed", "rotationspeed", "rotatespeed", "turnspeed");
        invertField = FindField(camCtrl, typeof(bool), "invert");

        if (sensField != null)
        {
            sensBase = (float)sensField.GetValue(camCtrl);
            if (sensBase <= 0.0001f) sensField = null;
        }

        Debug.Log("[ArtistPause] Mouse sensitivity field: " + (sensField != null ? sensField.Name : "not found (slider hidden)")
                  + " | invert field: " + (invertField != null ? invertField.Name : "not found (toggle hidden)"));
    }

    static FieldInfo FindField(object o, System.Type type, params string[] hints)
    {
        FieldInfo[] fields = o.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (string h in hints)
            foreach (FieldInfo f in fields)
                if (f.FieldType == type && f.Name.ToLowerInvariant().Contains(h)) return f;
        return null;
    }

    void ApplyAll()
    {
        ApplyAudio();
        ApplyGraphics();
        ApplyFov();
        ApplyLook();
    }

    void ApplyAudio() { AudioListener.volume = mute ? 0f : master; }

    void ApplyGraphics()
    {
        if (QualitySettings.GetQualityLevel() != quality) QualitySettings.SetQualityLevel(quality, true);
        int want = vsync ? 1 : 0;
        if (QualitySettings.vSyncCount != want) QualitySettings.vSyncCount = want;   
    }

    void ApplyFov()
    {
        Camera c = Camera.main;
        if (c != null && Mathf.Abs(c.fieldOfView - fov) > 0.01f) c.fieldOfView = fov;
    }

    void ApplyLook()
    {
        SensitivityScale = sens;
        InvertLookY = invertY;
        if (camCtrl == null) return;
        if (sensField != null) sensField.SetValue(camCtrl, sensBase * sens);
        if (invertField != null) invertField.SetValue(camCtrl, invertY);
    }

    void BuildResolutionList()
    {
        resList.Clear();
        foreach (Resolution r in Screen.resolutions)
        {
            Vector2Int v = new Vector2Int(r.width, r.height);
            if (r.width >= 800 && !resList.Contains(v)) resList.Add(v);
        }

        Vector2Int cur = new Vector2Int(Screen.width, Screen.height);
        if (!resList.Contains(cur)) resList.Add(cur);
        resList.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

        resIndex = Mathf.Max(0, resList.IndexOf(cur));
        displayMode = Screen.fullScreenMode == FullScreenMode.Windowed ? 1 : 0;
    }

    void ApplyResolution()
    {
        Vector2Int v = resList[Mathf.Clamp(resIndex, 0, resList.Count - 1)];
        Screen.SetResolution(v.x, v.y, displayMode == 0 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
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

    
    void UpdateFps(float udt)
    {
        if (fpsText == null) return;
        fpsText.enabled = showFps;
        fpsAccum += udt;
        fpsFrames++;
        if (fpsAccum >= 0.5f)
        {
            fpsText.text = "FPS  " + Mathf.RoundToInt(fpsFrames / fpsAccum);
            fpsAccum = 0f;
            fpsFrames = 0;
        }
    }

    void AnimateUI(float dt)
    {
        rootGroup.alpha = Mathf.MoveTowards(rootGroup.alpha, rootTarget, dt * 7f);
        if (rootTarget <= 0f && rootGroup.alpha <= 0.001f)
        {
            root.SetActive(false);
            return;
        }

        foreach (MenuPanel p in panels)
        {
            float a = Mathf.MoveTowards(p.group.alpha, p.target, dt * 6f);
            p.group.alpha = a;
            float e = Mathf.SmoothStep(0f, 1f, a);
            if (p.slide != 0f) p.rt.anchoredPosition = p.basePos + new Vector2((1f - e) * p.slide, 0f);
            if (p.scaleRt != null) p.scaleRt.localScale = Vector3.one * Mathf.Lerp(p.scaleFrom, 1f, e);
        }

        foreach (Item it in allItems) it.Tick(dt);
        foreach (TabButton tb in tabButtons) tb.Tick(dt);

        float left = statusUntil - Time.unscaledTime;
        statusText.color = new Color(Red.r, Red.g, Red.b, Mathf.Clamp01(left / 0.6f));
    }

    static string FormatTime(float s)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(s));
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
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

    static void ApplySprite(Image im, Sprite sprite)
    {
        if (sprite == null) return;
        im.sprite = sprite;
        im.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
    }

    static Image Img(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
    {
        RectTransform rt = NewRect(name, parent, anchor, pivot, pos, size);
        Image im = rt.gameObject.AddComponent<Image>();
        im.color = color;
        im.raycastTarget = false;
        ApplySprite(im, sprite);
        return im;
    }

    static Image StretchImg(string name, Transform parent, Color color, Sprite sprite)
    {
        RectTransform rt = Stretch(name, parent);
        Image im = rt.gameObject.AddComponent<Image>();
        im.color = color;
        im.raycastTarget = false;
        ApplySprite(im, sprite);
        return im;
    }

    static Text Txt(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
                    string content, int fontSize, TextAnchor align, Color color, bool bold, bool wrap)
    {
        RectTransform rt = NewRect(name, parent, anchor, pivot, pos, size);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = ExamUtil.UIFont;
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
        ApplySprite(bi, PanelSprite());
        bi.color = border;
        bi.raycastTarget = true;

        RectTransform f = Stretch("Face", parent);
        f.offsetMin = new Vector2(2f, 2f);
        f.offsetMax = new Vector2(-2f, -2f);
        Image fi = f.gameObject.AddComponent<Image>();
        ApplySprite(fi, PanelSprite());
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

    static ArtPauseEvents AddEv(GameObject go)
    {
        return go.AddComponent<ArtPauseEvents>();
    }

    void Wire(Item it, RectTransform target)
    {
        ArtPauseEvents ev = AddEv(target.gameObject);
        ev.enter = () => Hover(it);
        ev.click = () => ClickItem(it);
        allItems.Add(it);
    }

    
    static Sprite vignetteSprite, leftShadeSprite;

    
    static Sprite PanelSprite() { return null; }
    static Sprite PillSprite() { return null; }
    static Sprite CircleSprite() { return null; }

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
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem (ArtistPause)");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    void BuildUI()
    {
        GameObject cg = new GameObject("ArtistPause_UI");
        cg.transform.SetParent(transform, false);
        canvas = cg.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;  

        CanvasScaler sc = cg.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;
        cg.AddComponent<GraphicRaycaster>();

        RectTransform rr = Stretch("Root", canvas.transform);
        root = rr.gameObject;
        rootGroup = root.AddComponent<CanvasGroup>();
        rootGroup.alpha = 0f;
        rootGroup.blocksRaycasts = false;

        
        Image dim = StretchImg("Dim", rr, new Color(0.02f, 0.02f, 0.02f, 0.72f), null);
        dim.raycastTarget = true;

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

        BuildConfirm(rr);

        
        fpsText = Txt("FPS", canvas.transform, TC, TC, new Vector2(0f, -12f), new Vector2(300f, 30f), "", 22,
                      TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.95f), true, false);
        AddOutline(fpsText, 1.5f);
        fpsText.enabled = showFps;

        root.SetActive(false);
    }

    
    void BuildTitle(RectTransform rr)
    {
        titlePanel = NewPanel("TitleBlock", rr, ML, ML, new Vector2(150f, 40f), new Vector2(900f, 640f), -90f, 1f);
        RectTransform b = titlePanel.rt;

        Image chip = Img("Chip", b, TL, TL, Vector2.zero, new Vector2(470f, 42f), Accent, PillSprite());
        Txt("ChipText", chip.rectTransform, Mid, Mid, Vector2.zero, new Vector2(470f, 42f), "ENTRANCE EXAM   -   VIENNA 1907", 21,
            TextAnchor.MiddleCenter, new Color(1f, 1f, 1f), true, false);

        Text big = Txt("Paused", b, TL, TL, new Vector2(-6f, -62f), new Vector2(900f, 190f), "PAUSED", 150,
                       TextAnchor.UpperLeft, Color.white, true, false);
        AddOutline(big, 3f);

        Txt("Tagline", b, TL, TL, new Vector2(0f, -312f), new Vector2(860f, 44f), "<i>Take a breath. The exam will wait.</i>", 32,
            TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.85f), false, false);

        Txt("TimeLabel", b, TL, TL, new Vector2(0f, -392f), new Vector2(300f, 26f), "TIME IN LEVEL", 20,
            TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.55f), true, false);
        timeText = Txt("Time", b, TL, TL, new Vector2(0f, -420f), new Vector2(400f, 64f), "00:00", 54,
                       TextAnchor.UpperLeft, Color.white, true, false);

        Image tip = Img("TipCard", b, TL, TL, new Vector2(0f, -510f), new Vector2(760f, 110f), new Color(1f, 1f, 1f, 0.06f), PanelSprite());
        Img("TipAccent", tip.rectTransform, ML, ML, Vector2.zero, new Vector2(6f, 70f), Accent, PillSprite());
        tipText = Txt("TipText", tip.rectTransform, ML, ML, new Vector2(30f, 0f), new Vector2(700f, 100f), "", 25,
                      TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.92f), false, true);
    }

   
    void BuildMain(RectTransform rr)
    {
        mainPanel = NewPanel("MainPanel", rr, MR, MR, new Vector2(-170f, 0f), new Vector2(640f, 720f), 80f, 1f);
        RectTransform p = mainPanel.rt;
        Frame(p, PanelFace, AccentBorder);

        Txt("Header", p, TC, TC, new Vector2(0f, -30f), new Vector2(560f, 36f), "PAUSE MENU", 22, TextAnchor.MiddleCenter, Accent, true, false);
        Img("Divider", p, TC, TC, new Vector2(0f, -72f), new Vector2(560f, 2f), new Color(1f, 1f, 1f, 0.15f), PillSprite());

        string[] names = { "RESUME", "RESTART LEVEL", "SETTINGS", "RETURN TO THE FUTURE", "QUIT GAME" };
        string[] subs = { "Back to the exam", "Start the exam over", "Audio, video and gameplay", "Leave Vienna, 1907", "Exit to desktop" };
        Color[] cols = { Accent, Accent, Accent, Accent, Accent };
        System.Action[] acts = { Resume, AskRestart, EnterSettings, AskReturn, AskQuit };

        for (int i = 0; i < names.Length; i++)
        {
            ButtonItem b = MakeButton(p, names[i], subs[i], TC, TC, new Vector2(0f, -96f - i * 108f), new Vector2(560f, 94f),
                                      cols[i], 34, acts[i], false);
            mainItems.Add(b);
        }

        statusText = Txt("Status", p, BC, BC, new Vector2(0f, 26f), new Vector2(580f, 60f), "", 20, TextAnchor.MiddleCenter, Red, false, true);
    }

    ButtonItem MakeButton(Transform parent, string label, string sub, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
                          Color color, int fontSize, System.Action action, bool center)
    {
        RectTransform r = NewRect("Btn_" + label, parent, anchor, pivot, pos, size);
        Image bg = r.gameObject.AddComponent<Image>();
        ApplySprite(bg, PanelSprite());
        bg.color = ButtonIdle;
        bg.raycastTarget = true;

        ButtonItem b = new ButtonItem { root = r, bg = bg, color = color, center = center, height = size.y, action = action };

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
        settingsPanel = NewPanel("SettingsPanel", rr, Mid, Mid, Vector2.zero, new Vector2(1100f, 860f), 0f, 0.95f);
        RectTransform p = settingsPanel.rt;
        Frame(p, PanelFace, AccentBorder);

        Txt("Header", p, TL, TL, new Vector2(70f, -30f), new Vector2(700f, 64f), "SETTINGS", 48, TextAnchor.MiddleLeft, Color.white, true, false);
        Img("HeaderLine", p, TL, TL, new Vector2(70f, -96f), new Vector2(960f, 2f), new Color(1f, 1f, 1f, 0.15f), PillSprite());

        string[] tabNames = { "GAMEPLAY", "VIDEO", "AUDIO", "CONTROLS" };

        for (int i = 0; i < tabNames.Length; i++)
        {
            int idx = i;

            RectTransform tr = NewRect("Tab" + i, p, TL, TL, new Vector2(70f + i * 243f, -112f), new Vector2(234f, 56f));
            Image tbg = tr.gameObject.AddComponent<Image>();
            ApplySprite(tbg, PanelSprite());
            tbg.color = new Color(1f, 1f, 1f, 0.04f);
            tbg.raycastTarget = true;

            TabButton tb = new TabButton { bg = tbg };
            tb.label = Txt("Label", tr, Mid, Mid, Vector2.zero, new Vector2(234f, 56f), tabNames[i], 24, TextAnchor.MiddleCenter, Color.white, true, false);
            tb.line = Img("Line", tr, BC, BC, new Vector2(0f, 7f), new Vector2(150f, 5f), Accent, PillSprite());
            tabButtons.Add(tb);

            ArtPauseEvents ev = AddEv(tr.gameObject);
            ev.enter = () => tb.hover = true;
            ev.exit = () => tb.hover = false;
            ev.click = () => { if (idx != activeTab) { Play(sClick); SelectTab(idx); } };

            RectTransform content = NewRect("TabContent" + i, p, TC, TC, new Vector2(0f, -196f), new Vector2(960f, 520f));
            tabRoots.Add(content.gameObject);
            tabItems.Add(new List<Item>());
        }

        BuildResolutionList();
        BuildGameplayTab(0);
        BuildVideoTab(1);
        BuildAudioTab(2);
        BuildControlsTab(3);

        resetBtn = MakeButton(p, "RESET TO DEFAULTS", null, BL, BL, new Vector2(70f, 40f), new Vector2(350f, 64f),
                              Accent, 24, ResetDefaults, true);
        backBtn = MakeButton(p, "BACK", null, BR, BR, new Vector2(-70f, 40f), new Vector2(240f, 64f), Accent, 26, LeaveSettings, true);

        Txt("TabHint", p, BC, BC, new Vector2(0f, 62f), new Vector2(400f, 26f), "TAB  -  next tab", 19,
            TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.4f), false, false);

        activeTab = 0;
        for (int i = 0; i < tabRoots.Count; i++)
        {
            tabRoots[i].SetActive(i == 0);
            tabButtons[i].active = i == 0;
        }
    }

    void SelectTab(int i)
    {
        activeTab = Mathf.Clamp(i, 0, tabRoots.Count - 1);
        for (int k = 0; k < tabRoots.Count; k++)
        {
            tabRoots[k].SetActive(k == activeTab);
            tabButtons[k].active = k == activeTab;
        }

        settingsList.Clear();
        settingsList.AddRange(tabItems[activeTab]);
        settingsList.Add(resetBtn);
        settingsList.Add(backBtn);
        if (view == View.Settings) SetFocusList(settingsList, 0);
    }

    void CycleTab(int dir)
    {
        int n = tabRoots.Count;
        Play(sTick);
        SelectTab((activeTab + dir + n) % n);
    }

    void BuildGameplayTab(int tab)
    {
        Transform p = tabRoots[tab].transform;
        List<Item> list = tabItems[tab];
        int r = 0;

        if (sensField != null)
            list.Add(MakeSlider(p, r++, "Mouse Sensitivity", 0.25f, 2.5f, 0.05f, 0.05f,
                                () => sens, v => { sens = v; ApplyLook(); dirty = true; }, v => v.ToString("0.00") + "x"));

        if (invertField != null)
            list.Add(MakeToggle(p, r++, "Invert Look (Y)", () => invertY, b => { invertY = b; ApplyLook(); dirty = true; }));

        list.Add(MakeSlider(p, r++, "Field of View", FovMin, FovMax, 1f, 2f,
                            () => fov, v => { fov = v; ApplyFov(); dirty = true; }, v => Mathf.RoundToInt(v).ToString()));

        list.Add(MakeToggle(p, r++, "Show FPS Counter", () => showFps, b => { showFps = b; dirty = true; }));
    }

    void BuildVideoTab(int tab)
    {
        Transform p = tabRoots[tab].transform;
        List<Item> list = tabItems[tab];
        int r = 0;

        list.Add(MakeCycler(p, r++, "Display Mode", new[] { "FULLSCREEN", "WINDOWED" },
                            () => displayMode, i => { displayMode = i; ApplyResolution(); }));

        string[] resNames = new string[resList.Count];
        for (int i = 0; i < resNames.Length; i++) resNames[i] = resList[i].x + " x " + resList[i].y;
        list.Add(MakeCycler(p, r++, "Resolution", resNames, () => resIndex, i => { resIndex = i; ApplyResolution(); }));

        list.Add(MakeToggle(p, r++, "VSync", () => vsync, b => { vsync = b; ApplyGraphics(); dirty = true; }));

        list.Add(MakeCycler(p, r++, "Graphics Quality", QualitySettings.names,
                            () => quality, i => { quality = i; ApplyGraphics(); dirty = true; }));
    }

    void BuildAudioTab(int tab)
    {
        Transform p = tabRoots[tab].transform;
        List<Item> list = tabItems[tab];
        int r = 0;

        list.Add(MakeSlider(p, r++, "Master Volume", 0f, 1f, 0.01f, 0.05f,
                            () => master, v => { master = v; ApplyAudio(); dirty = true; }, v => Mathf.RoundToInt(v * 100f) + "%"));

        list.Add(MakeToggle(p, r++, "Mute All Audio", () => mute, b => { mute = b; ApplyAudio(); dirty = true; }));

        list.Add(MakeToggle(p, r++, "Menu Sounds", () => menuSounds, b => { menuSounds = b; dirty = true; }));
    }

    void BuildControlsTab(int tab)
    {
        Transform p = tabRoots[tab].transform;
        int n = Bindings.GetLength(0);

        for (int i = 0; i < n; i++)
        {
            string action = Bindings[i, 0];
            string key = Bindings[i, 1];

            RectTransform row = NewRect("Bind" + i, p, TC, TC, new Vector2(0f, -i * 70f), new Vector2(960f, 62f));
            Image bg = row.gameObject.AddComponent<Image>();
            ApplySprite(bg, PanelSprite());
            bg.color = RowIdle;
            bg.raycastTarget = false;

            Txt("Action", row, ML, ML, new Vector2(30f, 0f), new Vector2(520f, 50f), action, 28, TextAnchor.MiddleLeft,
                new Color(1f, 1f, 1f, 0.9f), false, false);

            float w = Mathf.Max(80f, key.Length * 19f + 44f);
            Image chip = Img("Key", row, MR, MR, new Vector2(-20f, 0f), new Vector2(w, 42f), new Color(1f, 1f, 1f, 0.10f), PanelSprite());
            Txt("KeyText", chip.rectTransform, Mid, Mid, Vector2.zero, new Vector2(w, 42f), key, 24, TextAnchor.MiddleCenter, Accent, true, false);
        }
    }

    
    void InitRow(RowItem it, Transform parent, int index, string label, float labelWidth)
    {
        RectTransform r = NewRect("Row_" + label, parent, TC, TC, new Vector2(0f, -index * 80f), new Vector2(960f, 72f));
        it.root = r;

        it.bg = r.gameObject.AddComponent<Image>();
        ApplySprite(it.bg, PanelSprite());
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

        Image right = Img("ArrowR", r, MR, MR, new Vector2(-20f, 0f), new Vector2(48f, 48f), new Color(1f, 1f, 1f, 0.10f), PanelSprite());
        right.raycastTarget = true;
        Text rg = Txt("Glyph", right.rectTransform, Mid, Mid, Vector2.zero, new Vector2(48f, 48f), ">", 30, TextAnchor.MiddleCenter, Accent, true, false);
        rg.supportRichText = false;
        AddEv(right.gameObject).click = () => { FocusOnly(it); it.Adjust(1); };

        Image left = Img("ArrowL", r, MR, MR, new Vector2(-372f, 0f), new Vector2(48f, 48f), new Color(1f, 1f, 1f, 0.10f), PanelSprite());
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

        Image track = Img("Track", hit, Mid, Mid, Vector2.zero, new Vector2(it.trackWidth, 12f), new Color(1f, 1f, 1f, 0.16f), PillSprite());
        Image fill = Img("Fill", track.rectTransform, ML, ML, Vector2.zero, new Vector2(0f, 12f), Accent, PillSprite());
        Image handle = Img("Handle", track.rectTransform, ML, Mid, Vector2.zero, new Vector2(30f, 30f), Color.white, CircleSprite());

        it.track = track.rectTransform;
        it.fill = fill.rectTransform;
        it.handle = handle.rectTransform;

        ArtPauseEvents ev = AddEv(hit.gameObject);
        ev.down = e => { FocusOnly(it); it.SetFromPointer(e); };
        ev.drag = e => it.SetFromPointer(e);

        return it;
    }

    
    void BuildConfirm(RectTransform rr)
    {
        RectTransform cr = Stretch("Confirm", rr);
        CanvasGroup g = cr.gameObject.AddComponent<CanvasGroup>();
        g.alpha = 0f;
        g.blocksRaycasts = false;
        g.interactable = false;

        Image dim = cr.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.62f);
        dim.raycastTarget = true;

        RectTransform box = NewRect("Box", cr, Mid, Mid, Vector2.zero, new Vector2(780f, 380f));
        confirmPanel = new MenuPanel { group = g, rt = cr, scaleRt = box, scaleFrom = 0.92f, basePos = Vector2.zero };
        panels.Add(confirmPanel);

        Frame(box, PanelFace, AccentBorder);

        confirmTitle = Txt("Title", box, TC, TC, new Vector2(0f, -40f), new Vector2(700f, 60f), "", 44, TextAnchor.MiddleCenter, Color.white, true, false);
        confirmMsg = Txt("Message", box, TC, TC, new Vector2(0f, -120f), new Vector2(660f, 110f), "", 28, TextAnchor.UpperCenter,
                         new Color(1f, 1f, 1f, 0.85f), false, true);

        confirmYes = MakeButton(box, "YES", null, BC, BC, new Vector2(-170f, 40f), new Vector2(300f, 72f), Red, 28, ConfirmYes, true);
        confirmNo = MakeButton(box, "CANCEL", null, BC, BC, new Vector2(170f, 40f), new Vector2(300f, 72f), Color.white, 28, CloseConfirm, true);
        confirmItems.Add(confirmYes);
        confirmItems.Add(confirmNo);
    }
}