using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[DisallowMultipleComponent]
public class ParadoxEndingDirector : MonoBehaviour
{
    // ------------------------------------------------------------------
    // DEBUG
    // ------------------------------------------------------------------
    [Header("DEBUG (Play Mode only)")]
    [Tooltip("Read-only display of the countdown. -1 = not running.")]
    [SerializeField] private float debugSecondsRemaining = -1f;
    [Tooltip("Tick to play the whole ending right now. Unticks itself.")]
    [SerializeField] private bool debugStartEndingNow;
    [Tooltip("Tick to jump the countdown to 15 seconds left so you can watch the warning phase. Unticks itself.")]
    [SerializeField] private bool debugJumpToLastSeconds;
    [Tooltip("Tick to restart the countdown from the full time. Unticks itself.")]
    [SerializeField] private bool debugRestartTimer;
    [Tooltip("Tick to run the countdown even if the 3 levels are not all complete.")]
    [SerializeField] private bool debugIgnoreLevelRequirement;

    // ------------------------------------------------------------------
    // Trigger
    // ------------------------------------------------------------------
    [Header("Trigger")]
    [Tooltip("Seconds between the player returning with all 3 levels complete and the ending starting. Only counts down in this scene.")]
    [SerializeField] private float endingDelaySeconds = 300f;
    [Tooltip("In the last N seconds the world starts to feel wrong (vignette, heartbeat, rumble, sky).")]
    [SerializeField] private float warningSeconds = 45f;

    // ------------------------------------------------------------------
    // Scene
    // ------------------------------------------------------------------
    [Header("Scene")]
    [Tooltip("The city prefab instance(s) in the scene (drag from the Hierarchy). If empty, every root object in the scene " +
             "except the player, camera, UI and this object is used.")]
    [SerializeField] private List<Transform> cityRoots = new List<Transform>();
    [Tooltip("The player. If empty, the object tagged 'Player' is used.")]
    [SerializeField] private Transform player;
    [Tooltip("The player's camera. If empty, Camera.main is used.")]
    [SerializeField] private Camera viewCamera;
    [Tooltip("Locks the player's movement and controls once his daughter appears.")]
    [SerializeField] private bool freezePlayer = true;

    [Header("Orbit camera (a new camera is created in code once he is frozen)")]
    [Tooltip("Distance from the player.")]
    [SerializeField] private float orbitRadius = 7f;
    [Tooltip("Camera height above the player's position.")]
    [SerializeField] private float orbitHeight = 2.2f;
    [Tooltip("Height above the player's position that the camera looks at (about chest height).")]
    [SerializeField] private float orbitFocusHeight = 1.2f;
    [Tooltip("Degrees per second. Negative = orbit the other way.")]
    [SerializeField] private float orbitSpeed = 8f;
    [Tooltip("Seconds taken to glide from his eyes out onto the orbit.")]
    [SerializeField] private float orbitBlendTime = 2.5f;
    [Tooltip("Your own scripts on the player/camera are disabled automatically. List any extra scripts to disable here " +
             "(for example scripts from a package).")]
    [SerializeField] private List<MonoBehaviour> extraScriptsToDisable = new List<MonoBehaviour>();
    [Tooltip("City pieces within this distance of the player never break, so he is left standing on a small island in the void.")]
    [SerializeField] private float safeRadius = 7f;

    // ------------------------------------------------------------------
    // Daughter
    // ------------------------------------------------------------------
    [Header("Fragmented daughter (built from glowing shards)")]
    [SerializeField] private Color daughterColor = new Color(0.72f, 0.92f, 1f, 1f);
    [Tooltip("Her height in world units.")]
    [SerializeField] private float daughterHeight = 1.15f;
    [Tooltip("How many shards make her body. More = more detailed, slightly more expensive.")]
    [SerializeField] private int shardCount = 210;
    [Tooltip("How far in front of the player she appears.")]
    [SerializeField] private float daughterSpawnDistance = 34f;
    [Tooltip("How close she stops to him.")]
    [SerializeField] private float daughterStopDistance = 3.2f;
    [SerializeField] private float daughterRunSpeed = 8f;
    [TextArea]
    [SerializeField] private string daughterLine = "Daddy... what did you do...";
    [TextArea]
    [SerializeField] private string daughterWhisper = "You promised me everything would be alright.";

    // ------------------------------------------------------------------
    // City breakup
    // ------------------------------------------------------------------
    [Header("City breakup")]
    [Tooltip("Seconds for the shockwave to travel from the player to the edge of the city.")]
    [SerializeField] private float breakupDuration = 14f;
    [Tooltip("How fast broken pieces are pulled into the sky (units per second).")]
    [SerializeField] private float ascendSpeed = 22f;
    [Tooltip("Spin of broken pieces (degrees per second).")]
    [SerializeField] private float tumbleSpeed = 120f;

    [Header("Atmosphere at the end of the world")]
    [SerializeField] private Color apocalypseFogColor = new Color(0.30f, 0.02f, 0.06f, 1f);
    [SerializeField] private float apocalypseFogDensity = 0.006f;
    [SerializeField] private Color apocalypseSunColor = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private float apocalypseSkyExposure = 0.12f;
    [SerializeField] private Color vignetteColor = new Color(0.55f, 0.02f, 0.06f, 1f);

    // ------------------------------------------------------------------
    // Audio / text / after
    // ------------------------------------------------------------------
    [Header("Daughter's voice (your own audio)")]
    [Tooltip("Plays the moment she screams \"Daddy... what did you do...\". Nothing plays if left empty.")]
    [SerializeField] private AudioClip daughterScreamClip;
    [Range(0f, 1f)]
    [SerializeField] private float daughterScreamVolume = 1f;
    [Tooltip("Optional. Plays as she whispers the promise.")]
    [SerializeField] private AudioClip daughterWhisperClip;
    [Range(0f, 1f)]
    [SerializeField] private float daughterWhisperVolume = 1f;

    [Header("Other audio (optional)")]
    [Tooltip("Optional. A low rumble loop is synthesised in code if left empty.")]
    [SerializeField] private AudioClip rumbleLoopOverride;
    [Tooltip("Music for the closing cutscene.")]
    [SerializeField] private AudioClip outroMusic;

    [Header("Text")]
    [Tooltip("Optional. Use the same font you gave the intro cinematic.")]
    [SerializeField] private TMP_FontAsset font;
    [TextArea]
    [SerializeField] private string closingQuote = "Don't let yesterday take up too much of today.";
    [SerializeField] private string closingQuoteAuthor = "Will Rogers";

