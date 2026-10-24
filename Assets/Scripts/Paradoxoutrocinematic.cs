using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
using Random = UnityEngine.Random;


public class ParadoxOutroCinematic : MonoBehaviour
{
    public static ParadoxOutroCinematic Instance { get; private set; }
    public static bool IsPlaying => Instance != null;

    public const string DefaultQuote = "Don't let yesterday take up too much of today.";
    public const string DefaultAuthor = "Will Rogers";

    
    private const string CreditsGroup = "GROUP 7";
    private static readonly string[] CreditsNames =
    {
        "Warren Gounden",
        "Leruo Phiri",
        "Thandolwethu Ngeno"
    };

    public static void Play(Action onComplete, AudioClip music = null, TMP_FontAsset font = null,
                            string quote = null, string author = null)
    {
        if (Instance != null) return;

        var go = new GameObject("ParadoxOutroCinematic");
        var c = go.AddComponent<ParadoxOutroCinematic>();
        c._onComplete = onComplete;
        c._music = music;
        c._font = font;
        c._quoteStr = string.IsNullOrWhiteSpace(quote) ? DefaultQuote : quote;
        c._authorStr = string.IsNullOrWhiteSpace(author) ? DefaultAuthor : author;
        c.Begin();
    }

    // ------------------------------------------------------------------
    // Types
    // ------------------------------------------------------------------
    private enum Fx { None, Shake, RedFlash, WhiteFlash, Heartbeat, Glitch }

    private struct Look
    {
        public Color top, bottom, accent;
        public float rise, sway, size, alpha, streak, pulse, glow, clock, clockSpeed;

        public static Look Lerp(Look a, Look b, float t)
        {
            return new Look
            {
                top = Color.Lerp(a.top, b.top, t),
                bottom = Color.Lerp(a.bottom, b.bottom, t),
                accent = Color.Lerp(a.accent, b.accent, t),
                rise = Mathf.Lerp(a.rise, b.rise, t),
                sway = Mathf.Lerp(a.sway, b.sway, t),
                size = Mathf.Lerp(a.size, b.size, t),
                alpha = Mathf.Lerp(a.alpha, b.alpha, t),
                streak = Mathf.Lerp(a.streak, b.streak, t),
                pulse = Mathf.Lerp(a.pulse, b.pulse, t),
                glow = Mathf.Lerp(a.glow, b.glow, t),
                clock = Mathf.Lerp(a.clock, b.clock, t),
                clockSpeed = Mathf.Lerp(a.clockSpeed, b.clockSpeed, t)
            };
        }
    }

    private class Page
    {
        public string text;
        public float size = 40f;
        public int look;
        public float black;
        public Fx fx;
        public bool fxAtEnd;
        public float extraHold;
        public float cps = 32f;
        public string cardNum, cardTitle, cardSub;

        public Page Card(string num, string title, string sub)
        {
            cardNum = num; cardTitle = title; cardSub = sub;
            return this;
        }
    }

    private class Particle
    {
        public RectTransform rt;
        public Image img;
        public Vector2 pos;
        public float vx, speedMul, phase, sizeMul, alphaMul;
    }

    private const int WARM = 0, UNRAVEL = 1, ECHO = 2, GRIEF = 3, ORCH = 4, STILL = 5, DAWN = 6;

    private const int SortingOrder = 32000; 
    private const float BarHeight = 140f;
    private const int ParticleCount = 48;

    // ------------------------------------------------------------------
    // State
    // ------------------------------------------------------------------
    private Action _onComplete;
    private AudioClip _music;
    private TMP_FontAsset _font;
    private string _quoteStr, _authorStr;
    private AudioSource _audio;
    private float _musicTarget;

    private Look[] _looks;
    private List<Page> _pages;
    private Look _cur, _target;
    private float _blackCur, _blackTarget;

    private bool _running, _skip, _advanceQueued, _allowSkip = true;
    private float _inputReadyAt, _skipHold;

    private Sprite _spWhite, _spRing, _spGlow, _spVignette, _spGradient;
    private readonly List<Texture2D> _textures = new List<Texture2D>();
    private readonly List<Sprite> _sprites = new List<Sprite>();

    private Canvas _canvas;
    private RectTransform _canvasRect, _stage, _textRoot, _barTop, _barBottom, _skipFill, _skipRoot;
    private Image _bgBase, _bgTop, _glow, _cover, _flash, _master, _cardLine;
    private CanvasGroup _textGroup, _cardGroup, _clockGroup, _quoteGroup, _authorGroup, _endGroup, _creditsGroup;
    private TextMeshProUGUI _text, _hint, _cardNum, _cardTitle, _cardSub;
    private TextMeshProUGUI _quoteTmp, _authorTmp, _endMain, _endSub, _creditsTitle, _creditsNames;
    private readonly List<Particle> _particles = new List<Particle>();
    private readonly List<Image> _glitchBars = new List<Image>();

