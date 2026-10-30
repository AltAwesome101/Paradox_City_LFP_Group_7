using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using GameDevExtensionMethods;
using Forestlevel;

/// <summary>Per-level text for the pause menu. Edit in the UIManager inspector.</summary>
[Serializable]
public class PauseMenuInfo
{
    public string chipText = "APPLE FOREST  -  16TH CENTURY";
    public string tagline = "Take a breath. The apples will wait.";
    public string timeCaption = "TIME IN LEVEL";
    public string resumeSub = "Back to the forest";
    public string restartSub = "Start the level over";
    public string futureSub = "Leave the 16th century";

    // PLACEHOLDER tips - replace with real Apple Forest hints.
    public string[] tips =
    {
        "Follow the apple arrow. It points to where the next apple is about to drop.",
        "When the counter appears, an apple is on its way. Get in position.",
        "Check the minimap for zones you haven't finished yet.",
        "Catch all 10 apples to complete the level."
    };
}

public class PauseMenuView : BaseUIView
{
    const string Prefs = "ArtPause_";      // same keys as FuturePauseMenu, so settings are shared
    const string Focused = "pm-focused";
    const string Hidden = "pm-hidden";
    const string AccentHex = "78C83C";     // keep in sync with --accent in PauseMenu.uss

    enum Page { Main, Settings, Confirm }

    class Item
    {
        public VisualElement Root;
        public Action Activate;
        public Action<int> Adjust;
        public Action Refresh;
    }

    // Scenes
    readonly SceneReference currentScene;
    readonly SceneReference futureScene;
    readonly PauseMenuInfo _info;

    // Elements
    readonly VisualElement _titleBlock, _mainPanel, _settingsPanel, _confirmPanel;
    readonly Label _hint, _time, _tip, _confirmTitle, _confirmMsg, _confirmYesLabel, _fps;
    readonly Button _restartbtn, _resumebtn, _futurebtn, _settingsbtn, _quitbtn, _confirmYes, _confirmNo;

    public event Action ResumeRequested;

    // Navigation
    readonly List<Item> _mainItems = new();
    readonly List<Item> _settingsItems = new();
    readonly List<Item> _confirmItems = new();
    readonly List<Item> _allItems = new();
    List<Item> _focusList = new();
    int _focusIndex;
    int _mainFocusBeforeConfirm;
    Page _page = Page.Main;
    bool _visible;
    Action _confirmAction;

    // Settings state
    float _master = 1f;
    bool _mute, _vsync, _showFps, _dirty;
    int _quality;
    static bool s_defaultsKnown;
    static bool s_defVsync = true;
    static int s_defQuality;

    float _fpsAccum;
    int _fpsFrames;

