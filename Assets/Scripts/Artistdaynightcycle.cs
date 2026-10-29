using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


[DisallowMultipleComponent]
public class ArtistDayNightCycle : MonoBehaviour
{
    

    [System.Serializable]
    public struct TimeColors
    {
        public Color night;
        public Color twilight;
        public Color day;

        public TimeColors(Color night, Color twilight, Color day)
        {
            this.night = night;
            this.twilight = twilight;
            this.day = day;
        }
    }

    private class Lamp
    {
        public Light light;
        public float baseIntensity;
        public bool createdByUs;
        public bool origEnabled;
        public float delay;      
        public float level;      
        public bool allowed;     
        public float sqrDist;
    }

    private class WindowGlow
    {
        public Renderer renderer;
        public Material[] original;
        public Material[] instances;
        public List<int> indices;
        public float delay;
        public float applied = -1f;
    }

    private class SavedRenderState
    {
        public UnityEngine.Rendering.AmbientMode ambientMode;
        public Color ambientSky, ambientEquator, ambientGround, ambientLight;
        public float ambientIntensity;
        public float reflectionIntensity;
        public bool fog;
        public FogMode fogMode;
        public Color fogColor;
        public float fogDensity;
        public Material skybox;
        public Light sun;
    }

   

    [Header("Scene")]
    [Tooltip("The cycle only runs while this scene is the active scene. Must match the scene name exactly.")]
    [SerializeField] private string targetSceneName = "Artist_Hitler_Level";

    [Header("Time")]
    [Tooltip("Hour of the day (0-24) the level starts at. 14 = early afternoon, so dusk arrives during the level.")]
    [SerializeField, Range(0f, 24f)] private float startHour = 14f;
    [Tooltip("Real-time seconds for one full 24 hour day.")]
    [SerializeField] private float dayLengthSeconds = 600f;
    [SerializeField] private float timeSpeedMultiplier = 1f;
    [Tooltip("Tick to stop time. While ticked you can drag 'Current Hour' to preview any time of day.")]
    [SerializeField] private bool freezeTime = false;
    [Tooltip("Live clock. Shows the current hour while playing.")]
    [SerializeField, Range(0f, 24f)] private float currentHour = 14f;
    [SerializeField, Range(3f, 9f)] private float sunriseHour = 6f;
    [SerializeField, Range(15f, 21f)] private float sunsetHour = 18f;

    [Header("Sun and moon")]
    [Tooltip("Compass direction the sun travels along.")]
    [SerializeField] private float sunYaw = 30f;
    [SerializeField] private float sunIntensity = 1.1f;
    [SerializeField] private LightShadows sunShadows = LightShadows.Soft;
    [Tooltip("Switch off any other directional lights in the scene so they don't light the night.")]
    [SerializeField] private bool disableOtherDirectionalLights = true;
    [SerializeField] private Color moonLightColor = new Color(0.55f, 0.65f, 1f);
    [SerializeField] private float moonIntensity = 0.28f;
    [SerializeField] private bool moonShadows = false;
    [SerializeField] private Color moonDiscColor = new Color(0.92f, 0.95f, 1f);
    [Tooltip("Moon size as a fraction of the sky dome radius.")]
    [SerializeField] private float moonDiscSize = 0.05f;

    [Header("Sky colours (night / twilight / day)")]
    [SerializeField]
    private TimeColors ambientColors = new TimeColors(
        new Color(0.07f, 0.09f, 0.19f), new Color(0.48f, 0.34f, 0.36f), new Color(0.55f, 0.60f, 0.70f));
    [SerializeField]
    private TimeColors skyTintColors = new TimeColors(
        new Color(0.08f, 0.10f, 0.20f), new Color(0.90f, 0.52f, 0.38f), new Color(0.5f, 0.5f, 0.5f));
    [SerializeField]
    private TimeColors sunColors = new TimeColors(
        new Color(1f, 0.5f, 0.25f), new Color(1f, 0.55f, 0.3f), new Color(1f, 0.95f, 0.85f));
    [SerializeField]
    private TimeColors fogColors = new TimeColors(
        new Color(0.04f, 0.05f, 0.10f), new Color(0.65f, 0.43f, 0.38f), new Color(0.62f, 0.70f, 0.80f));

    [Header("Fog")]
    [SerializeField] private bool controlFog = true;
    [SerializeField] private float fogDensity = 0.002f;

    [Header("Stars")]
    [SerializeField] private int starCount = 600;
    [Tooltip("Apparent size range of the stars (small numbers, e.g. 0.002 - 0.005).")]
    [SerializeField] private Vector2 starSize = new Vector2(0.002f, 0.005f);
    [Tooltip("Sky dome is clamped to 90% of the camera's far clip plane.")]
    [SerializeField] private float skyDomeRadius = 450f;

    

    [Header("Street lights - existing lamps")]
    [Tooltip("Any object whose name contains one of these words gets a light at night " +
             "(or, if it already has a Light, that light is switched on at night).")]
    [SerializeField]
    private string[] lampNameKeywords =
        { "lamp", "streetlight", "street_light", "street light", "lantern", "lightpost", "light_post", "gaslight", "gas_light" };
    [SerializeField] private int maxDetectedLamps = 300;
    [Tooltip("Objects bigger than this in any direction are ignored (they're probably a group, not a lamp).")]
    [SerializeField] private float maxLampSize = 25f;