    [Header("After the ending")]
    [Tooltip("Scene to load when the credits finish. Must be in Build Settings. Leave empty to quit the game instead.")]
    [SerializeField] private string mainMenuSceneName = "";
    [Tooltip("Used only when no menu scene is set. (In the Editor this stops Play Mode.)")]
    [SerializeField] private bool quitIfNoMenuScene = true;
    [SerializeField] private UnityEvent onEndingStarted;
    [SerializeField] private UnityEvent onEndingComplete;

    // ------------------------------------------------------------------
    // Static state (survives scene changes, reset on every Play)
    // ------------------------------------------------------------------
    private static bool s_timerRunning;
    private static bool s_endingStarted;
    private static float s_remaining;

    public static bool EndingActive { get; private set; }
    public static bool EndingStarted => s_endingStarted;
    
    public static float SecondsRemaining => s_timerRunning ? Mathf.Max(0f, s_remaining) : -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { ResetEnding(); }

    
    public static void ResetEnding()
    {
        s_timerRunning = false;
        s_endingStarted = false;
        s_remaining = 0f;
        EndingActive = false;
    }

    // ------------------------------------------------------------------
    // Types
    // ------------------------------------------------------------------
    private const int G_BODY = 0, G_ARM_L = 1, G_ARM_R = 2, G_LEG_L = 3, G_LEG_R = 4;

    private class Shard
    {
        public Transform t;
        public int group;
        public Vector3 home, pivot, dir, spin, baseScale;
        public Quaternion baseRot;
        public float speed, phase;
    }

    private class Girl
    {
        public Transform root;
        public Shard[] shards;
        public Material mat;
        public Light glow;
        public float alpha, alphaTarget, frag, fragTarget, dissolve, runPhase, swingAmt;
        public float tearY, tearOff, tearUntil;
        public bool running, arrived, dissolving, gone;
        public Vector3 runTarget;
    }

    private class Chunk
    {
        public Transform t;
        public Renderer[] rs;
        public Vector3 pos0, scale0, dir, tumble;
        public Quaternion rot0;
        public float delay, age, speed, life;
        public bool flying;
    }

    // ------------------------------------------------------------------
    // State
    // ------------------------------------------------------------------
    private Camera _cam;
    private Transform _player;

    
    private bool _atmoCaptured;
    private bool _fogOn0;
    private FogMode _fogMode0;
    private Color _fogColor0, _ambient0;
    private float _fogDensity0, _ambientInt0, _skyExposure0;
    private Color _skyTint0, _skyTint1;
    private Material _skyInst;
    private Light _sun;
    private Color _sunColor0;
    private float _sunInt0;

    
    private float _dread, _dreadTarget;
    private bool _heartbeatOn = true;
    private float _nextBeat, _beatPulse;
    private float _shake, _fovKick, _fov0;
    private float _flashA, _flashDecay = 1f, _coverA;
    private Color _flashColor = Color.white;
    private float _glitchLeft;
    private bool _glitchOn;
    private bool _slowed;

   
    private bool _camLocked;
    private Camera _orbitCam;
    private Vector3 _origCamPos, _orbitCenter;
    private Quaternion _origCamRot;
    private float _orbitAngle, _orbitBlend, _orbitDist = -1f;
    private readonly List<MonoBehaviour> _frozen = new List<MonoBehaviour>();

    
    private Girl _girl;
    private Mesh _cubeMesh;
    private readonly List<Chunk> _chunks = new List<Chunk>();
    private readonly List<Chunk> _live = new List<Chunk>();
    private int _nextChunk;
    private bool _breaking;
    private float _breakClock;

   
    private Canvas _canvas;
    private Image _vig, _flash, _cover;
    private CanvasGroup _screamGroup, _subGroup;
    private TextMeshProUGUI _scream, _screamRed, _screamCyan, _sub;
    private float _screamTarget, _subTarget;
    private readonly Vector2 _screamBase = new Vector2(0f, 40f);
    private readonly List<Image> _glitchBars = new List<Image>();
    private Sprite _spWhite, _spVig;
    private readonly List<Texture2D> _textures = new List<Texture2D>();
    private readonly List<Sprite> _sprites = new List<Sprite>();

    
    private AudioSource _sfx, _rumble;
    private AudioClip _rumbleClip, _beatClip, _impactClip;
    private const int SampleRate = 22050;

    
    private void OnDestroy()
    {
        if (_slowed) Time.timeScale = 1f;
        if (_girl != null)
        {
            if (_girl.root != null) Destroy(_girl.root.gameObject);
            if (_girl.mat != null) Destroy(_girl.mat);
        }
        if (_skyInst != null) Destroy(_skyInst);
        foreach (var s in _sprites) if (s != null) Destroy(s);
        foreach (var t in _textures) if (t != null) Destroy(t);
        if (_rumbleClip != null) Destroy(_rumbleClip);
        if (_beatClip != null) Destroy(_beatClip);
        if (_impactClip != null) Destroy(_impactClip);
    }

    private void Update()
    {
        HandleDebug();
        if (!EndingActive) TickTimer();

        UpdateFx();
        UpdateGirl();
        UpdateBreakup();
    }