    public PauseMenuView(VisualElement container, SceneReference CurrentScene, SceneReference FutureScene,
                         PauseMenuInfo info, Label fpsLabel = null) : base(container)
    {
        currentScene = CurrentScene;
        futureScene = FutureScene;
        _info = info ?? new PauseMenuInfo();
        _fps = fpsLabel;

        _titleBlock = container.Q("pm-title-block");
        _mainPanel = container.Q("pm-main-panel");
        _settingsPanel = container.Q("pm-settings-panel");
        _confirmPanel = container.Q("pm-confirm");
        _hint = container.Q<Label>("pm-hint-label");
        _time = container.Q<Label>("pm-time-label");
        _tip = container.Q<Label>("pm-tip-label");
        _confirmTitle = container.Q<Label>("pm-confirm-title");
        _confirmMsg = container.Q<Label>("pm-confirm-msg");
        _confirmYesLabel = container.Q<Label>("confirm-yes-label");

        _resumebtn = container.Q<Button>("resume-button");
        _restartbtn = container.Q<Button>("restart-button");
        _settingsbtn = container.Q<Button>("settings-button");
        _futurebtn = container.Q<Button>("future-button");
        _quitbtn = container.Q<Button>("quit-button");
        _confirmYes = container.Q<Button>("confirm-yes");
        _confirmNo = container.Q<Button>("confirm-no");

        // Per-level text
        container.Q<Label>("pm-chip-label").text = _info.chipText;
        container.Q<Label>("pm-tagline-label").text = _info.tagline;
        container.Q<Label>("pm-time-caption").text = _info.timeCaption;
        SetSub(_resumebtn, _info.resumeSub);
        SetSub(_restartbtn, _info.restartSub);
        SetSub(_futurebtn, _info.futureSub);

        // Settings first so saved volume/quality apply as soon as the level loads
        CaptureDefaults();
        LoadSettings();
        ApplyAll();

        // Main buttons (order = keyboard order)
        AddButtonItem(_mainItems, _resumebtn, OnResumeClicked);
        AddButtonItem(_mainItems, _restartbtn, AskRestart);
        AddButtonItem(_mainItems, _settingsbtn, EnterSettings);
        AddButtonItem(_mainItems, _futurebtn, ReturnToFuture);
        AddButtonItem(_mainItems, _quitbtn, AskQuit);

        // Confirm buttons
        AddButtonItem(_confirmItems, _confirmYes, () =>
        {
            var a = _confirmAction;
            _confirmAction = null;
            a?.Invoke();
        });
        AddButtonItem(_confirmItems, _confirmNo, CloseConfirm);

        // Settings rows
        var s = container.Q("pm-settings-panel");
        _settingsItems.Add(BindSlider(s, "master", () => _master, v => { _master = v; ApplyAudio(); _dirty = true; }));
        _settingsItems.Add(BindCycler(s, "mute", new[] { "OFF", "ON" },
            () => _mute ? 1 : 0, i => { _mute = i == 1; ApplyAudio(); _dirty = true; }));
        _settingsItems.Add(BindCycler(s, "quality", QualitySettings.names,
            () => _quality, i => { _quality = i; ApplyGraphics(); _dirty = true; }));
        _settingsItems.Add(BindCycler(s, "vsync", new[] { "OFF", "ON" },
            () => _vsync ? 1 : 0, i => { _vsync = i == 1; ApplyGraphics(); _dirty = true; }));
        _settingsItems.Add(BindCycler(s, "fps", new[] { "OFF", "ON" },
            () => _showFps ? 1 : 0, i => { _showFps = i == 1; _dirty = true; }));
        AddButtonItem(_settingsItems, s.Q<Button>("reset-button"), ResetDefaults);
        AddButtonItem(_settingsItems, s.Q<Button>("back-button"), LeaveSettings);

        RefreshAll();
        SetPage(Page.Main);
        SetFocusList(_mainItems, 0);
    }

    // ------------------------------------------------------------------ show / hide

    public override void Hide()
    {
        base.Hide();
        _container.SetDisplay(false);
        if (_visible) SaveIfDirty();
        _visible = false;
        GamePause.Release(this);
        CursorUtility.LockAndHide(visible: false, lockstate: true);
    }

    public override void Show()
    {
        base.Show();
        _container.SetDisplay(true);
        GamePause.Request(this);
        CursorUtility.LockAndHide(visible: true, lockstate: false);

        if (_visible) return;           // ApplyVisibility can call Show repeatedly
        _visible = true;

        _time.text = FormatTime(Time.timeSinceLevelLoad);
        string tip = _info.tips != null && _info.tips.Length > 0
            ? _info.tips[UnityEngine.Random.Range(0, _info.tips.Length)] : "";
        _tip.text = $"<color=#{AccentHex}><b>TIP</b></color>    {tip}";

        SetPage(Page.Main);
        SetFocusList(_mainItems, 0);
    }

    /// <summary>Call from UIManager.Update (runs while timeScale = 0).</summary>
    public void Tick(float unscaledDt)
    {
        UpdateFps(unscaledDt);
        if (!_visible || _focusList.Count == 0) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        bool up = kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame;
        bool down = kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame;
        bool left = kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame;
        bool right = kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame;
        bool submit = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame
                      || kb.spaceKey.wasPressedThisFrame;

        if (_page == Page.Confirm)
        {
            up |= left;
            down |= right;
            left = right = false;
        }

        if (up) MoveFocus(-1);
        if (down) MoveFocus(1);

        var cur = _focusList[_focusIndex];
        if (left) cur.Adjust?.Invoke(-1);
        if (right) cur.Adjust?.Invoke(1);
        if (submit) cur.Activate?.Invoke();
    }

    /// <summary>Escape handler. Returns true if it closed a sub-page (so the menu stays open).</summary>
    public bool HandleBack()
    {
        switch (_page)
        {
            case Page.Confirm: CloseConfirm(); return true;
            case Page.Settings: LeaveSettings(); return true;
            default: return false;
        }
    }

    // ------------------------------------------------------------------ actions

    void RestartLevel()
    {
        Debug.Log("Restart Level");
        Hide();
        SceneManager.LoadScene(currentScene.Name);
    }

    public void ReturnToFuture()
    {
        Debug.Log("Return to the future");
        SceneManager.LoadScene(futureScene.Name);
    }

    void OnResumeClicked() => ResumeRequested?.Invoke();