    private RectTransform _clockRoot, _minuteHand, _hourHand;
    private readonly List<Image> _clockImgs = new List<Image>();
    private readonly List<float> _clockMix = new List<float>();
    private readonly List<float> _clockAlpha = new List<float>();
    private float _clockAngle;

    private float _shake, _beat, _beat2At = -1f, _glitchLeft;
    private bool _glitchOn;
    private float _flashA, _flashDecay = 1f;
    private Color _flashColor = Color.white;
    private float _barSlide, _barTarget;
    private float _hintA, _hintTarget;
    private float _masterA = 1f, _masterTarget = 1f, _masterSpeed = 1f;

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
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        foreach (var s in _sprites) if (s != null) Destroy(s);
        foreach (var t in _textures) if (t != null) Destroy(t);
    }

    private void Begin()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        _looks = BuildLooks();
        _pages = BuildPages();
        _cur = _target = _looks[UNRAVEL];

        BuildSprites();
        BuildUI();

        _masterA = _masterTarget = 1f;

        if (_music != null)
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.clip = _music;
            _audio.loop = true;
            _audio.playOnAwake = false;
            _audio.ignoreListenerPause = true;
            _audio.volume = 0f;
            _audio.Play();
            _musicTarget = 0.8f;
        }

        _running = true;
        StartCoroutine(Run());
    }

    private void Finish()
    {
        _running = false;
        var cb = _onComplete;
        _onComplete = null;
        Instance = null;
        Destroy(gameObject);
        if (cb != null) cb();
    }

    // ------------------------------------------------------------------
    // Input
    // ------------------------------------------------------------------
    private static bool AdvancePressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var k = Keyboard.current;
        var m = Mouse.current;
        return (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
            || (m != null && m.leftButton.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
#endif
    }

    private static bool SkipHeld()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var k = Keyboard.current;
        return k != null && k.escapeKey.isPressed;
#else
        return Input.GetKey(KeyCode.Escape);
#endif
    }

    private bool ConsumeAdvance()
    {
        if (!_advanceQueued) return false;
        _advanceQueued = false;
        return true;
    }

    // ------------------------------------------------------------------
    // The story
    // ------------------------------------------------------------------
    private static Color C(float r, float g, float b) { return new Color(r, g, b, 1f); }
    private static Color A(Color c, float a) { c.a = a; return c; }

    private static Look L(Color top, Color bottom, Color accent, float rise, float sway, float size,
                          float alpha, float streak, float pulse, float glow, float clock, float clockSpeed)
    {
        return new Look
        {
            top = top,
            bottom = bottom,
            accent = accent,
            rise = rise,
            sway = sway,
            size = size,
            alpha = alpha,
            streak = streak,
            pulse = pulse,
            glow = glow,
            clock = clock,
            clockSpeed = clockSpeed
        };
    }

    private static Look[] BuildLooks()
    {
        return new[]
        {
            
            L(C(.10f,.07f,.04f), C(.36f,.23f,.10f), C(1.0f,.84f,.55f),   14f, 18f, 14f, .35f,  0f, 0f,  .26f, 0f,   0f),   
            L(C(.12f,.01f,.05f), C(.40f,.03f,.08f), C(1.0f,.28f,.38f),  170f, 70f,  8f, .70f, 14f, 5f,  .34f, .7f, -900f),  
            L(C(.02f,.05f,.09f), C(.10f,.20f,.28f), C(.70f,.95f,1.0f),   36f, 30f, 12f, .50f,  0f, 1.6f,.30f, 0f,   0f),   
            L(C(.02f,.02f,.03f), C(.08f,.08f,.10f), C(.72f,.76f,.82f),    3f,  8f,  8f, .20f,  0f, 0f,  .08f, 0f,   0f),   
            L(C(.03f,.11f,.07f), C(.14f,.34f,.13f), C(.78f,1.0f,.50f),    6f, 42f, 30f, .85f,  0f, 2.2f,.30f, 0f,   0f),   
            L(C(.01f,.01f,.02f), C(.05f,.05f,.07f), C(.72f,.76f,.82f),    2f,  5f,  6f, .18f,  0f, 0f,  .06f, 1f,   0f),   
            L(C(.06f,.05f,.07f), C(.40f,.26f,.14f), C(1.0f,.82f,.55f),   22f, 14f, 12f, .45f,  0f, .8f, .30f, 0f,   0f),   
        };
    }

    private static Page P(int look, string text, float size = 40f, Fx fx = Fx.None, bool fxAtEnd = false,
                          float black = 0f, float extraHold = 0f)
    {
        return new Page { look = look, text = text, size = size, fx = fx, fxAtEnd = fxAtEnd, black = black, extraHold = extraHold };
    }

    private List<Page> BuildPages()
    {
        return new List<Page>
        {
            // ---------- Chapter V : the price ----------
            P(UNRAVEL, "He had done it.", 52f, Fx.Heartbeat, extraHold: 0.6f)
                .Card("CHAPTER V", "THE PRICE", "THE FINAL LOSS"),
            P(UNRAVEL, "Every moment he had touched, he had changed."),
            P(UNRAVEL, "He had rewritten history one memory at a time, certain that each change would carry him a step closer to the people he loved."),
            P(UNRAVEL, "But time is not a door that only opens one way.", 44f, Fx.Shake),
            P(UNRAVEL, "Every thread he pulled unravelled another.", 44f),
            P(UNRAVEL, "And when the last thread gave way, the city that held every era together began to fall apart.", 40f, Fx.Glitch, extraHold: 0.6f),
            P(ECHO, "The streets dissolved into light.\nThe towers turned to dust.\nTime itself began to forget.", 40f, black: 0.15f),
            P(ECHO, "And then, through the noise, he heard her.", 44f),
            P(ECHO, "Not the daughter he remembered.\nOnly what was left of her.\nA fragment. An echo of a life that no longer had a place to exist.", 38f, black: 0.2f, extraHold: 0.6f),
            P(ECHO, "She did not ask why he had come back.\nShe asked what he had done.", 44f, Fx.Heartbeat, extraHold: 1f),
            P(GRIEF, "He had no answer.", 52f, black: 0.5f, extraHold: 1.2f),

            // ---------- Chapter VI : the regret ----------
            P(GRIEF, "He had spent half a lifetime trying to save her from a single moment, and in doing so, had erased every other moment she was ever meant to have.", 38f, black: 0.45f)
                .Card("CHAPTER VI", "THE REGRET", "THE TRUE WOUND"),
            P(GRIEF, "He understood, too late, that the past had never been the wound.", 44f, black: 0.45f),
            P(GRIEF, "The wound was the regret.", 56f, Fx.Heartbeat, black: 0.5f, extraHold: 1.2f),
            P(GRIEF, "Night after night he had gone back to that orchard in his mind, and let it keep him there, while the years he still had slipped quietly away.", 38f, black: 0.45f),
            P(GRIEF, "His love had nowhere left to go, so it became regret.\nAnd regret built him a prison out of time.", 40f, black: 0.45f, extraHold: 0.8f),
            P(ORCH, "The laughter beneath the apple tree.\nHer small hand in his.\nThe promise that everything would be alright.", 40f, extraHold: 1f),
            P(ORCH, "That was all she had ever needed him to keep.", 44f, extraHold: 0.8f),
            P(GRIEF, "But it was too late.", 56f, Fx.Heartbeat, black: 0.5f, extraHold: 1.4f),
            P(STILL, "The city was gone.\nThe machine was silent.\nAnd the last thing he heard was the small voice that had once believed her father could fix anything.", 38f, black: 0.35f, extraHold: 1f),
            P(STILL, "There was no city. No machine. No time.\nOnly a man, standing in the dark, wishing he had held her hand a little longer.", 40f, black: 0.4f, extraHold: 1f),
            P(DAWN, "He had spent his whole life trying to go back.", 44f, black: 0.2f),
            P(DAWN, "And never once learned how to go on.", 52f, Fx.Heartbeat, black: 0.2f, extraHold: 1.8f),
        };
    }

    // ------------------------------------------------------------------
    // Main sequence
    // ------------------------------------------------------------------
    private IEnumerator Run()
    {
        _inputReadyAt = Time.unscaledTime + 0.6f;
        yield return null;

        _barTarget = 1f;
        yield return FadeMaster(0f, 1.8f);
        yield return Wait(0.6f);

        foreach (var p in _pages)
        {
            if (_skip) break;

            _target = _looks[p.look];
            _blackTarget = p.black;

            if (p.cardTitle != null) yield return ShowCard(p);
            if (_skip) break;

            yield return ShowPage(p);
        }

        
        _skip = false;
        _allowSkip = false;
        _skipRoot.gameObject.SetActive(false);
        _textGroup.alpha = 0f;
        _cardGroup.alpha = 0f;

        yield return ShowQuote();
        yield return ShowTheEnd();

        _musicTarget = 0f;
        yield return FadeMaster(1f, 2.2f);
        Finish();
    }

    private IEnumerator ShowCard(Page p)
    {
        _hintTarget = 0f;
        _cardNum.text = p.cardNum;
        _cardTitle.text = p.cardTitle;
        _cardSub.text = p.cardSub ?? "";
        _cardNum.color = A(_target.accent, 0.9f);
        _cardSub.color = A(Color.Lerp(Color.white, _target.accent, 0.5f), 0.8f);
        _cardLine.color = _target.accent;
        _cardGroup.alpha = 0f;

        const float total = 3.6f;
        float t = 0f;
        while (t < total && !_skip)
        {
            if (t > 0.9f && ConsumeAdvance()) break;
            t += Time.unscaledDeltaTime;

            _cardGroup.alpha = Mathf.Clamp01(t / 0.8f) * Mathf.Clamp01((total - t) / 0.7f);
            _cardTitle.characterSpacing = Mathf.Lerp(28f, 10f, Mathf.Clamp01(t / total));
            float lineW = Mathf.Lerp(0f, 260f, 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 1.1f), 3f));
            _cardLine.rectTransform.sizeDelta = new Vector2(lineW, 3f);
            yield return null;
        }

        _cardGroup.alpha = 0f;
        yield return Wait(0.35f);
    }

    private IEnumerator ShowPage(Page p)
    {
        _advanceQueued = false;
        _hintTarget = 0f;

        _text.fontSize = p.size;
        _text.text = p.text;
        _text.maxVisibleCharacters = 0;
        _textGroup.alpha = 1f;

        if (!p.fxAtEnd) TriggerFx(p.fx);
        yield return Typewriter(_text, p.cps);
        if (p.fxAtEnd) TriggerFx(p.fx);

        float hold = Mathf.Clamp(0.8f + p.text.Length * 0.014f, 1.4f, 4.6f) + p.extraHold;
        _hintTarget = 1f;
        _advanceQueued = false;
        yield return WaitOrAdvance(hold);
        _hintTarget = 0f;

        float t = 0f;
        while (t < 0.5f && !_skip)
        {
            t += Time.unscaledDeltaTime;
            _textGroup.alpha = 1f - Mathf.Clamp01(t / 0.5f);
            yield return null;
        }
        _textGroup.alpha = 0f;
        yield return Wait(0.25f);
    }

    private IEnumerator ShowQuote()
    {
        _advanceQueued = false;
        _target = _looks[DAWN];
        _blackTarget = 0.35f;
        _hintTarget = 0f;

        _quoteTmp.text = _quoteStr;
        _quoteTmp.maxVisibleCharacters = 0;
        _authorTmp.text = "\u2014 " + _authorStr;
        _quoteGroup.alpha = 1f;
        _authorGroup.alpha = 0f;

        yield return Wait(1.2f);
        yield return Typewriter(_quoteTmp, 20f);

        float t = 0f;
        while (t < 1.2f)
        {
            t += Time.unscaledDeltaTime;
            _authorGroup.alpha = Mathf.Clamp01(t / 1.2f);
            yield return null;
        }
        _authorGroup.alpha = 1f;

        _hintTarget = 1f;
        _advanceQueued = false;
        yield return WaitOrAdvance(6.5f);
        _hintTarget = 0f;

        t = 0f;
        while (t < 1.2f)
        {
            t += Time.unscaledDeltaTime;
            float a = 1f - Mathf.Clamp01(t / 1.2f);
            _quoteGroup.alpha = a;
            _authorGroup.alpha = a;
            yield return null;
        }
        _quoteGroup.alpha = 0f;
        _authorGroup.alpha = 0f;
        yield return Wait(0.6f);
    }

    private IEnumerator ShowTheEnd()
    {
        _advanceQueued = false;
        _target = _looks[STILL];
        _blackTarget = 0.3f;
        _endGroup.alpha = 0f;
        _creditsGroup.alpha = 0f;

        float t = 0f;
        while (t < 2f)
        {
            t += Time.unscaledDeltaTime;
            _endGroup.alpha = Mathf.Clamp01(t / 2f);
            _endMain.characterSpacing = Mathf.Lerp(50f, 30f, Mathf.Clamp01(t / 2f));
            yield return null;
        }
        _endGroup.alpha = 1f;

        
        yield return Wait(1.2f);
        t = 0f;
        while (t < 1.8f)
        {
            t += Time.unscaledDeltaTime;
            _creditsGroup.alpha = Mathf.Clamp01(t / 1.8f);
            yield return null;
        }
        _creditsGroup.alpha = 1f;

        _hintTarget = 0f;
        _advanceQueued = false;
        yield return WaitOrAdvance(8f);
    }

    private IEnumerator Typewriter(TextMeshProUGUI t, float cps)
    {
        t.maxVisibleCharacters = 0;
        t.ForceMeshUpdate();
        int total = t.textInfo.characterCount;

        float perChar = 1f / Mathf.Max(1f, cps);
        float nextDelay = 0f, acc = 0f;
        int shown = 0;

        while (shown < total)
        {
            if (_skip || ConsumeAdvance()) break;

            acc += Time.unscaledDeltaTime;
            while (shown < total && acc >= nextDelay)
            {
                acc -= nextDelay;
                shown++;
                t.maxVisibleCharacters = shown;
                char c = t.textInfo.characterInfo[shown - 1].character;
                nextDelay = perChar + PauseFor(c);
            }
            yield return null;
        }

        t.maxVisibleCharacters = 99999;
    }

    private static float PauseFor(char c)
    {
        switch (c)
        {
            case '.': case '!': case '?': return 0.42f;
            case ',': case ';': return 0.16f;
            case ':': return 0.2f;
            case '\n': return 0.3f;
            default: return 0f;
        }
    }

    private IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds && !_skip)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitOrAdvance(float seconds)
    {
        float t = 0f;
        while (t < seconds && !_skip)
        {
            if (ConsumeAdvance()) yield break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator FadeMaster(float target, float duration)
    {
        _masterTarget = target;
        _masterSpeed = 1f / Mathf.Max(0.01f, duration);
        while (!Mathf.Approximately(_masterA, _masterTarget)) yield return null;
    }

    private void TriggerFx(Fx fx)
    {
        switch (fx)
        {
            case Fx.Shake:
                _shake = 1f;
                break;
            case Fx.RedFlash:
                _flashColor = new Color(0.9f, 0.05f, 0.08f);
                _flashA = 0.55f; _flashDecay = 1.2f; _shake = 0.6f;
                break;
            case Fx.WhiteFlash:
                _flashColor = Color.white;
                _flashA = 1f; _flashDecay = 0.5f; _shake = 1.2f;
                break;
            case Fx.Heartbeat:
                _beat = 1f;
                _beat2At = Time.unscaledTime + 0.28f;
                break;
            case Fx.Glitch:
                _glitchLeft = 0.9f; _shake = 0.5f;
                break;
        }
    }

    // ------------------------------------------------------------------
    // Per-frame visuals
    // ------------------------------------------------------------------
    private void Update()
    {
        if (!_running) return;

        float dt = Time.unscaledDeltaTime;
        float now = Time.unscaledTime;

        if (now >= _inputReadyAt && AdvancePressed()) _advanceQueued = true;
        _skipHold = Mathf.Clamp01(_skipHold + (SkipHeld() ? dt : -dt * 2f));
        if (_allowSkip && _skipHold >= 1f) _skip = true;
        _skipFill.sizeDelta = new Vector2(220f * _skipHold, 3f);

        _cur = Look.Lerp(_cur, _target, 1f - Mathf.Exp(-dt * 1.5f));
        _blackCur = Mathf.MoveTowards(_blackCur, _blackTarget, dt * 0.8f);

        _bgBase.color = _cur.bottom;
        _bgTop.color = _cur.top;
        _cover.color = new Color(0f, 0f, 0f, _blackCur);

        if (_beat2At > 0f && now >= _beat2At) { _beat = 0.8f; _beat2At = -1f; }
        _beat *= Mathf.Exp(-dt * 7f);
        _shake *= Mathf.Exp(-dt * 4f);
        if (_shake < 0.01f) _shake = 0f;

        Vector2 off = _shake > 0f ? Random.insideUnitCircle * (_shake * 22f) : Vector2.zero;
        _stage.anchoredPosition = off;
        _textRoot.anchoredPosition = off * 0.35f;
        _stage.localScale = Vector3.one * (1f + 0.025f * _beat);

        _glow.color = A(_cur.accent, _cur.glow * (0.85f + 0.15f * Mathf.Sin(now * 1.2f) + 0.4f * _beat));

        _flashA = Mathf.MoveTowards(_flashA, 0f, dt * _flashDecay);
        _flash.color = A(_flashColor, _flashA);

        _barSlide = Mathf.MoveTowards(_barSlide, _barTarget, dt * 0.7f);
        float s = Mathf.SmoothStep(0f, 1f, _barSlide);
        _barTop.anchoredPosition = new Vector2(0f, BarHeight * (1f - s));
        _barBottom.anchoredPosition = new Vector2(0f, -BarHeight * (1f - s));

        _text.color = Color.Lerp(Color.white, _cur.accent, 0.15f);
        _quoteTmp.color = Color.Lerp(Color.white, _cur.accent, 0.12f);
        _authorTmp.color = A(Color.Lerp(Color.white, _cur.accent, 0.5f), 0.85f);

        _hintA = Mathf.MoveTowards(_hintA, _hintTarget, dt * 2f);
        _hint.alpha = _hintA * (0.35f + 0.25f * Mathf.Sin(now * 3f));

        _masterA = Mathf.MoveTowards(_masterA, _masterTarget, dt * _masterSpeed);
        _master.color = new Color(0f, 0f, 0f, _masterA);
        if (_audio != null) _audio.volume = Mathf.MoveTowards(_audio.volume, _musicTarget, dt * 0.8f);

        UpdateParticles(now, dt);
        UpdateClock(dt);
        UpdateGlitch(dt);
    }

    private void UpdateParticles(float now, float dt)
    {
        float halfW = Mathf.Max(960f, _canvasRect.rect.width * 0.5f) + 80f;
        float halfH = Mathf.Max(540f, _canvasRect.rect.height * 0.5f) + 80f;

        foreach (var p in _particles)
        {
            p.pos.y += _cur.rise * p.speedMul * dt;
            p.pos.x += (p.vx + Mathf.Sin(now * 0.8f * p.speedMul + p.phase) * _cur.sway) * dt;

            if (p.pos.y > halfH) { p.pos.y = -halfH; p.pos.x = Random.Range(-halfW, halfW); }
            else if (p.pos.y < -halfH) { p.pos.y = halfH; p.pos.x = Random.Range(-halfW, halfW); }
            if (p.pos.x > halfW) p.pos.x = -halfW;
            else if (p.pos.x < -halfW) p.pos.x = halfW;

            float sz = _cur.size * p.sizeMul;
            p.rt.anchoredPosition = p.pos;
            p.rt.sizeDelta = new Vector2(sz, sz * (1f + _cur.streak));

            float flicker = Mathf.Lerp(1f,
                0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(now * _cur.pulse + p.phase)),
                Mathf.Clamp01(_cur.pulse));
            p.img.color = A(_cur.accent, _cur.alpha * p.alphaMul * flicker);
        }
    }

    private void UpdateClock(float dt)
    {
        if (_cur.clock < 0.01f)
        {
            _clockGroup.alpha = 0f;
            return;
        }

        
        _clockAngle += _cur.clockSpeed * dt;
        _minuteHand.localRotation = Quaternion.Euler(0f, 0f, _clockAngle);
        _hourHand.localRotation = Quaternion.Euler(0f, 0f, _clockAngle / 12f);

        _clockGroup.alpha = _cur.clock * 0.4f;
        for (int i = 0; i < _clockImgs.Count; i++)
            _clockImgs[i].color = A(Color.Lerp(Color.white, _cur.accent, _clockMix[i]), _clockAlpha[i]);
    }

    private void UpdateGlitch(float dt)
    {
        if (_glitchLeft > 0f)
        {
            _glitchLeft -= dt;
            _glitchOn = true;
            foreach (var b in _glitchBars)
            {
                bool on = Random.value < 0.6f;
                b.color = A(Random.value < 0.5f ? _cur.accent : Color.white, on ? Random.Range(0.08f, 0.35f) : 0f);
                b.rectTransform.anchoredPosition = new Vector2(Random.Range(-60f, 60f), Random.Range(-500f, 500f));
                b.rectTransform.sizeDelta = new Vector2(3200f, Random.Range(4f, 60f));
            }
        }
        else if (_glitchOn)
        {
            _glitchOn = false;
            foreach (var b in _glitchBars) b.color = A(Color.white, 0f);
        }
    }

    // ------------------------------------------------------------------
    // Generated art
    // ------------------------------------------------------------------
    private void BuildSprites()
    {
        _spWhite = MakeSprite(MakeTexture(4, (x, y, n) => Color.white));

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
            return new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - d), 2f));
        }));

        _spVignette = MakeSprite(MakeTexture(128, (x, y, n) =>
        {
            float nx = (x / (n - 1f) - 0.5f) * 2f;
            float ny = (y / (n - 1f) - 0.5f) * 2f;
            float d = Mathf.Sqrt(nx * nx + ny * ny) / 1.414f;
            return new Color(0f, 0f, 0f, Mathf.Pow(d, 2.2f) * 0.95f);
        }));

        var g = new Texture2D(2, 256, TextureFormat.RGBA32, false);
        g.wrapMode = TextureWrapMode.Clamp;
        g.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < 256; y++)
        {
            var c = new Color(1f, 1f, 1f, y / 255f);
            g.SetPixel(0, y, c);
            g.SetPixel(1, y, c);
        }
        g.Apply();
        _textures.Add(g);
        _spGradient = Sprite.Create(g, new Rect(0, 0, 2, 256), new Vector2(0.5f, 0.5f), 100f);
        _sprites.Add(_spGradient);
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
        _textures.Add(tex);
        return tex;
    }

    private Sprite MakeSprite(Texture2D tex)
    {
        var s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        _sprites.Add(s);
        return s;
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
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt, float pad = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-pad, -pad);
        rt.offsetMax = new Vector2(pad, pad);
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
        if (_font != null) t.font = _font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = align;
        t.characterSpacing = spacing;
        t.raycastTarget = false;
        return t;
    }

    private static CanvasGroup NewGroup(RectTransform rt)
    {
        var g = rt.gameObject.AddComponent<CanvasGroup>();
        g.alpha = 0f;
        g.interactable = false;
        g.blocksRaycasts = false;
        return g;
    }

    // ------------------------------------------------------------------
    // Build the screen
    // ------------------------------------------------------------------
    private void BuildUI()
    {
        var canvasGo = new GameObject("OutroCanvas");
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = SortingOrder;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        _canvasRect = canvasGo.GetComponent<RectTransform>();
        Transform root = canvasGo.transform;

        
        _stage = NewRect("Stage", root);
        Stretch(_stage);

        _bgBase = NewImage("Base", _stage, _spWhite, Color.black, true);
        Stretch(_bgBase.rectTransform, 90f);
        _bgTop = NewImage("Top", _stage, _spGradient, Color.black);
        Stretch(_bgTop.rectTransform, 90f);

        _glow = NewImage("Glow", _stage, _spGlow, A(Color.white, 0.2f));
        SetRect(_glow.rectTransform, new Vector2(0f, 40f), new Vector2(1300f, 1300f));

        var pl = NewRect("Particles", _stage);
        Stretch(pl);
        BuildParticles(pl);

        BuildClock(_stage);

        for (int i = 0; i < 6; i++)
        {
            var b = NewImage("Glitch" + i, _stage, _spWhite, A(Color.white, 0f));
            SetRect(b.rectTransform, Vector2.zero, new Vector2(3200f, 10f));
            _glitchBars.Add(b);
        }

        
        _cover = NewImage("Cover", root, _spWhite, A(Color.black, 0f));
        Stretch(_cover.rectTransform);

        var vig = NewImage("Vignette", root, _spVignette, Color.white);
        Stretch(vig.rectTransform);

        _flash = NewImage("Flash", root, _spWhite, A(Color.white, 0f));
        Stretch(_flash.rectTransform);

        
        _textRoot = NewRect("TextRoot", root);
        Stretch(_textRoot);
        _textGroup = NewGroup(_textRoot);
        _text = NewText("Story", _textRoot, "", 40f, Color.white, new Vector2(0f, 10f), new Vector2(1500f, 640f));
        _text.lineSpacing = 22f;

        
        var cardRoot = NewRect("Card", root);
        Stretch(cardRoot);
        _cardGroup = NewGroup(cardRoot);
        _cardNum = NewText("CardNum", cardRoot, "", 26f, Color.white, new Vector2(0f, 120f), new Vector2(900f, 40f),
                           FontStyles.Normal, TextAlignmentOptions.Center, 16f);
        _cardTitle = NewText("CardTitle", cardRoot, "", 84f, Color.white, new Vector2(0f, 35f), new Vector2(1700f, 120f),
                             FontStyles.Bold, TextAlignmentOptions.Center, 20f);
        _cardLine = NewImage("CardLine", cardRoot, _spWhite, Color.white);
        SetRect(_cardLine.rectTransform, new Vector2(0f, -40f), new Vector2(0f, 3f));
        _cardSub = NewText("CardSub", cardRoot, "", 26f, Color.white, new Vector2(0f, -85f), new Vector2(1200f, 40f),
                           FontStyles.Normal, TextAlignmentOptions.Center, 12f);

       
        var quoteRoot = NewRect("Quote", root);
        Stretch(quoteRoot);
        _quoteGroup = NewGroup(quoteRoot);
        _quoteTmp = NewText("QuoteText", quoteRoot, "", 56f, new Color(1f, 1f, 1f, 0.95f), new Vector2(0f, 40f),
                            new Vector2(1500f, 320f), FontStyles.Italic, TextAlignmentOptions.Center, 2f);
        _quoteTmp.lineSpacing = 14f;

        var authorRoot = NewRect("Author", root);
        Stretch(authorRoot);
        _authorGroup = NewGroup(authorRoot);
        _authorTmp = NewText("AuthorText", authorRoot, "", 30f, Color.white, new Vector2(0f, -150f),
                             new Vector2(900f, 50f), FontStyles.Normal, TextAlignmentOptions.Center, 10f);

        
        var endRoot = NewRect("TheEnd", root);
        Stretch(endRoot);
        _endGroup = NewGroup(endRoot);
        _endMain = NewText("EndMain", endRoot, "THE END", 110f, Color.white, new Vector2(0f, 160f),
                           new Vector2(1500f, 180f), FontStyles.Bold, TextAlignmentOptions.Center, 30f);
        _endSub = NewText("EndSub", endRoot, "THANK YOU FOR PLAYING", 26f, new Color(1f, 1f, 1f, 0.6f), new Vector2(0f, 70f),
                          new Vector2(1200f, 40f), FontStyles.Normal, TextAlignmentOptions.Center, 14f);

        
        var creditsRoot = NewRect("Credits", root);
        Stretch(creditsRoot);
        _creditsGroup = NewGroup(creditsRoot);
        _creditsTitle = NewText("CreditsTitle", creditsRoot, CreditsGroup, 34f, new Color(1f, 0.82f, 0.55f, 0.95f),
                                new Vector2(0f, -45f), new Vector2(1000f, 50f), FontStyles.Bold, TextAlignmentOptions.Center, 18f);
        _creditsNames = NewText("CreditsNames", creditsRoot, string.Join("\n", CreditsNames), 30f, new Color(1f, 1f, 1f, 0.9f),
                                new Vector2(0f, -155f), new Vector2(1000f, 190f), FontStyles.Normal, TextAlignmentOptions.Center, 8f);
        _creditsNames.lineSpacing = 18f;

        
        var top = NewImage("BarTop", root, _spWhite, Color.black).rectTransform;
        top.anchorMin = new Vector2(0f, 1f); top.anchorMax = new Vector2(1f, 1f);
        top.pivot = new Vector2(0.5f, 1f); top.sizeDelta = new Vector2(0f, BarHeight);
        top.anchoredPosition = new Vector2(0f, BarHeight);
        _barTop = top;

        var bottom = NewImage("BarBottom", root, _spWhite, Color.black).rectTransform;
        bottom.anchorMin = new Vector2(0f, 0f); bottom.anchorMax = new Vector2(1f, 0f);
        bottom.pivot = new Vector2(0.5f, 0f); bottom.sizeDelta = new Vector2(0f, BarHeight);
        bottom.anchoredPosition = new Vector2(0f, -BarHeight);
        _barBottom = bottom;

        
        _hint = NewText("Hint", root, "SPACE / CLICK TO CONTINUE", 18f, Color.white, Vector2.zero, new Vector2(700f, 30f),
                        FontStyles.Normal, TextAlignmentOptions.Center, 8f);
        Anchor(_hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(700f, 30f));
        _hint.alpha = 0f;

        _skipRoot = NewRect("Skip", root);
        Stretch(_skipRoot);

        var skipLabel = NewText("SkipLabel", _skipRoot, "HOLD ESC TO SKIP", 16f, new Color(1f, 1f, 1f, 0.4f), Vector2.zero,
                                new Vector2(400f, 26f), FontStyles.Normal, TextAlignmentOptions.Right, 6f);
        Anchor(skipLabel.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60f, 46f), new Vector2(400f, 26f));

        var skipBg = NewImage("SkipBg", _skipRoot, _spWhite, new Color(1f, 1f, 1f, 0.15f));
        Anchor(skipBg.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60f, 36f), new Vector2(220f, 3f));
        var skipFill = NewImage("SkipFill", _skipRoot, _spWhite, new Color(1f, 1f, 1f, 0.85f));
        Anchor(skipFill.rectTransform, new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(-280f, 36f), new Vector2(0f, 3f));
        _skipFill = skipFill.rectTransform;

        
        _master = NewImage("Master", root, _spWhite, Color.black);
        Stretch(_master.rectTransform);
    }

    private void BuildParticles(RectTransform parent)
    {
        _particles.Clear();
        for (int i = 0; i < ParticleCount; i++)
        {
            var p = new Particle
            {
                pos = new Vector2(Random.Range(-1000f, 1000f), Random.Range(-600f, 600f)),
                phase = Random.Range(0f, 6.28f),
                speedMul = Random.Range(0.6f, 1.4f),
                sizeMul = Random.Range(0.6f, 1.5f),
                alphaMul = Random.Range(0.5f, 1f),
                vx = Random.Range(-12f, 12f)
            };

            var img = NewImage("P" + i, parent, _spGlow, Color.white);
            SetRect(img.rectTransform, p.pos, Vector2.one * 10f);
            p.rt = img.rectTransform;
            p.img = img;
            _particles.Add(p);
        }
    }

    private Image ClockImg(string name, Transform parent, Sprite sprite, float mix, float alpha)
    {
        var img = NewImage(name, parent, sprite, Color.white);
        _clockImgs.Add(img);
        _clockMix.Add(mix);
        _clockAlpha.Add(alpha);
        return img;
    }

    private void BuildClock(Transform parent)
    {
        const float R = 280f;
        _clockRoot = NewRect("Clock", parent);
        SetRect(_clockRoot, new Vector2(0f, 20f), new Vector2(R * 2f, R * 2f));
        _clockGroup = NewGroup(_clockRoot);

        SetRect(ClockImg("Ring", _clockRoot, _spRing, 1f, 1f).rectTransform, Vector2.zero, new Vector2(R * 2f, R * 2f));
        SetRect(ClockImg("InnerRing", _clockRoot, _spRing, 0.6f, 0.5f).rectTransform, Vector2.zero, new Vector2(R * 1.55f, R * 1.55f));

        for (int i = 0; i < 12; i++)
        {
            bool major = i % 3 == 0;
            var holder = NewRect("Tick" + i, _clockRoot);
            SetRect(holder, Vector2.zero, new Vector2(R * 2f, R * 2f));
            holder.localRotation = Quaternion.Euler(0f, 0f, -i * 30f);

            var tick = ClockImg("Mark", holder, _spWhite, 1f, major ? 1f : 0.65f);
            SetRect(tick.rectTransform, new Vector2(0f, R - (major ? 24f : 18f)),
                    new Vector2(major ? 6f : 3f, major ? 26f : 14f));
        }

        var hour = ClockImg("HourHand", _clockRoot, _spWhite, 0.4f, 1f);
        _hourHand = hour.rectTransform;
        SetRect(_hourHand, Vector2.zero, new Vector2(9f, R * 0.45f));
        _hourHand.pivot = new Vector2(0.5f, 0f);

        var minute = ClockImg("MinuteHand", _clockRoot, _spWhite, 0f, 1f);
        _minuteHand = minute.rectTransform;
        SetRect(_minuteHand, Vector2.zero, new Vector2(5f, R * 0.68f));
        _minuteHand.pivot = new Vector2(0.5f, 0f);

        SetRect(ClockImg("Hub", _clockRoot, _spGlow, 1f, 1f).rectTransform, Vector2.zero, new Vector2(40f, 40f));
    }
}