    private void LateUpdate()
    {
        if (!_camLocked || _orbitCam == null) return;

        float dt = Time.unscaledDeltaTime;

        _orbitAngle += orbitSpeed * dt;
        _orbitBlend = Mathf.Min(1f, _orbitBlend + dt / Mathf.Max(0.05f, orbitBlendTime));
        float blend = Mathf.SmoothStep(0f, 1f, _orbitBlend);

        Vector3 focus = _orbitCenter + Vector3.up * orbitFocusHeight;
        float a = _orbitAngle * Mathf.Deg2Rad;
        float h = orbitHeight + Mathf.Sin(Time.unscaledTime * 0.35f) * 0.35f; 
        Vector3 desired = _orbitCenter + new Vector3(Mathf.Cos(a) * orbitRadius, h, Mathf.Sin(a) * orbitRadius);

        
        Vector3 toCam = desired - focus;
        float dist = toCam.magnitude;
        Vector3 dir = dist > 0.01f ? toCam / dist : Vector3.back;
        float target = dist;
        RaycastHit hit;
        if (Physics.SphereCast(focus, 0.3f, dir, out hit, dist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            target = Mathf.Max(1.5f, hit.distance - 0.2f);

        if (_orbitDist < 0f) _orbitDist = dist;
        _orbitDist = Mathf.Lerp(_orbitDist, target, 1f - Mathf.Exp(-dt * (target < _orbitDist ? 10f : 2f)));

        Vector3 orbitPos = focus + dir * _orbitDist;
        Quaternion orbitRot = Quaternion.LookRotation(focus - orbitPos);

        Vector3 pos = Vector3.Lerp(_origCamPos, orbitPos, blend);
        Quaternion rot = Quaternion.Slerp(_origCamRot, orbitRot, blend);

        if (_shake > 0.001f)
        {
            pos += Random.insideUnitSphere * (_shake * 0.06f);
            rot *= Quaternion.Euler(Random.insideUnitSphere * (_shake * 2.2f));
        }

        _orbitCam.transform.SetPositionAndRotation(pos, rot);
        _orbitCam.fieldOfView = _fov0 + _fovKick;
    }

    private void CreateOrbitCamera()
    {
        _origCamPos = _cam.transform.position;
        _origCamRot = _cam.transform.rotation;
        _orbitCenter = PlayerPos();

        
        Vector3 flat = _origCamPos - _orbitCenter;
        flat.y = 0f;
        if (flat.sqrMagnitude < 1f)
        {
            
            Vector3 f = _cam.transform.forward;
            flat = new Vector3(-f.x, 0f, -f.z);
        }
        _orbitAngle = Mathf.Atan2(flat.z, flat.x) * Mathf.Rad2Deg;
        _orbitBlend = 0f;
        _orbitDist = -1f;

        var go = new GameObject("EndingOrbitCamera");
        _orbitCam = go.AddComponent<Camera>();
        _orbitCam.CopyFrom(_cam);
        _orbitCam.depth = _cam.depth + 10f;
        _orbitCam.enabled = true;
        go.tag = "MainCamera";
        go.transform.SetPositionAndRotation(_origCamPos, _origCamRot);

        
        CopyRenderingComponents(_cam.gameObject, go);

        _cam.enabled = false; 
        _fov0 = _orbitCam.fieldOfView;
        _camLocked = true;
    }

    private static void CopyRenderingComponents(GameObject from, GameObject to)
    {
        foreach (var mb in from.GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;
            Type type = mb.GetType();
            string ns = type.Namespace ?? "";
            if (!ns.StartsWith("UnityEngine.Rendering")) continue; 
            if (type.Name == "Volume") continue;                   
            try
            {
                var copy = to.GetComponent(type);
                if (copy == null) copy = to.AddComponent(type);
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(mb), copy);
            }
            catch (Exception) { /* not critical: the camera still works with defaults */ }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        Vector3 c = player != null ? player.position : transform.position;
        Gizmos.DrawWireSphere(c, safeRadius);
    }

    // ------------------------------------------------------------------
    // Debug + timer
    // ------------------------------------------------------------------
    private void HandleDebug()
    {
        if (debugRestartTimer)
        {
            debugRestartTimer = false;
            if (!EndingActive) { s_endingStarted = false; s_timerRunning = true; s_remaining = endingDelaySeconds; }
        }
        if (debugJumpToLastSeconds)
        {
            debugJumpToLastSeconds = false;
            if (!EndingActive) { s_endingStarted = false; s_timerRunning = true; s_remaining = 15f; }
        }
        if (debugStartEndingNow)
        {
            debugStartEndingNow = false;
            BeginEnding();
        }
    }

    private static bool AllLevelsComplete()
    {
        var wsm = WorldStateManager.Instance;
        if (wsm == null) return false;
        return wsm.IsLevelCompleted(LevelId.ArtistHitler)
            && wsm.IsLevelCompleted(LevelId.AppleForest)
            && wsm.IsLevelCompleted(LevelId.WrongBeer);
    }

    private void TickTimer()
    {
        if (s_endingStarted) return;

        bool all = debugIgnoreLevelRequirement || AllLevelsComplete();
        if (!all)
        {
            s_timerRunning = false;
            debugSecondsRemaining = -1f;
            _dreadTarget = 0f;
            return;
        }

        
        if (!s_timerRunning)
        {
            s_timerRunning = true;
            s_remaining = endingDelaySeconds;
        }

        s_remaining -= Time.deltaTime;
        debugSecondsRemaining = s_remaining;

        float warn = warningSeconds > 0.01f ? Mathf.Clamp01(1f - s_remaining / warningSeconds) : 0f;
        _dreadTarget = warn * 0.55f;

        if (s_remaining <= 0f) BeginEnding();
    }

    
    public void BeginEnding()
    {
        if (EndingActive || s_endingStarted) return;
        s_endingStarted = true;
        s_timerRunning = false;
        EndingActive = true;
        StartCoroutine(EndingRoutine());
    }

    // ------------------------------------------------------------------
    // The sequence
    // ------------------------------------------------------------------
    private static IEnumerator WaitU(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator EndingRoutine()
    {
        if (onEndingStarted != null) onEndingStarted.Invoke();

        _cam = ResolveCamera();
        _player = ResolvePlayer();
        EnsureOverlay();
        EnsureAudio();
        CaptureAtmosphereIfNeeded();

        
        var hub = FindOne<HubPrefabSwitcher>();
        if (hub != null) hub.enabled = false;

        
        _heartbeatOn = true;
        _dreadTarget = 0.75f;
        yield return WaitU(3.5f);

        
        FreezePlayer();
        SpawnGirl();
        PlaySfx(_beatClip, 0.9f);
        _girl.alphaTarget = 1f;
        _girl.fragTarget = 0.35f;
        _glitchLeft = 0.5f;
        yield return WaitU(1.8f);

        
        _dreadTarget = 1f;
        _girl.running = true;
        float guard = 0f;
        while (!_girl.arrived && guard < 20f)
        {
            guard += Time.unscaledDeltaTime;
            yield return null;
        }
        _girl.running = false;

        
        _heartbeatOn = false;
        _dreadTarget = 0.4f;
        _girl.fragTarget = 0.15f;
        yield return WaitU(1.4f);

        
        PlaySfx(daughterScreamClip, daughterScreamVolume);
        PlaySfx(_impactClip, 1f);
        _flashColor = Color.white; _flashA = 1f; _flashDecay = 0.7f;
        _shake = 1.8f;
        _fovKick = 16f;
        _glitchLeft = 1.4f;
        _girl.fragTarget = 1f;
        _dreadTarget = 1f;
        ShowScream(daughterLine);
        StartBreakup();

        Time.timeScale = 0.25f;
        _slowed = true;
        yield return WaitU(1.1f);
        Time.timeScale = 1f;
        _slowed = false;
        _girl.fragTarget = 0.6f;

        
        yield return WaitU(3.0f);
        _screamTarget = 0f;
        yield return WaitU(0.8f);

        _subTarget = 1f;
        PlaySfx(daughterWhisperClip, daughterWhisperVolume);
        yield return TypeText(_sub, daughterWhisper, 18f);
        yield return WaitU(2.2f);
        _subTarget = 0f;

        _girl.dissolving = true;
        _girl.fragTarget = 1f;
        while (!_girl.gone) yield return null;

        while (_breakClock < breakupDuration + 1.5f) yield return null;

        
        _dreadTarget = 0.5f;
        yield return WaitU(2.5f);

        
        _flashColor = Color.white;
        _flashDecay = 0f;
        float t = 0f;
        while (t < 1.8f)
        {
            t += Time.unscaledDeltaTime;
            _flashA = Mathf.SmoothStep(0f, 1f, t / 1.8f);
            yield return null;
        }
        _flashA = 1f;
        yield return WaitU(0.4f);

        t = 0f;
        while (t < 1.2f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 1.2f);
            _coverA = k;
            _flashA = 1f - k;
            yield return null;
        }
        _coverA = 1f;
        _flashA = 0f;

        _dreadTarget = 0f;
        if (_rumble != null) _rumble.Stop();

        ParadoxOutroCinematic.Play(OnOutroFinished, outroMusic, font, closingQuote, closingQuoteAuthor);
    }

    private void OnOutroFinished()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        EndingActive = false;

        if (onEndingComplete != null) onEndingComplete.Invoke();

        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        if (quitIfNoMenuScene)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
    }

    // ------------------------------------------------------------------
    // Player / camera helpers
    // ------------------------------------------------------------------
    private Camera ResolveCamera()
    {
        if (viewCamera != null) return viewCamera;
        if (Camera.main != null) return Camera.main;
        return FindOne<Camera>();
    }

    private Transform ResolvePlayer()
    {
        if (player != null) return player;
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) return go.transform;
        return _cam != null ? _cam.transform : null;
    }