    void AskRestart() =>
        ShowConfirm("RESTART LEVEL?", "Your progress in this level will be lost.", "RESTART", false, RestartLevel);

    void AskQuit() =>
        ShowConfirm("QUIT GAME?", "Anything unsaved will be lost.", "QUIT", true, DoQuit);

    void DoQuit()
    {
        SaveIfDirty();
        Debug.Log("Quitting game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------------ pages

    void SetPage(Page p)
    {
        _page = p;
        _titleBlock.EnableInClassList(Hidden, p == Page.Settings);
        _mainPanel.EnableInClassList(Hidden, p == Page.Settings);
        _settingsPanel.EnableInClassList(Hidden, p != Page.Settings);
        _confirmPanel.EnableInClassList(Hidden, p != Page.Confirm);

        _hint.text = p switch
        {
            Page.Settings => "W / S  select        A / D  adjust        ESC  back",
            Page.Confirm => "A / D  choose        ENTER  confirm        ESC  cancel",
            _ => "W / S  navigate        ENTER  select        ESC  resume"
        };
    }

    void EnterSettings()
    {
        SetPage(Page.Settings);
        RefreshAll();
        SetFocusList(_settingsItems, 0);
    }

    void LeaveSettings()
    {
        SaveIfDirty();
        SetPage(Page.Main);
        SetFocusList(_mainItems, 2);
    }

    void ShowConfirm(string title, string msg, string yes, bool danger, Action onYes)
    {
        _confirmTitle.text = title;
        _confirmMsg.text = msg;
        _confirmYesLabel.text = yes;
        _confirmYes.EnableInClassList("pm-btn--danger", danger);
        _confirmAction = onYes;

        _mainFocusBeforeConfirm = _focusIndex;
        SetPage(Page.Confirm);
        SetFocusList(_confirmItems, 1);     // default to CANCEL
    }

    void CloseConfirm()
    {
        _confirmAction = null;
        SetPage(Page.Main);
        SetFocusList(_mainItems, _mainFocusBeforeConfirm);
    }

    // ------------------------------------------------------------------ focus

    void SetFocusList(List<Item> list, int start)
    {
        _focusList = list;
        foreach (var it in _allItems) it.Root.RemoveFromClassList(Focused);
        _focusIndex = list.Count == 0 ? 0 : Mathf.Clamp(start, 0, list.Count - 1);
        if (list.Count > 0) list[_focusIndex].Root.AddToClassList(Focused);
    }

    void SetFocusIndex(int i)
    {
        if (_focusList.Count == 0) return;
        if (_focusIndex >= 0 && _focusIndex < _focusList.Count)
            _focusList[_focusIndex].Root.RemoveFromClassList(Focused);
        _focusIndex = Mathf.Clamp(i, 0, _focusList.Count - 1);
        _focusList[_focusIndex].Root.AddToClassList(Focused);
    }

    void MoveFocus(int dir)
    {
        int n = _focusList.Count;
        if (n == 0) return;
        SetFocusIndex((_focusIndex + dir + n) % n);
    }

    void FocusItem(Item it)
    {
        int i = _focusList.IndexOf(it);
        if (i >= 0 && i != _focusIndex) SetFocusIndex(i);
    }

    // ------------------------------------------------------------------ item binding

    static void SetSub(Button b, string text)
    {
        var sub = b.Q<Label>(className: "pm-btn__sub");
        if (sub != null && !string.IsNullOrEmpty(text)) sub.text = text;
    }

    void Register(List<Item> list, Item item)
    {
        item.Root.RegisterCallback<PointerEnterEvent>(_ => FocusItem(item));
        list.Add(item);
        _allItems.Add(item);
    }

    void AddButtonItem(List<Item> list, Button b, Action act)
    {
        var item = new Item { Root = b, Activate = act };
        b.clicked += () => { FocusItem(item); act(); };
        item.Root.RegisterCallback<PointerEnterEvent>(_ => FocusItem(item));
        list.Add(item);
        _allItems.Add(item);
    }

    Item BindCycler(VisualElement root, string id, string[] options, Func<int> get, Action<int> set)
    {
        var row = root.Q(id + "-row");
        var value = row.Q<Label>(id + "-value");
        var item = new Item { Root = row };

        item.Refresh = () =>
            value.text = options.Length == 0 ? "" : options[Mathf.Clamp(get(), 0, options.Length - 1)];
        item.Adjust = dir =>
        {
            int n = options.Length;
            if (n == 0) return;
            set(((get() + dir) % n + n) % n);
            item.Refresh();
        };
        item.Activate = () => item.Adjust(1);

        row.Q(id + "-prev").RegisterCallback<ClickEvent>(e => { FocusItem(item); item.Adjust(-1); e.StopPropagation(); });
        row.Q(id + "-next").RegisterCallback<ClickEvent>(e => { FocusItem(item); item.Adjust(1); e.StopPropagation(); });
        row.RegisterCallback<ClickEvent>(_ => { FocusItem(item); item.Activate(); });

        // Register() adds to _allItems; the caller adds to _settingsItems
        item.Root.RegisterCallback<PointerEnterEvent>(_ => FocusItem(item));
        _allItems.Add(item);
        return item;
    }

    Item BindSlider(VisualElement root, string id, Func<float> get, Action<float> set)
    {
        var row = root.Q(id + "-row");
        var slider = row.Q<Slider>(id + "-slider");
        var value = row.Q<Label>(id + "-value");
        var item = new Item { Root = row };
        slider.focusable = false;   // we drive focus ourselves

        item.Refresh = () =>
        {
            float v = get();
            slider.SetValueWithoutNotify(v);
            value.text = Mathf.RoundToInt(v * 100f) + "%";
        };
        item.Adjust = dir => { set(Mathf.Clamp01(get() + dir * 0.05f)); item.Refresh(); };

        slider.RegisterValueChangedCallback(e =>
        {
            FocusItem(item);
            set(Mathf.Round(e.newValue * 100f) / 100f);
            value.text = Mathf.RoundToInt(get() * 100f) + "%";
        });

        item.Root.RegisterCallback<PointerEnterEvent>(_ => FocusItem(item));
        _allItems.Add(item);
        return item;
    }

    void RefreshAll()
    {
        foreach (var it in _allItems) it.Refresh?.Invoke();
    }

    // ------------------------------------------------------------------ settings

    static void CaptureDefaults()
    {
        if (s_defaultsKnown) return;
        s_defVsync = QualitySettings.vSyncCount > 0;
        s_defQuality = QualitySettings.GetQualityLevel();
        s_defaultsKnown = true;
    }

    void LoadSettings()
    {
        _master = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefs + "Master", 1f));
        _mute = PlayerPrefs.GetInt(Prefs + "Mute", 0) == 1;
        _showFps = PlayerPrefs.GetInt(Prefs + "Fps", 0) == 1;
        _vsync = PlayerPrefs.GetInt(Prefs + "VSync", s_defVsync ? 1 : 0) == 1;
        _quality = Mathf.Clamp(PlayerPrefs.GetInt(Prefs + "Quality", s_defQuality), 0,
                               Mathf.Max(0, QualitySettings.names.Length - 1));
    }