    [Header("Street lights - generated lamp posts (used when no lamps are found)")]
    [SerializeField] private bool generateLampsIfNoneFound = true;
    [Tooltip("Centre of the area to fill with lamps. If empty, the Player (or camera) position at start is used.")]
    [SerializeField] private Transform lampAreaCenter;
    [SerializeField] private float lampAreaSize = 160f;
    [SerializeField] private float lampSpacing = 14f;
    [SerializeField] private int maxGeneratedLamps = 120;
    [SerializeField] private float poleHeight = 4.2f;
    [SerializeField] private float poleRadius = 0.08f;
    [SerializeField] private float headSize = 0.45f;
    [SerializeField] private Color poleColor = new Color(0.10f, 0.10f, 0.11f);
    [Tooltip("Rays start this far above the ground. Spots with a roof / bridge / floating object above are skipped.")]
    [SerializeField] private float rayStartHeight = 150f;
    [Tooltip("Ground hits further than this from the reference ground height are skipped.")]
    [SerializeField] private float heightTolerance = 2f;

    [Header("Street lights - look and performance")]
    [SerializeField] private Color lampColor = new Color(1f, 0.78f, 0.45f);
    [SerializeField] private float lampIntensity = 3f;
    [SerializeField] private float lampRange = 16f;
    [Tooltip("Sun height (-1 to 1) at which lamps are fully off. ~0.1 = they start glowing just after sunset.")]
    [SerializeField] private float lampsOnSunHeight = 0.1f;
    [SerializeField] private float lampFadeSpeed = 3f;
    [Tooltip("Only the nearest N street lights to the camera actually emit light.")]
    [SerializeField] private int maxActiveLampLights = 24;
    [SerializeField] private float lampActivationDistance = 120f;
    [SerializeField] private float lampRefreshInterval = 0.25f;
    [SerializeField] private Color headOffColor = new Color(0.35f, 0.33f, 0.30f);
    [SerializeField] private Color headOnColor = new Color(1.3f, 1.0f, 0.6f);

    

    [Header("Window lights")]
    [SerializeField] private bool lightWindows = true;
    [Tooltip("Any object with a mesh whose name contains one of these words gets window lights (e.g. a 'Windows' object inside a building prefab).")]
    [SerializeField] private string[] windowNameKeywords = { "window" };
    [Tooltip("Chance (0-1) that any given window is lit. Lower = a more 'lived in' look with dark windows.")]
    [Range(0f, 1f)]
    [SerializeField] private float windowLitChance = 0.75f;
    [SerializeField] private Color windowLightColor = new Color(1f, 0.78f, 0.45f);
    [SerializeField] private float windowLightIntensity = 1.6f;
    [SerializeField] private float windowLightRange = 7f;
    [Tooltip("How far in front of the window the light sits. 0 = exactly inside the window object. " +
             "A little distance makes the light actually reach the wall and the street.")]
    [SerializeField] private float windowLightOffset = 1f;
    [Tooltip("A big window object (e.g. a whole facade) gets one light every this many units along it...")]
    [SerializeField] private float windowSpacing = 4f;
    [Tooltip("...and one row of lights per this much height.")]
    [SerializeField] private float floorHeight = 3.5f;
    [SerializeField] private int maxLightsPerWindowObject = 12;
    [SerializeField] private int maxDetectedWindows = 400;
    [Tooltip("Total cap on window lights created (disabled ones cost almost nothing).")]
    [SerializeField] private int maxWindowLights = 600;
    [Tooltip("Window objects bigger than this in any direction are ignored.")]
    [SerializeField] private float maxWindowObjectSize = 80f;
    [Tooltip("Only the nearest N window lights to the camera actually emit light.")]
    [SerializeField] private int maxActiveWindowLights = 40;
    [SerializeField] private float windowActivationDistance = 70f;

    [Header("Window glow (emissive material)")]
    [Tooltip("Also makes the window material itself glow. Needs a material with an Emission property.")]
    [SerializeField] private bool glowWindowMaterials = true;
    [SerializeField] private float windowEmissionIntensity = 1.5f;
    [Tooltip("If a window object's material isn't named window/glass/pane, it only glows when the mesh is smaller than this.")]
    [SerializeField] private float glowMaxRendererSize = 8f;

    [Header("Night-only objects (signs, etc.) - active only at night")]
    [SerializeField] private List<GameObject> nightOnlyObjects = new List<GameObject>();

    [Header("Events")]
    [SerializeField] private UnityEvent onNightFall;
    [SerializeField] private UnityEvent onDayBreak;

  

    public static ArtistDayNightCycle Instance { get; private set; }
    public float Hour => currentHour;
    public bool IsNight { get; private set; }
    public bool IsRunning => _running;

    public void SetHour(float hour) { currentHour = Mathf.Repeat(hour, 24f); }

  

    private bool _running;
    private bool _quitting;
    private bool _hourInitialised;

    private GameObject _root;
    private Light _sun, _moon;
    private bool _sunCreated;
    private Quaternion _sunOrigRot;
    private Color _sunOrigColor;
    private float _sunOrigIntensity;
    private LightShadows _sunOrigShadows;

    private readonly List<Light> _disabledLights = new List<Light>();
    private SavedRenderState _saved;

    private Material _skyMat, _starMat, _moonMat, _glowMat, _poleMat;
    private bool _fallbackSky;
    private Color _savedCamBackground;
    private Transform _starRoot, _moonTransform;
    private Renderer _starRenderer, _moonRenderer;

    private Gradient _ambientGrad, _skyGrad, _sunGrad, _fogGrad;
    private Camera _cam;