    private Vector3 PlayerPos()
    {
        if (_player != null) return _player.position;
        if (_cam != null) return _cam.transform.position - Vector3.up * 1.6f;
        return transform.position;
    }

    private static bool IsProjectScript(MonoBehaviour mb)
    {
        string asm = mb.GetType().Assembly.GetName().Name;
        return asm.StartsWith("Assembly-CSharp");
    }

    private void FreezePlayer()
    {
        if (!freezePlayer || _camLocked) return;

        if (_cam != null) CreateOrbitCamera();

        var targets = new List<MonoBehaviour>();
        if (_player != null) targets.AddRange(_player.GetComponentsInChildren<MonoBehaviour>(false));
        if (_cam != null && (_player == null || !_cam.transform.IsChildOf(_player)))
            targets.AddRange(_cam.GetComponentsInChildren<MonoBehaviour>(false));
        targets.AddRange(extraScriptsToDisable);

        foreach (var mb in targets)
        {
            if (mb == null || mb == this || !mb.enabled) continue;
            if (!extraScriptsToDisable.Contains(mb) && !IsProjectScript(mb)) continue;
            mb.enabled = false;
            _frozen.Add(mb);
        }

        if (_player != null)
            foreach (var rb in _player.GetComponentsInChildren<Rigidbody>())
                rb.isKinematic = true;
    }

    // ------------------------------------------------------------------
    // Fx (vignette, heartbeat, rumble, atmosphere, text, flashes)
    // ------------------------------------------------------------------
    private void UpdateFx()
    {
        float dt = Time.unscaledDeltaTime;
        float now = Time.unscaledTime;

        _dread = Mathf.MoveTowards(_dread, _dreadTarget, dt * 0.25f);

        if (_dread > 0.001f && !_atmoCaptured) CaptureAtmosphereIfNeeded();
        if (_atmoCaptured) ApplyAtmosphere(_dread);

        bool active = _dread > 0.001f || EndingActive;
        if (!active)
        {
            if (_canvas != null) _vig.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, 0f);
            if (_rumble != null && _rumble.isPlaying) _rumble.Stop();
            return;
        }