    void SaveIfDirty()
    {
        if (!_dirty) return;
        _dirty = false;
        PlayerPrefs.SetFloat(Prefs + "Master", _master);
        PlayerPrefs.SetInt(Prefs + "Mute", _mute ? 1 : 0);
        PlayerPrefs.SetInt(Prefs + "Fps", _showFps ? 1 : 0);
        PlayerPrefs.SetInt(Prefs + "VSync", _vsync ? 1 : 0);
        PlayerPrefs.SetInt(Prefs + "Quality", _quality);
        PlayerPrefs.Save();
    }

    void ResetDefaults()
    {
        _master = 1f;
        _mute = false;
        _showFps = false;
        _vsync = s_defVsync;
        _quality = Mathf.Clamp(s_defQuality, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        _dirty = true;
        ApplyAll();
        RefreshAll();
        SaveIfDirty();
    }

    void ApplyAll() { ApplyAudio(); ApplyGraphics(); }

    void ApplyAudio() => AudioListener.volume = _mute ? 0f : _master;

    void ApplyGraphics()
    {
        if (QualitySettings.GetQualityLevel() != _quality) QualitySettings.SetQualityLevel(_quality, true);
        int want = _vsync ? 1 : 0;
        if (QualitySettings.vSyncCount != want) QualitySettings.vSyncCount = want;
    }

    void UpdateFps(float dt)
    {
        if (_fps == null) return;
        _fps.style.display = _showFps ? DisplayStyle.Flex : DisplayStyle.None;
        _fpsAccum += dt;
        _fpsFrames++;
        if (_fpsAccum >= 0.5f)
        {
            _fps.text = "FPS  " + Mathf.RoundToInt(_fpsFrames / _fpsAccum);
            _fpsAccum = 0f;
            _fpsFrames = 0;
        }
    }

    static string FormatTime(float s)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(s));
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    public void Dispose() => SaveIfDirty();
}