    private readonly List<Lamp> _lamps = new List<Lamp>();          // street lights
    private readonly List<Lamp> _windowLights = new List<Lamp>();   // window lights
    private readonly List<WindowGlow> _glows = new List<WindowGlow>();
    private readonly List<Lamp> _sortBuffer = new List<Lamp>();
    private int _windowObjectCount;
    private bool _lampsReady;
    private float _nextLampRefresh;

    private bool[] _nightObjectOriginal;

  

    private void OnEnable()
    {
        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        EvaluateScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        if (_running) StopCycle(!_quitting);
        if (Instance == this) Instance = null;
    }

    private void OnApplicationQuit() { _quitting = true; }

    private void OnValidate()
    {
        dayLengthSeconds = Mathf.Max(1f, dayLengthSeconds);
        starCount = Mathf.Max(0, starCount);
        lampSpacing = Mathf.Max(3f, lampSpacing);
        if (_running) BuildGradients();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { EvaluateScene(); }
    private void OnActiveSceneChanged(Scene oldScene, Scene newScene) { EvaluateScene(); }

    private void Update()
    {
        if (!_running) return;

        if (!freezeTime)
        {
            float hoursPerSecond = 24f / dayLengthSeconds;
            currentHour = Mathf.Repeat(currentHour + Time.deltaTime * hoursPerSecond * timeSpeedMultiplier, 24f);
        }

        ApplyTime(false);
    }

    private void LateUpdate()
    {
        if (!_running) return;
        FollowCamera();
    }

    private void OnDrawGizmosSelected()
    {
        if (lampAreaCenter == null) return;
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(lampAreaCenter.position, new Vector3(lampAreaSize, 2f, lampAreaSize));
    }

  

    private void EvaluateScene()
    {
        bool shouldRun = SceneManager.GetActiveScene().name == targetSceneName;

        
        if (_running && _root == null) StopCycle(false);

        if (shouldRun && !_running) StartCycle();
        else if (!shouldRun && _running) StopCycle(false);
    }

    private void StartCycle()
    {
        _running = true;

        if (!_hourInitialised)
        {
            currentHour = startHour;
            _hourInitialised = true;
        }

        _root = new GameObject("ArtistDayNight_Generated");

        SaveRenderState();
        BuildGradients();
        SetupSun();
        SetupMoon();
        SetupSky();
        SetupStars();
        SetupGlowMaterial();

        _nightObjectOriginal = new bool[nightOnlyObjects.Count];
        for (int i = 0; i < nightOnlyObjects.Count; i++)
            _nightObjectOriginal[i] = nightOnlyObjects[i] != null && nightOnlyObjects[i].activeSelf;

        if (controlFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientIntensity = 1f;

        ApplyTime(true);
        StartCoroutine(BuildLightsRoutine());
    }

    private void StopCycle(bool restoreSettings)
    {
        if (!_running) return;
        _running = false;
        _lampsReady = false;

        StopAllCoroutines();

        DisposeLampList(_lamps);
        DisposeLampList(_windowLights);

        foreach (WindowGlow g in _glows) ResetGlow(g);
        _glows.Clear();

        if (restoreSettings)
        {
            for (int i = 0; i < nightOnlyObjects.Count && _nightObjectOriginal != null && i < _nightObjectOriginal.Length; i++)
                if (nightOnlyObjects[i] != null) nightOnlyObjects[i].SetActive(_nightObjectOriginal[i]);

            RestoreRenderState();
        }

        foreach (Light l in _disabledLights)
            if (l != null) l.enabled = true;
        _disabledLights.Clear();

        if (_root != null) Destroy(_root);
        _root = null;

        DestroyMat(ref _skyMat);
        DestroyMat(ref _starMat);
        DestroyMat(ref _moonMat);
        DestroyMat(ref _glowMat);
        DestroyMat(ref _poleMat);

        _fallbackSky = false;
        IsNight = false;
    }

    private static void DisposeLampList(List<Lamp> list)
    {
        foreach (Lamp lamp in list)
        {
            if (lamp.light == null) continue;
            if (lamp.createdByUs) Destroy(lamp.light.gameObject);
            else { lamp.light.enabled = lamp.origEnabled; lamp.light.intensity = lamp.baseIntensity; }
        }
        list.Clear();
    }

    private static void DestroyMat(ref Material m)
    {
        if (m != null) Destroy(m);
        m = null;
    }

   

    private void SaveRenderState()
    {
        _saved = new SavedRenderState
        {
            ambientMode = RenderSettings.ambientMode,
            ambientSky = RenderSettings.ambientSkyColor,
            ambientEquator = RenderSettings.ambientEquatorColor,
            ambientGround = RenderSettings.ambientGroundColor,
            ambientLight = RenderSettings.ambientLight,
            ambientIntensity = RenderSettings.ambientIntensity,
            reflectionIntensity = RenderSettings.reflectionIntensity,
            fog = RenderSettings.fog,
            fogMode = RenderSettings.fogMode,
            fogColor = RenderSettings.fogColor,
            fogDensity = RenderSettings.fogDensity,
            skybox = RenderSettings.skybox,
            sun = RenderSettings.sun
        };
    }

    private void RestoreRenderState()
    {
        if (_saved == null) return;

        RenderSettings.ambientMode = _saved.ambientMode;
        RenderSettings.ambientSkyColor = _saved.ambientSky;
        RenderSettings.ambientEquatorColor = _saved.ambientEquator;
        RenderSettings.ambientGroundColor = _saved.ambientGround;
        RenderSettings.ambientLight = _saved.ambientLight;
        RenderSettings.ambientIntensity = _saved.ambientIntensity;
        RenderSettings.reflectionIntensity = _saved.reflectionIntensity;
        RenderSettings.fog = _saved.fog;
        RenderSettings.fogMode = _saved.fogMode;
        RenderSettings.fogColor = _saved.fogColor;
        RenderSettings.fogDensity = _saved.fogDensity;
        RenderSettings.skybox = _saved.skybox;
        RenderSettings.sun = _saved.sun;

        if (_sun != null && !_sunCreated)
        {
            _sun.transform.rotation = _sunOrigRot;
            _sun.color = _sunOrigColor;
            _sun.intensity = _sunOrigIntensity;
            _sun.shadows = _sunOrigShadows;
        }

        if (_fallbackSky)
        {
            Camera cam = GetCamera();
            if (cam != null) cam.backgroundColor = _savedCamBackground;
        }
    }

 

    private void BuildGradients()
    {
        _ambientGrad = BuildGradient(ambientColors);
        _skyGrad = BuildGradient(skyTintColors);
        _sunGrad = BuildGradient(sunColors);
        _fogGrad = BuildGradient(fogColors);
    }

    private Gradient BuildGradient(TimeColors c)
    {
        float rise = sunriseHour;
        float set = sunsetHour;

        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(c.night, 0f),
                new GradientColorKey(c.night, (rise - 1f) / 24f),
                new GradientColorKey(c.twilight, rise / 24f),
                new GradientColorKey(c.day, (rise + 2f) / 24f),
                new GradientColorKey(c.day, (set - 2f) / 24f),
                new GradientColorKey(c.twilight, set / 24f),
                new GradientColorKey(c.night, (set + 1.5f) / 24f),
                new GradientColorKey(c.night, 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }

    private void SetupSun()
    {
        _sun = RenderSettings.sun;

        if (_sun == null || _sun.type != LightType.Directional)
        {
            _sun = null;
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional || !l.enabled || !l.gameObject.activeInHierarchy) continue;
                if (_sun == null || l.intensity > _sun.intensity) _sun = l;
            }
        }

        if (_sun == null)
        {
            var go = new GameObject("Sun");
            go.transform.SetParent(_root.transform, false);
            _sun = go.AddComponent<Light>();
            _sun.type = LightType.Directional;
            _sunCreated = true;
        }
        else
        {
            _sunCreated = false;
            _sunOrigRot = _sun.transform.rotation;
            _sunOrigColor = _sun.color;
            _sunOrigIntensity = _sun.intensity;
            _sunOrigShadows = _sun.shadows;
        }

        RenderSettings.sun = _sun;

        if (disableOtherDirectionalLights)
        {
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l == _sun || l.type != LightType.Directional || !l.enabled) continue;
                l.enabled = false;
                _disabledLights.Add(l);
            }
        }
    }

    private void SetupMoon()
    {
        var go = new GameObject("Moon");
        go.transform.SetParent(_root.transform, false);
        _moon = go.AddComponent<Light>();
        _moon.type = LightType.Directional;
        _moon.color = moonLightColor;
        _moon.intensity = 0f;
        _moon.shadows = LightShadows.None;
    }

    private void SetupSky()
    {
        Shader skyShader = Shader.Find("Skybox/Procedural");

        if (skyShader != null)
        {
            _skyMat = new Material(skyShader) { name = "DayNight_Sky" };
            RenderSettings.skybox = _skyMat;
            return;
        }

        Debug.LogWarning("[ArtistDayNightCycle] Shader 'Skybox/Procedural' not found. Add it under " +
                         "Project Settings > Graphics > Always Included Shaders. Using a plain background colour instead.");
        RenderSettings.skybox = null;
        _fallbackSky = true;
        Camera cam = GetCamera();
        if (cam != null) _savedCamBackground = cam.backgroundColor;
    }

    private void SetupStars()
    {
        _starMat = CreateUnlitMaterial("DayNight_Stars", true);
        _moonMat = CreateUnlitMaterial("DayNight_Moon", true);

        if (_starMat == null || _moonMat == null)
        {
            Debug.LogWarning("[ArtistDayNightCycle] 'Sprites/Default' shader not found, so stars and the moon disc are skipped.");
            return;
        }

        
        var starGO = new GameObject("Stars");
        starGO.transform.SetParent(_root.transform, false);
        _starRoot = starGO.transform;

        var ps = starGO.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = starCount + 16;
        main.startSpeed = 0f;
        main.startLifetime = 1e8f;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = false;

        var psr = starGO.GetComponent<ParticleSystemRenderer>();
        psr.sharedMaterial = _starMat;
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        psr.receiveShadows = false;
        _starRenderer = psr;

        ps.Play();

        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < starCount; i++)
        {
            ep.position = Random.onUnitSphere;
            ep.velocity = Vector3.zero;
            ep.startLifetime = 1e8f;
            ep.startSize = Random.Range(starSize.x, starSize.y);

            
            Color tint = Random.value < 0.25f
                ? Color.Lerp(Color.white, Random.value < 0.5f ? new Color(0.7f, 0.8f, 1f) : new Color(1f, 0.9f, 0.7f), 0.8f)
                : Color.white;
            tint.a = Random.Range(0.5f, 1f);
            ep.startColor = tint;

            ps.Emit(ep, 1);
        }

        
        var moonGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moonGO.name = "MoonDisc";
        RemoveCollider(moonGO);
        moonGO.transform.SetParent(_root.transform, false);
        _moonTransform = moonGO.transform;
        _moonRenderer = moonGO.GetComponent<Renderer>();
        _moonRenderer.sharedMaterial = _moonMat;
        _moonRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _moonRenderer.receiveShadows = false;
    }

    private void SetupGlowMaterial()
    {
        _glowMat = CreateUnlitMaterial("DayNight_LampGlow", false);
        if (_glowMat != null) _glowMat.color = headOffColor;
    }

    private static Material CreateUnlitMaterial(string matName, bool needsAlpha)
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null && !needsAlpha) sh = Shader.Find("Unlit/Color");
        return sh == null ? null : new Material(sh) { name = matName };
    }

    private static void RemoveCollider(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
    }


    private static float Smooth(float x) { return x * x * (3f - 2f * x); }

    private static Color Scale(Color c, float f) { return new Color(c.r * f, c.g * f, c.b * f, 1f); }

    /// Sun angle in degrees: 0 = sunrise, 90 = noon, 180 = sunset, 270 = midnight.
    private float SunAngle(float hour)
    {
        float dayLength = sunsetHour - sunriseHour;

        if (hour >= sunriseHour && hour <= sunsetHour)
            return (hour - sunriseHour) / dayLength * 180f;

        float h = hour < sunriseHour ? hour + 24f : hour;
        float nightLength = 24f - dayLength;
        return 180f + (h - sunsetHour) / nightLength * 180f;
    }

    private void ApplyTime(bool silent)
    {
        if (_sun == null || _moon == null) return;

        float h01 = currentHour / 24f;

        
        float angle = SunAngle(currentHour);
        _sun.transform.rotation = Quaternion.Euler(angle, sunYaw, 0f);
        _moon.transform.rotation = Quaternion.Euler(angle + 180f, sunYaw, 0f);

        
        float s = -_sun.transform.forward.y;

        float sunFactor = Smooth(Mathf.InverseLerp(-0.02f, 0.25f, s));
        float moonFactor = Smooth(Mathf.InverseLerp(0f, 0.25f, -s));
        float daylight = Smooth(Mathf.InverseLerp(-0.15f, 0.35f, s));
        float starsAlpha = Mathf.Clamp01(Mathf.InverseLerp(0.02f, -0.25f, s));
        float lampFactor = 1f - Smooth(Mathf.InverseLerp(lampsOnSunHeight - 0.2f, lampsOnSunHeight, s));

        
        _sun.color = _sunGrad.Evaluate(h01);
        _sun.intensity = sunIntensity * sunFactor;
        _sun.shadows = sunFactor > 0.01f ? sunShadows : LightShadows.None;

        _moon.enabled = moonFactor > 0.001f;
        _moon.color = moonLightColor;
        _moon.intensity = moonIntensity * moonFactor;
        _moon.shadows = (moonShadows && moonFactor > 0.01f) ? LightShadows.Soft : LightShadows.None;

        
        Color amb = _ambientGrad.Evaluate(h01);
        RenderSettings.ambientSkyColor = amb;
        RenderSettings.ambientEquatorColor = Scale(amb, 0.8f);
        RenderSettings.ambientGroundColor = Scale(amb, 0.55f);
        if (_saved != null)
            RenderSettings.reflectionIntensity = _saved.reflectionIntensity * Mathf.Lerp(0.15f, 1f, daylight);

        
        Color fogCol = _fogGrad.Evaluate(h01);
        if (controlFog) RenderSettings.fogColor = fogCol;

        
        if (_skyMat != null)
        {
            _skyMat.SetColor("_SkyTint", _skyGrad.Evaluate(h01));
            _skyMat.SetColor("_GroundColor", fogCol);
            _skyMat.SetFloat("_Exposure", Mathf.Lerp(0.15f, 1.3f, daylight));
        }
        else if (_fallbackSky)
        {
            Camera cam = GetCamera();
            if (cam != null) cam.backgroundColor = Scale(_skyGrad.Evaluate(h01), Mathf.Lerp(0.2f, 1.6f, daylight));
        }

        
        if (_starRenderer != null)
        {
            _starRenderer.enabled = starsAlpha > 0.01f;
            _starMat.color = new Color(1f, 1f, 1f, starsAlpha);
        }

        if (_moonRenderer != null)
        {
            float moonAlpha = Mathf.Clamp01(Mathf.InverseLerp(0f, -0.2f, s));
            _moonRenderer.enabled = moonAlpha > 0.01f;
            Color mc = moonDiscColor;
            mc.a = moonAlpha;
            _moonMat.color = mc;
        }

        
        if (_glowMat != null) _glowMat.color = Color.Lerp(headOffColor, headOnColor, lampFactor);
        UpdateAllLights(lampFactor);

        
        bool night = lampFactor > 0.5f;
        if (night != IsNight || silent)
        {
            bool changed = night != IsNight;
            IsNight = night;

            for (int i = 0; i < nightOnlyObjects.Count; i++)
                if (nightOnlyObjects[i] != null) nightOnlyObjects[i].SetActive(night);

            if (changed && !silent)
            {
                if (night) onNightFall?.Invoke();
                else onDayBreak?.Invoke();
            }
        }
    }

    private void FollowCamera()
    {
        Camera cam = GetCamera();
        if (cam == null) return;

        Vector3 p = cam.transform.position;
        float r = Mathf.Min(skyDomeRadius, cam.farClipPlane * 0.9f);

        if (_starRoot != null)
        {
            _starRoot.position = p;
            _starRoot.localScale = Vector3.one * r;
            
            _starRoot.rotation = Quaternion.AngleAxis(currentHour / 24f * 360f, new Vector3(0.2f, 1f, 0.1f).normalized);
        }

        if (_moonTransform != null && _moon != null)
        {
            Vector3 dirToMoon = -_moon.transform.forward;
            _moonTransform.position = p + dirToMoon * (r * 0.95f);
            _moonTransform.localScale = Vector3.one * (r * moonDiscSize);
        }
    }

    private Camera GetCamera()
    {
        if (_cam == null || !_cam.isActiveAndEnabled) _cam = Camera.main;
        return _cam;
    }

    

    private IEnumerator BuildLightsRoutine()
    {
        
        yield return null;
        if (_root == null) yield break;

        Transform[] all = FindObjectsByType<Transform>(FindObjectsSortMode.None);

        
        int found = DetectExistingLamps(all);
        string source = "found in the scene";

        if (found == 0 && generateLampsIfNoneFound)
        {
            GenerateStreetLamps();
            source = "generated";
        }

        
        _windowObjectCount = 0;
        if (lightWindows)
            yield return StartCoroutine(DetectWindowsRoutine(all));

        if (!_running || _root == null) yield break;

        Debug.Log("[ArtistDayNightCycle] " + _lamps.Count + " street lights (" + source + "), " +
                  _windowLights.Count + " window lights on " + _windowObjectCount + " window objects, " +
                  _glows.Count + " glowing window renderers.");

        if (_lamps.Count == 0 && _windowLights.Count == 0)
            Debug.Log("[ArtistDayNightCycle] Nothing to light. Name lamp objects with one of the 'Lamp Name Keywords' " +
                      "and window objects with one of the 'Window Name Keywords'.");

        _nextLampRefresh = 0f;
        _lampsReady = true;
    }

    private static bool NameMatches(string objectName, string[] keywords)
    {
        for (int i = 0; i < keywords.Length; i++)
        {
            string k = keywords[i];
            if (!string.IsNullOrEmpty(k) && objectName.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    private static bool HasMatchingDescendant(Transform t, string[] keywords)
    {
        Transform[] kids = t.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < kids.Length; i++)
            if (kids[i] != t && NameMatches(kids[i].name, keywords)) return true;
        return false;
    }

    private static bool TryGetBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            if (!any) { bounds = renderers[i].bounds; any = true; }
            else bounds.Encapsulate(renderers[i].bounds);
        }
        return any;
    }

    private Light CreateLight(Transform parent, Vector3 worldPos, Color color, float range, string goName)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent, true);
        go.transform.position = worldPos;

        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.range = range;
        l.intensity = 0f;
        l.shadows = LightShadows.None;
        l.enabled = false;
        return l;
    }

    

    private int DetectExistingLamps(Transform[] all)
    {
        int added = 0;

        foreach (Transform t in all)
        {
            if (added >= maxDetectedLamps) break;
            if (t == null || t.IsChildOf(_root.transform)) continue;
            if (!NameMatches(t.name, lampNameKeywords)) continue;
            if (HasMatchingDescendant(t, lampNameKeywords)) continue; 

            
            bool usedExisting = false;
            foreach (Light l in t.GetComponentsInChildren<Light>(true))
            {
                if (l.type == LightType.Directional) continue;

                _lamps.Add(new Lamp { light = l, baseIntensity = l.intensity, origEnabled = l.enabled });
                l.enabled = false;
                usedExisting = true;
                added++;
            }
            if (usedExisting) continue;

            
            if (!TryGetBounds(t.GetComponentsInChildren<Renderer>(), out Bounds b)) continue;
            if (b.size.x > maxLampSize || b.size.y > maxLampSize || b.size.z > maxLampSize) continue;

            Vector3 top = new Vector3(b.center.x, b.max.y - 0.2f, b.center.z);
            Light nl = CreateLight(t, top, lampColor, lampRange, "NightLight_Auto");
            _lamps.Add(new Lamp { light = nl, baseIntensity = lampIntensity, createdByUs = true });
            added++;
        }

        return added;
    }

    

    private Vector3 GetLampAreaCentre()
    {
        if (lampAreaCenter != null) return lampAreaCenter.position;

        GameObject player = null;
        try { player = GameObject.FindGameObjectWithTag("Player"); }
        catch (UnityException) { /* Player tag not defined */ }
        if (player != null) return player.transform.position;

        Camera cam = GetCamera();
        return cam != null ? cam.transform.position : transform.position;
    }

    /// Nearest hit going straight down that is not a moving thing (rigidbody / character).
    private bool RaycastStaticDown(Vector3 origin, float distance, out RaycastHit result)
    {
        result = default;
        bool found = false;
        float best = float.MaxValue;

        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit h in hits)
        {
            if (h.rigidbody != null || h.collider is CharacterController) continue;
            if (h.collider.transform.IsChildOf(_root.transform)) continue;
            if (h.distance >= best) continue;

            best = h.distance;
            result = h;
            found = true;
        }
        return found;
    }

    private void GenerateStreetLamps()
    {
        lampSpacing = Mathf.Max(3f, lampSpacing);
        Vector3 centre = GetLampAreaCentre();

        
        float groundY = centre.y;
        if (RaycastStaticDown(centre + Vector3.up, 200f, out RaycastHit centreHit))
            groundY = centreHit.point.y;

        
        var cells = new List<Vector3>();
        float half = lampAreaSize * 0.5f;
        for (float x = -half; x <= half; x += lampSpacing)
            for (float z = -half; z <= half; z += lampSpacing)
                cells.Add(new Vector3(centre.x + x, 0f, centre.z + z));

        cells.Sort((a, b) =>
        {
            float da = (a.x - centre.x) * (a.x - centre.x) + (a.z - centre.z) * (a.z - centre.z);
            float db = (b.x - centre.x) * (b.x - centre.x) + (b.z - centre.z) * (b.z - centre.z);
            return da.CompareTo(db);
        });

        int made = 0;
        foreach (Vector3 cell in cells)
        {
            if (made >= maxGeneratedLamps) break;

            Vector3 origin = new Vector3(cell.x, groundY + rayStartHeight, cell.z);
            if (!RaycastStaticDown(origin, rayStartHeight + 100f, out RaycastHit hit)) continue;

            
            if (hit.normal.y < 0.85f) continue;
            if (Mathf.Abs(hit.point.y - groundY) > heightTolerance) continue;

            
            Vector3 p = hit.point;
            if (Physics.CheckCapsule(p + Vector3.up * 0.6f, p + Vector3.up * (poleHeight - 0.2f),
                                     poleRadius + 0.35f, ~0, QueryTriggerInteraction.Ignore))
                continue;

            BuildLampPost(p);
            made++;
        }
    }

    private void BuildLampPost(Vector3 groundPoint)
    {
        var post = new GameObject("StreetPost");
        post.transform.SetParent(_root.transform, false);
        post.transform.position = groundPoint;

        
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Pole";
        RemoveCollider(pole);
        pole.transform.SetParent(post.transform, false);
        pole.transform.localPosition = new Vector3(0f, poleHeight * 0.5f, 0f);
        pole.transform.localScale = new Vector3(poleRadius * 2f, poleHeight * 0.5f, poleRadius * 2f);

        var poleRenderer = pole.GetComponent<Renderer>();
        if (_poleMat == null && poleRenderer.sharedMaterial != null)
        {
            _poleMat = new Material(poleRenderer.sharedMaterial) { name = "DayNight_Pole" };
            _poleMat.color = poleColor;
        }
        if (_poleMat != null) poleRenderer.sharedMaterial = _poleMat;

        
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        RemoveCollider(head);
        head.transform.SetParent(post.transform, false);
        head.transform.localPosition = new Vector3(0f, poleHeight + headSize * 0.3f, 0f);
        head.transform.localScale = Vector3.one * headSize;

        var headRenderer = head.GetComponent<Renderer>();
        headRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        headRenderer.receiveShadows = false;
        if (_glowMat != null) headRenderer.sharedMaterial = _glowMat;

        
        Light l = CreateLight(post.transform, head.transform.position, lampColor, lampRange, "NightLight_Auto");
        _lamps.Add(new Lamp { light = l, baseIntensity = lampIntensity, createdByUs = true });
    }

    

    private IEnumerator DetectWindowsRoutine(Transform[] all)
    {
        var parentBounds = new Dictionary<Transform, Bounds>();
        var noParentBounds = new HashSet<Transform>();
        int processed = 0;

        foreach (Transform t in all)
        {
            if (++processed % 150 == 0)
            {
                yield return null;
                if (!_running || _root == null) yield break;
            }

            if (_windowObjectCount >= maxDetectedWindows || _windowLights.Count >= maxWindowLights) break;
            if (t == null || t.IsChildOf(_root.transform)) continue;
            if (!NameMatches(t.name, windowNameKeywords)) continue;
            if (NameMatches(t.name, lampNameKeywords)) continue;               
            if (HasMatchingDescendant(t, windowNameKeywords)) continue;         

            Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
            if (!TryGetBounds(renderers, out Bounds b)) continue;
            if (b.size.x > maxWindowObjectSize || b.size.y > maxWindowObjectSize || b.size.z > maxWindowObjectSize) continue;

            
            Vector3 outward = GetOutward(t, b, parentBounds, noParentBounds);

            bool alongX = b.size.x >= b.size.z;
            float length = alongX ? b.size.x : b.size.z;
            int cols = Mathf.Clamp(Mathf.RoundToInt(length / Mathf.Max(0.5f, windowSpacing)), 1, maxLightsPerWindowObject);
            int rows = Mathf.Clamp(Mathf.RoundToInt(b.size.y / Mathf.Max(0.5f, floorHeight)), 1, maxLightsPerWindowObject);
            if (cols * rows > maxLightsPerWindowObject) rows = Mathf.Max(1, maxLightsPerWindowObject / cols);

            int created = 0;
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    if (_windowLights.Count >= maxWindowLights) break;
                    if (Random.value > windowLitChance) continue;   

                    float px = alongX ? b.min.x + (c + 0.5f) / cols * b.size.x : b.center.x;
                    float pz = alongX ? b.center.z : b.min.z + (c + 0.5f) / cols * b.size.z;
                    float py = b.min.y + (r + 0.5f) / rows * b.size.y;
                    Vector3 pos = new Vector3(px, py, pz) + outward * windowLightOffset;

                    Color col = Color.Lerp(windowLightColor, new Color(1f, 0.9f, 0.7f), Random.value * 0.5f);
                    Light l = CreateLight(t, pos, col, windowLightRange, "WindowLight_Auto");

                    _windowLights.Add(new Lamp
                    {
                        light = l,
                        baseIntensity = windowLightIntensity * Random.Range(0.8f, 1.2f),
                        createdByUs = true,
                        delay = Random.Range(0f, 0.7f)
                    });
                    created++;
                }
            }

            if (created == 0) continue;
            _windowObjectCount++;

            
            if (glowWindowMaterials)
            {
                float glowDelay = Random.Range(0f, 0.7f);
                foreach (Renderer r in renderers)
                    if (r is MeshRenderer) TryAddGlow(r, glowDelay);
            }
        }
    }

    /// Direction pointing away from the building (so the light sits outside the wall, where it can light things).
    private Vector3 GetOutward(Transform window, Bounds windowBounds, Dictionary<Transform, Bounds> cache, HashSet<Transform> none)
    {
        Vector3 dir = window.forward;
        Transform parent = window.parent;

        if (parent != null && !none.Contains(parent))
        {
            if (!cache.TryGetValue(parent, out Bounds pb))
            {
                if (TryGetBounds(parent.GetComponentsInChildren<Renderer>(), out pb) && pb.size.x < 80f && pb.size.z < 80f)
                    cache[parent] = pb;
                else
                    none.Add(parent);
            }

            if (cache.ContainsKey(parent))
            {
                Vector3 d = windowBounds.center - cache[parent].center;
                d.y = 0f;
                if (d.sqrMagnitude > 0.01f) dir = d;
            }
        }

        dir.y = 0f;
        return dir.sqrMagnitude < 0.0001f ? Vector3.forward : dir.normalized;
    }

    private void TryAddGlow(Renderer r, float delay)
    {
        Material[] mats = r.sharedMaterials;
        var named = new List<int>();
        var any = new List<int>();

        for (int i = 0; i < mats.Length; i++)
        {
            Material m = mats[i];
            if (m == null || !m.HasProperty("_EmissionColor")) continue;

            any.Add(i);
            string n = m.name;
            if (n.IndexOf("window", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("glass", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("pane", System.StringComparison.OrdinalIgnoreCase) >= 0)
                named.Add(i);
        }

        List<int> use = named.Count > 0 ? named : null;
        if (use == null)
        {
            Vector3 s = r.bounds.size;
            if (Mathf.Max(s.x, Mathf.Max(s.y, s.z)) <= glowMaxRendererSize) use = any;
        }
        if (use == null || use.Count == 0) return;

        _glows.Add(new WindowGlow { renderer = r, original = mats, indices = use, delay = delay });
    }

    private void SetGlow(WindowGlow g, float level)
    {
        if (g.renderer == null) { ResetGlow(g); return; }

        if (level <= 0.001f)
        {
            if (g.instances != null) ResetGlow(g);
            return;
        }

        if (g.instances == null) g.instances = g.renderer.materials;   
        if (Mathf.Abs(level - g.applied) < 0.01f) return;
        g.applied = level;

        Color emission = windowLightColor * (windowEmissionIntensity * level);
        foreach (int idx in g.indices)
        {
            if (idx >= g.instances.Length || g.instances[idx] == null) continue;
            g.instances[idx].EnableKeyword("_EMISSION");
            g.instances[idx].SetColor("_EmissionColor", emission);
        }
    }

    private void ResetGlow(WindowGlow g)
    {
        if (g.instances == null) { g.applied = -1f; return; }

        if (g.renderer != null) g.renderer.sharedMaterials = g.original;
        foreach (Material m in g.instances)
            if (m != null) Destroy(m);

        g.instances = null;
        g.applied = -1f;
    }

    

    private static int CompareLampDistance(Lamp a, Lamp b) { return a.sqrDist.CompareTo(b.sqrDist); }

    private void RefreshBudget(List<Lamp> list, int maxActive, float maxDistance, Vector3 from)
    {
        float maxSqr = maxDistance * maxDistance;

        _sortBuffer.Clear();
        foreach (Lamp lamp in list)
        {
            lamp.allowed = false;
            if (lamp.light == null) continue;

            lamp.sqrDist = (lamp.light.transform.position - from).sqrMagnitude;
            if (lamp.sqrDist <= maxSqr) _sortBuffer.Add(lamp);
        }

        _sortBuffer.Sort(CompareLampDistance);

        int n = Mathf.Min(maxActive, _sortBuffer.Count);
        for (int i = 0; i < n; i++) _sortBuffer[i].allowed = true;
    }

    private void StepLamps(List<Lamp> list, float factor)
    {
        float step = Time.deltaTime * lampFadeSpeed;

        for (int i = 0; i < list.Count; i++)
        {
            Lamp lamp = list[i];
            if (lamp.light == null) continue;

            
            float f = lamp.delay > 0f ? Smooth(Mathf.InverseLerp(lamp.delay, lamp.delay + 0.25f, factor)) : factor;

            float target = lamp.allowed ? f : 0f;
            lamp.level = Mathf.MoveTowards(lamp.level, target, step);

            bool on = lamp.level > 0.002f;
            if (lamp.light.enabled != on) lamp.light.enabled = on;
            if (on) lamp.light.intensity = lamp.baseIntensity * lamp.level;
        }
    }

    private void UpdateAllLights(float lampFactor)
    {
        if (!_lampsReady) return;

        if (Time.unscaledTime >= _nextLampRefresh)
        {
            _nextLampRefresh = Time.unscaledTime + lampRefreshInterval;

            Camera cam = GetCamera();
            Vector3 p = cam != null ? cam.transform.position : transform.position;

            RefreshBudget(_lamps, maxActiveLampLights, lampActivationDistance, p);
            RefreshBudget(_windowLights, maxActiveWindowLights, windowActivationDistance, p);
        }

        StepLamps(_lamps, lampFactor);
        StepLamps(_windowLights, lampFactor);

        for (int i = 0; i < _glows.Count; i++)
        {
            WindowGlow g = _glows[i];
            float f = Smooth(Mathf.InverseLerp(g.delay, g.delay + 0.25f, lampFactor));
            SetGlow(g, f);
        }
    }
}