        EnsureOverlay();
        EnsureAudio();

        
        float rv = _dread * 0.85f;
        _rumble.volume = rv;
        if (rv > 0.01f && !_rumble.isPlaying) _rumble.Play();
        else if (rv <= 0.01f && _rumble.isPlaying) _rumble.Stop();

        
        if (_heartbeatOn && _dread > 0.2f && now >= _nextBeat)
        {
            _nextBeat = now + Mathf.Lerp(1.8f, 0.7f, _dread);
            PlaySfx(_beatClip, 0.25f + 0.55f * _dread);
            _beatPulse = 1f;
        }
        _beatPulse = Mathf.MoveTowards(_beatPulse, 0f, dt * 3f);

        
        float pulse = 0.5f + 0.5f * Mathf.Sin(now * Mathf.Lerp(1.5f, 5f, _dread));
        float va = Mathf.Clamp01(_dread * (0.45f + 0.25f * pulse) + _beatPulse * 0.25f * _dread);
        _vig.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, va);

        
        if (_glitchLeft <= 0f && Random.value < _dread * _dread * 0.03f) _glitchLeft = Random.Range(0.06f, 0.2f);
        UpdateGlitch(dt);

        
        _shake *= Mathf.Exp(-dt * 3f);
        if (_shake < 0.005f) _shake = 0f;
        _fovKick = Mathf.MoveTowards(_fovKick, 0f, dt * 10f);

        _flashA = Mathf.MoveTowards(_flashA, 0f, dt * _flashDecay);
        _flash.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, _flashA);
        _cover.color = new Color(0f, 0f, 0f, _coverA);

        
        _screamGroup.alpha = Mathf.MoveTowards(_screamGroup.alpha, _screamTarget, dt * (_screamTarget > 0.5f ? 8f : 1.2f));
        if (_screamGroup.alpha > 0.01f)
        {
            float pop = 1f + 0.04f * Mathf.Sin(now * 30f);
            _scream.rectTransform.anchoredPosition = _screamBase + Random.insideUnitCircle * 5f;
            _screamRed.rectTransform.anchoredPosition = _screamBase + new Vector2(Random.Range(-16f, 4f), Random.Range(-5f, 5f));
            _screamCyan.rectTransform.anchoredPosition = _screamBase + new Vector2(Random.Range(-4f, 16f), Random.Range(-5f, 5f));
            _scream.rectTransform.localScale = Vector3.one * pop;
        }

        _subGroup.alpha = Mathf.MoveTowards(_subGroup.alpha, _subTarget, dt * 1.5f);
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
                Color c = Random.value < 0.5f ? daughterColor : Color.white;
                c.a = on ? Random.Range(0.06f, 0.3f) : 0f;
                b.color = c;
                b.rectTransform.anchoredPosition = new Vector2(Random.Range(-60f, 60f), Random.Range(-500f, 500f));
                b.rectTransform.sizeDelta = new Vector2(3200f, Random.Range(4f, 60f));
            }
        }
        else if (_glitchOn)
        {
            _glitchOn = false;
            foreach (var b in _glitchBars) b.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    private void ShowScream(string text)
    {
        _scream.text = text;
        _screamRed.text = text;
        _screamCyan.text = text;
        _screamGroup.alpha = 1f;
        _screamTarget = 1f;
    }

    private IEnumerator TypeText(TextMeshProUGUI t, string s, float cps)
    {
        t.text = s;
        t.maxVisibleCharacters = 0;
        t.ForceMeshUpdate();
        int total = t.textInfo.characterCount;
        float acc = 0f;
        int shown = 0;

        while (shown < total)
        {
            acc += Time.unscaledDeltaTime * cps;
            int n = (int)acc;
            if (n > 0)
            {
                acc -= n;
                shown = Mathf.Min(total, shown + n);
                t.maxVisibleCharacters = shown;
            }
            yield return null;
        }
        t.maxVisibleCharacters = 99999;
    }

    // ------------------------------------------------------------------
    // Atmosphere
    // ------------------------------------------------------------------
    private void CaptureAtmosphereIfNeeded()
    {
        if (_atmoCaptured) return;
        _atmoCaptured = true;

        _fogOn0 = RenderSettings.fog;
        _fogMode0 = RenderSettings.fogMode;
        _fogColor0 = RenderSettings.fogColor;
        _fogDensity0 = RenderSettings.fogDensity;
        _ambient0 = RenderSettings.ambientLight;
        _ambientInt0 = RenderSettings.ambientIntensity;

        
        var sky = RenderSettings.skybox;
        if (sky != null)
        {
            _skyInst = new Material(sky);
            RenderSettings.skybox = _skyInst;
            if (_skyInst.HasProperty("_Exposure")) _skyExposure0 = _skyInst.GetFloat("_Exposure");
            if (_skyInst.HasProperty("_SkyTint")) _skyTint0 = _skyInst.GetColor("_SkyTint");
            if (_skyInst.HasProperty("_Tint")) _skyTint1 = _skyInst.GetColor("_Tint");
        }

        _sun = RenderSettings.sun;
        if (_sun == null)
        {
            foreach (var l in FindMany<Light>())
            {
                if (l.type == LightType.Directional && l.enabled) { _sun = l; break; }
            }
        }
        if (_sun != null)
        {
            _sunColor0 = _sun.color;
            _sunInt0 = _sun.intensity;
        }
    }

    private void ApplyAtmosphere(float k)
    {
        if (k > 0.001f)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = Mathf.Lerp(_fogOn0 ? _fogDensity0 : 0f, apocalypseFogDensity, k);
        }
        else
        {
            RenderSettings.fog = _fogOn0;
            RenderSettings.fogMode = _fogMode0;
            RenderSettings.fogDensity = _fogDensity0;
        }

        RenderSettings.fogColor = Color.Lerp(_fogColor0, apocalypseFogColor, k);
        RenderSettings.ambientIntensity = Mathf.Lerp(_ambientInt0, _ambientInt0 * 0.25f, k);
        RenderSettings.ambientLight = Color.Lerp(_ambient0, apocalypseFogColor, k * 0.6f);

        if (_skyInst != null)
        {
            if (_skyInst.HasProperty("_Exposure"))
                _skyInst.SetFloat("_Exposure", Mathf.Lerp(_skyExposure0, apocalypseSkyExposure, k));
            if (_skyInst.HasProperty("_SkyTint"))
                _skyInst.SetColor("_SkyTint", Color.Lerp(_skyTint0, apocalypseFogColor, k));
            if (_skyInst.HasProperty("_Tint"))
                _skyInst.SetColor("_Tint", Color.Lerp(_skyTint1, apocalypseFogColor, k * 0.8f));
        }

        if (_sun != null)
        {
            _sun.color = Color.Lerp(_sunColor0, apocalypseSunColor, k);
            _sun.intensity = Mathf.Lerp(_sunInt0, _sunInt0 * 0.35f, k);
        }
    }

    // ------------------------------------------------------------------
    // The fragmented daughter
    // ------------------------------------------------------------------
    private float GroundY(Vector3 p, float fallback)
    {
        RaycastHit hit;
        if (Physics.Raycast(p + Vector3.up * 6f, Vector3.down, out hit, 40f,
                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return hit.point.y;
        return fallback;
    }

    private void SpawnGirl()
    {
        Vector3 playerPos = PlayerPos();
        Vector3 eye = _cam != null ? _cam.transform.position : playerPos + Vector3.up * 1.6f;

        Vector3 fwd = _cam != null ? _cam.transform.forward : (_player != null ? _player.forward : Vector3.forward);
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        fwd.Normalize();

        
        float[] angles = { 0f, 25f, -25f, 50f, -50f, 80f, -80f, 120f, -120f, 180f };
        float[] scales = { 1f, 0.7f, 0.5f };

        Vector3 spawn = playerPos + fwd * daughterSpawnDistance * 0.5f;
        Vector3 chosenDir = fwd;
        bool found = false;

        for (int si = 0; si < scales.Length && !found; si++)
        {
            for (int ai = 0; ai < angles.Length; ai++)
            {
                Vector3 d = Quaternion.AngleAxis(angles[ai], Vector3.up) * fwd;
                Vector3 p = playerPos + d * daughterSpawnDistance * scales[si];
                p.y = GroundY(p, playerPos.y);

                Vector3 chest = p + Vector3.up * (daughterHeight * 0.6f);
                if (!Physics.Linecast(eye, chest, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    spawn = p;
                    chosenDir = d;
                    found = true;
                    break;
                }
            }
        }

        _girl = BuildGirl();
        _girl.root.position = spawn;
        Vector3 faceDir = -chosenDir;
        _girl.root.rotation = Quaternion.LookRotation(faceDir);
        _girl.runTarget = playerPos + chosenDir * daughterStopDistance;
    }

    private Girl BuildGirl()
    {
        if (_cubeMesh == null)
        {
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cubeMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tmp);
        }

        var g = new Girl();
        var rootGo = new GameObject("FragmentedDaughter");
        g.root = rootGo.transform;
        g.root.localScale = Vector3.one * daughterHeight;

        
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        g.mat = new Material(sh);
        g.mat.color = new Color(daughterColor.r, daughterColor.g, daughterColor.b, 0f);

        var list = new List<Shard>();
        float k = Mathf.Max(60, shardCount) / 210f;

        
        void Add(int count, int group, Vector3 pivot, Func<Vector3> sample)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("shard");
                var tr = go.transform;
                tr.SetParent(g.root, false);

                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = _cubeMesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = g.mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                float size = Random.Range(0.03f, 0.075f);
                var s = new Shard
                {
                    t = tr,
                    group = group,
                    home = sample(),
                    pivot = pivot,
                    dir = Random.onUnitSphere,
                    spin = Random.onUnitSphere,
                    baseScale = new Vector3(size, size * Random.Range(0.6f, 2.2f), size * 0.35f),
                    baseRot = Random.rotation,
                    speed = Random.Range(0.6f, 2.2f),
                    phase = Random.Range(0f, 6.283f)
                };
                tr.localPosition = s.home;
                tr.localRotation = s.baseRot;
                tr.localScale = Vector3.zero;
                list.Add(s);
            }
        }

        
        Add(Mathf.RoundToInt(50 * k), G_BODY, Vector3.zero,
            () => new Vector3(0f, 0.86f, 0f) + Random.insideUnitSphere * 0.1f);
        Add(Mathf.RoundToInt(10 * k), G_BODY, Vector3.zero,
            () => new Vector3(Random.value < 0.5f ? -0.13f : 0.13f, 0.78f, 0f) + Random.insideUnitSphere * 0.035f);

        
        Add(Mathf.RoundToInt(90 * k), G_BODY, Vector3.zero, () =>
        {
            float y = Random.Range(0.36f, 0.7f);
            float t = Mathf.InverseLerp(0.36f, 0.7f, y);
            float r = Mathf.Lerp(0.21f, 0.07f, t) * Random.Range(0.7f, 1f);
            float a = Random.value * 6.2832f;
            return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r * 0.7f);
        });

        
        for (int side = 0; side < 2; side++)
        {
            float sgn = side == 0 ? -1f : 1f;
            Vector3 shoulder = new Vector3(sgn * 0.08f, 0.68f, 0f);
            Vector3 hand = new Vector3(sgn * 0.17f, 0.46f, 0.04f);
            Add(Mathf.RoundToInt(14 * k), side == 0 ? G_ARM_L : G_ARM_R, shoulder,
                () => Vector3.Lerp(shoulder, hand, Random.value) + Random.insideUnitSphere * 0.012f);
        }

       
        for (int side = 0; side < 2; side++)
        {
            float sgn = side == 0 ? -1f : 1f;
            Vector3 hip = new Vector3(sgn * 0.045f, 0.38f, 0f);
            Vector3 foot = new Vector3(sgn * 0.045f, 0.02f, 0f);
            Add(Mathf.RoundToInt(16 * k), side == 0 ? G_LEG_L : G_LEG_R, hip,
                () => Vector3.Lerp(hip, foot, Random.value) + Random.insideUnitSphere * 0.012f);
        }

        g.shards = list.ToArray();

        var lightGo = new GameObject("Glow");
        lightGo.transform.SetParent(g.root, false);
        lightGo.transform.localPosition = new Vector3(0f, 0.7f, 0.3f);
        g.glow = lightGo.AddComponent<Light>();
        g.glow.type = LightType.Point;
        g.glow.color = daughterColor;
        g.glow.range = 9f;
        g.glow.intensity = 0f;
        g.glow.shadows = LightShadows.None;

        return g;
    }

    private void UpdateGirl()
    {
        var g = _girl;
        if (g == null || g.gone) return;

        float dt = Time.deltaTime;
        float udt = Time.unscaledDeltaTime;
        float now = Time.time;

        g.alpha = Mathf.MoveTowards(g.alpha, g.alphaTarget, udt * 0.9f);
        g.frag = Mathf.MoveTowards(g.frag, g.fragTarget, udt * 1.6f);
        if (g.dissolving) g.dissolve = Mathf.Min(1f, g.dissolve + dt * 0.3f);

        
        Vector3 p = g.root.position;
        if (g.running)
        {
            Vector3 to = g.runTarget - p;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist <= 0.1f)
            {
                g.running = false;
                g.arrived = true;
            }
            else
            {
                Vector3 dir = to / dist;
                p += dir * Mathf.Min(dist, daughterRunSpeed * dt);
                g.root.rotation = Quaternion.Slerp(g.root.rotation, Quaternion.LookRotation(dir), 1f - Mathf.Exp(-dt * 10f));
                g.runPhase += dt * daughterRunSpeed * 1.5f;
            }
        }
        else if (_cam != null)
        {
            Vector3 look = _cam.transform.position - p;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                g.root.rotation = Quaternion.Slerp(g.root.rotation, Quaternion.LookRotation(look), 1f - Mathf.Exp(-dt * 6f));
        }
        p.y = Mathf.Lerp(p.y, GroundY(p, p.y), 1f - Mathf.Exp(-dt * 12f));
        g.root.position = p;

        
        g.swingAmt = Mathf.MoveTowards(g.swingAmt, g.running ? 1f : 0f, dt * 4f);
        float sw = Mathf.Sin(g.runPhase) * g.swingAmt;
        float bob = Mathf.Abs(sw) * 0.05f;
        Quaternion qArmL = Quaternion.AngleAxis(-sw * 40f, Vector3.right);
        Quaternion qArmR = Quaternion.AngleAxis(sw * 40f, Vector3.right);
        Quaternion qLegL = Quaternion.AngleAxis(sw * 50f, Vector3.right);
        Quaternion qLegR = Quaternion.AngleAxis(-sw * 50f, Vector3.right);

        
        if (now > g.tearUntil && Random.value < 0.03f + 0.08f * g.frag)
        {
            g.tearY = Random.Range(0.05f, 1f);
            g.tearOff = Random.Range(-0.25f, 0.25f);
            g.tearUntil = now + Random.Range(0.05f, 0.18f);
        }
        bool tearing = now < g.tearUntil;

        float fragPow = g.frag * g.frag;
        float miss = Mathf.Lerp(0.04f, 0.4f, g.frag);
        float d = g.dissolve;

        foreach (var s in g.shards)
        {
            Vector3 pos = s.home;
            switch (s.group)
            {
                case G_ARM_L: pos = s.pivot + qArmL * (s.home - s.pivot); break;
                case G_ARM_R: pos = s.pivot + qArmR * (s.home - s.pivot); break;
                case G_LEG_L: pos = s.pivot + qLegL * (s.home - s.pivot); break;
                case G_LEG_R: pos = s.pivot + qLegR * (s.home - s.pivot); break;
            }
            pos.y += bob;

            
            float wob = 0.5f + 0.5f * Mathf.Sin(now * s.speed + s.phase);
            pos += s.dir * (fragPow * 0.9f * wob + 0.015f);
            pos += Random.insideUnitSphere * (0.004f + 0.03f * g.frag);

            if (tearing && Mathf.Abs(s.home.y - g.tearY) < 0.07f) pos.x += g.tearOff;

           
            if (d > 0f) pos += Vector3.up * (d * d * (1.5f + s.speed)) + s.dir * (d * 0.8f);

            s.t.localPosition = pos;
            s.t.localRotation = Quaternion.AngleAxis(now * s.speed * 40f * (1f + g.frag * 3f), s.spin) * s.baseRot;

            float vis = Random.value < miss ? 0f : 1f;
            s.t.localScale = s.baseScale * (vis * (1f - d));
        }

        
        Color c = Color.Lerp(daughterColor, new Color(1f, 0.3f, 0.4f), g.frag * 0.4f * Random.value);
        c.a = g.alpha * (0.8f + 0.2f * Random.value) * (1f - d * 0.5f);
        g.mat.color = c;
        g.glow.intensity = g.alpha * (1.2f + 1.5f * g.frag) * (0.7f + 0.3f * Random.value) * (1f - d);

        if (d >= 1f)
        {
            g.gone = true;
            g.root.gameObject.SetActive(false);
        }
    }

    // ------------------------------------------------------------------
    // The city breakup
    // ------------------------------------------------------------------
    private List<Transform> AutoFindCityRoots()
    {
        var result = new List<Transform>();
        foreach (var go in gameObject.scene.GetRootGameObjects())
        {
            var tr = go.transform;
            if (tr == transform.root) continue;
            if (_girl != null && tr == _girl.root) continue;
            if (_player != null && _player.IsChildOf(tr)) continue;
            if (_cam != null && _cam.transform.IsChildOf(tr)) continue;
            if (go.GetComponentInChildren<Canvas>(true) != null) continue;
            if (go.GetComponentInChildren<EventSystem>(true) != null) continue;
            if (go.GetComponentInChildren<Camera>(true) != null) continue;
            result.Add(tr);
        }
        Debug.LogWarning("[ParadoxEndingDirector] No City Roots assigned, so every other root object in the scene is used. " +
                         "Drag your city prefab into 'City Roots' for a cleaner result.");
        return result;
    }

    private void GatherChunks()
    {
        _chunks.Clear();
        _live.Clear();
        _nextChunk = 0;

        var roots = new List<Transform>();
        foreach (var r in cityRoots) if (r != null) roots.Add(r);
        if (roots.Count == 0) roots = AutoFindCityRoots();

        
        var renderers = new List<Renderer>();
        var owners = new HashSet<Transform>();
        foreach (var root in roots)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(false))
            {
                if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                if (_player != null && r.transform.IsChildOf(_player)) continue;
                if (r.transform.IsChildOf(transform)) continue;
                if (_girl != null && r.transform.IsChildOf(_girl.root)) continue;
                renderers.Add(r);
                owners.Add(r.transform);
            }
        }

        Vector3 epicentre = PlayerPos();
        float safeSqr = safeRadius * safeRadius;
        var seen = new HashSet<Transform>();
        float maxDist = 1f;
        var temp = new List<KeyValuePair<Chunk, float>>();

        foreach (var r in renderers)
        {
            Transform tr = r.transform;

            
            bool nested = false;
            for (Transform up = tr.parent; up != null; up = up.parent)
            {
                if (owners.Contains(up)) { nested = true; break; }
            }
            if (nested || !seen.Add(tr)) continue;

            Bounds b = r.bounds;
            if (b.SqrDistance(epicentre) < safeSqr) continue; 

            var c = new Chunk
            {
                t = tr,
                rs = tr.GetComponentsInChildren<Renderer>(false),
                pos0 = tr.position,
                rot0 = tr.rotation,
                scale0 = tr.localScale
            };

            Vector3 radial = c.pos0 - epicentre;
            radial.y = 0f;
            radial = radial.sqrMagnitude > 0.01f ? radial.normalized : new Vector3(Random.value - 0.5f, 0f, Random.value - 0.5f).normalized;
            Vector3 swirl = Vector3.Cross(Vector3.up, radial);

            
            c.dir = (Vector3.up + swirl * 0.55f + radial * 0.25f + Random.insideUnitSphere * 0.25f).normalized;
            c.speed = ascendSpeed * Random.Range(0.4f, 1.2f);
            c.tumble = Random.onUnitSphere * (tumbleSpeed * Random.Range(0.3f, 1.2f));
            c.life = Random.Range(5f, 9f);

            float dist = (b.center - epicentre).magnitude;
            maxDist = Mathf.Max(maxDist, Mathf.Min(dist, 800f));
            temp.Add(new KeyValuePair<Chunk, float>(c, dist));
        }

        foreach (var kv in temp)
        {
            kv.Key.delay = Mathf.Clamp01(kv.Value / maxDist) * breakupDuration * 0.85f + Random.Range(0f, 0.5f);
            _chunks.Add(kv.Key);
        }
        _chunks.Sort((a, b) => a.delay.CompareTo(b.delay));
    }

    private void StartBreakup()
    {
        GatherChunks();
        _breakClock = 0f;
        _breaking = true;
    }

    private void UpdateBreakup()
    {
        if (!_breaking) return;

        float dt = Time.deltaTime;
        _breakClock += dt;

        
        while (_nextChunk < _chunks.Count && _chunks[_nextChunk].delay <= _breakClock)
        {
            var c = _chunks[_nextChunk++];
            if (c.t == null) continue;
            foreach (var col in c.t.GetComponentsInChildren<Collider>()) col.enabled = false;
            _live.Add(c);
        }

        const float tremor = 0.8f;
        for (int i = _live.Count - 1; i >= 0; i--)
        {
            var c = _live[i];
            if (c.t == null) { _live.RemoveAt(i); continue; }

            c.age += dt;

            if (c.age < tremor)
            {
                
                float k = c.age / tremor;
                c.t.position = c.pos0 + Random.insideUnitSphere * (0.06f + 0.25f * k);
                bool on = Random.value > 0.25f * k;
                foreach (var r in c.rs) if (r != null) r.enabled = on;
            }
            else
            {
                if (!c.flying)
                {
                    c.flying = true;
                    foreach (var r in c.rs) if (r != null) r.enabled = true;
                }

                float ft = c.age - tremor;
                float accel = ascendSpeed * 0.9f;
                c.t.position = c.pos0 + c.dir * (c.speed * ft + 0.5f * accel * ft * ft);
                c.t.rotation = Quaternion.Euler(c.tumble * ft) * c.rot0;

                float shrink = 1f - Mathf.Clamp01((ft - c.life * 0.5f) / (c.life * 0.5f));
                c.t.localScale = c.scale0 * shrink;

                if (ft >= c.life)
                {
                    c.t.gameObject.SetActive(false);
                    _live.RemoveAt(i);
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // Overlay UI (vignette, flashes, scream text, subtitle)
    // ------------------------------------------------------------------
    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private Image NewImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var rt = NewRect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private TextMeshProUGUI NewText(string name, Transform parent, float size, Color color, Vector2 pos, Vector2 box,
                                    FontStyles style, float spacing)
    {
        var rt = NewRect(name, parent);
        SetRect(rt, pos, box);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = "";
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
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

    private Sprite MakeSprite(Texture2D tex)
    {
        _textures.Add(tex);
        var s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        _sprites.Add(s);
        return s;
    }

    private void EnsureOverlay()
    {
        if (_canvas != null) return;

        
        var white = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var wp = new Color[16];
        for (int i = 0; i < wp.Length; i++) wp[i] = Color.white;
        white.SetPixels(wp);
        white.Apply();
        _spWhite = MakeSprite(white);

        const int vs = 128;
        var vt = new Texture2D(vs, vs, TextureFormat.RGBA32, false);
        vt.wrapMode = TextureWrapMode.Clamp;
        vt.filterMode = FilterMode.Bilinear;
        var vp = new Color[vs * vs];
        for (int y = 0; y < vs; y++)
        {
            for (int x = 0; x < vs; x++)
            {
                float nx = (x / (vs - 1f) - 0.5f) * 2f;
                float ny = (y / (vs - 1f) - 0.5f) * 2f;
                float dd = Mathf.Sqrt(nx * nx + ny * ny) / 1.414f;
                vp[y * vs + x] = new Color(1f, 1f, 1f, Mathf.Pow(dd, 2.0f));
            }
        }
        vt.SetPixels(vp);
        vt.Apply();
        _spVig = MakeSprite(vt);

        
        var canvasGo = new GameObject("EndingOverlay");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 29000;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Transform root = canvasGo.transform;

        _vig = NewImage("Vignette", root, _spVig, new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, 0f));
        Stretch(_vig.rectTransform);

        for (int i = 0; i < 5; i++)
        {
            var b = NewImage("Glitch" + i, root, _spWhite, new Color(1f, 1f, 1f, 0f));
            SetRect(b.rectTransform, Vector2.zero, new Vector2(3200f, 10f));
            _glitchBars.Add(b);
        }

        _flash = NewImage("Flash", root, _spWhite, new Color(1f, 1f, 1f, 0f));
        Stretch(_flash.rectTransform);

       
        var screamRoot = NewRect("Scream", root);
        Stretch(screamRoot);
        _screamGroup = NewGroup(screamRoot);
        var box = new Vector2(1800f, 300f);
        _screamRed = NewText("Red", screamRoot, 112f, new Color(1f, 0.1f, 0.25f, 1f), _screamBase, box, FontStyles.Bold, 6f);
        _screamCyan = NewText("Cyan", screamRoot, 112f, new Color(0f, 1f, 1f, 1f), _screamBase, box, FontStyles.Bold, 6f);
        _scream = NewText("Main", screamRoot, 112f, Color.white, _screamBase, box, FontStyles.Bold, 6f);
        _screamRed.alpha = 0.75f;
        _screamCyan.alpha = 0.75f;

        
        var subRoot = NewRect("Subtitle", root);
        Stretch(subRoot);
        _subGroup = NewGroup(subRoot);
        _sub = NewText("Text", subRoot, 40f, new Color(0.85f, 0.95f, 1f, 0.95f), new Vector2(0f, -300f),
                       new Vector2(1500f, 120f), FontStyles.Italic, 3f);

        
        _cover = NewImage("Cover", root, _spWhite, new Color(0f, 0f, 0f, 0f));
        Stretch(_cover.rectTransform);
    }

    // ------------------------------------------------------------------
    // Audio (synthesised so no assets are needed)
    // ------------------------------------------------------------------
    private void EnsureAudio()
    {
        if (_sfx != null) return;

        _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.spatialBlend = 0f;
        _sfx.ignoreListenerPause = true;

        _rumbleClip = rumbleLoopOverride != null ? rumbleLoopOverride : MakeRumble();
        _beatClip = MakeHeartbeat();
        _impactClip = MakeImpact();

        _rumble = gameObject.AddComponent<AudioSource>();
        _rumble.clip = _rumbleClip;
        _rumble.loop = true;
        _rumble.playOnAwake = false;
        _rumble.spatialBlend = 0f;
        _rumble.ignoreListenerPause = true;
        _rumble.volume = 0f;
    }

    private void PlaySfx(AudioClip clip, float volume)
    {
        if (_sfx != null && clip != null) _sfx.PlayOneShot(clip, volume);
    }

    private static AudioClip FinishClip(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip MakeRumble()
    {
        const float len = 6f;
        int n = (int)(SampleRate * len);
        var d = new float[n];
        float lp = 0f, lp2 = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float noise = Random.value * 2f - 1f;
            lp += (noise - lp) * 0.015f;
            lp2 += (lp - lp2) * 0.05f;

            float lfo = 0.65f + 0.35f * Mathf.Sin(6.2832f * 0.5f * t);
            float sub = Mathf.Sin(6.2832f * 36f * t) * 0.35f + Mathf.Sin(6.2832f * 54f * t) * 0.15f;
            float edge = Mathf.Clamp01(Mathf.Min(t, len - t) / 0.08f);
            d[i] = Mathf.Clamp((lp2 * 9f + sub) * lfo * edge, -1f, 1f) * 0.8f;
        }
        return FinishClip("ParadoxRumble", d);
    }

    private static float Thump(float t, float t0, float amp)
    {
        float x = t - t0;
        if (x < 0f) return 0f;
        float f = 62f - 28f * Mathf.Clamp01(x * 6f);
        return Mathf.Sin(6.2832f * f * x) * Mathf.Exp(-x * 16f) * amp;
    }

    private static AudioClip MakeHeartbeat()
    {
        int n = (int)(SampleRate * 0.9f);
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            d[i] = Mathf.Clamp(Thump(t, 0f, 1f) + Thump(t, 0.3f, 0.75f), -1f, 1f);
        }
        return FinishClip("ParadoxHeartbeat", d);
    }

    private static AudioClip MakeImpact()
    {
        const float len = 1.6f;
        int n = (int)(SampleRate * len);
        var d = new float[n];
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float f = 90f - 55f * Mathf.Clamp01(t * 1.5f);
            float boom = Mathf.Sin(6.2832f * f * t) * Mathf.Exp(-t * 3f);
            lp += ((Random.value * 2f - 1f) - lp) * 0.2f;
            float crack = lp * Mathf.Exp(-t * 9f) * 0.8f;
            d[i] = Mathf.Clamp(boom + crack, -1f, 1f);
        }
        return FinishClip("ParadoxImpact", d);
    }

    // ------------------------------------------------------------------
    // Version-safe Find helpers
    // ------------------------------------------------------------------
    private static T FindOne<T>() where T : UnityEngine.Object
    {
#if UNITY_2022_2_OR_NEWER
        return FindFirstObjectByType<T>();
#else
        return FindObjectOfType<T>();
#endif
    }

    private static T[] FindMany<T>() where T : UnityEngine.Object
    {
#if UNITY_2022_2_OR_NEWER
        return FindObjectsByType<T>(FindObjectsSortMode.None);
#else
        return FindObjectsOfType<T>();
#endif
    }
}