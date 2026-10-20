using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;



public class TimeMachinePortal : MonoBehaviour
{
    [Tooltip("Which level this specific time machine leads to. Just a label for now - " +
             "e.g. \"VennaAcademy\", \"AppleForest\" or \"WrongBeer\"")]
    public string destinationLevel;

    [Header("Optional")]
    [Tooltip("If you're using TextMeshPro world-space text above the portal, drop it here " +
             "and it'll auto-fill with destinationLevel on start (one less place to edit).")]
    [SerializeField] TMPro.TextMeshPro destinationLabel;

    bool hasTriggered = false;

    void Awake()
    {
        if (destinationLabel != null)
            destinationLabel.text = destinationLevel;

        
        TimeMachineTransition.Warmup();
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[TimeMachinePortal] OnTriggerEnter on \"{gameObject.name}\" by \"{other.name}\" " +
                  $"(tag={other.tag}, layer={LayerMask.LayerToName(other.gameObject.layer)}, hasTriggered={hasTriggered})");

        if (hasTriggered || TimeMachineTransition.IsBusy) return;

        if (!IsValidEntrant(other, out var label))
        {
            Debug.Log($"[TimeMachinePortal] \"{other.name}\" did not match car or player checks - ignoring.");
            return;
        }

        if (string.IsNullOrEmpty(destinationLevel))
        {
            Debug.LogWarning($"[TimeMachinePortal] \"{gameObject.name}\" has no destinationLevel set - not loading anything.");
            return;
        }

        Collider own = GetComponent<Collider>();
        Vector3 center = own != null ? own.bounds.center : transform.position;
        float radius = own != null ? own.bounds.extents.magnitude : 2f;

        Debug.Log($"[TimeMachinePortal] {label} entered \"{gameObject.name}\" \u2192 loading: {destinationLevel}");

        
        if (TimeMachineTransition.Begin(destinationLevel, center, radius, other.attachedRigidbody))
            hasTriggered = true;
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"[TimeMachinePortal] OnTriggerExit on \"{gameObject.name}\" by \"{other.name}\"");

        if (IsValidEntrant(other, out _))
            hasTriggered = false;
    }

    
    bool IsValidEntrant(Collider other, out string label)
    {
        if (other.GetComponentInParent<CarController>() != null)
        {
            label = "car";
            return true;
        }

        
        if (other.CompareTag("Player"))
        {
            label = "player";
            return true;
        }

        label = null;
        return false;
    }
}



public class TimeMachineTransition : MonoBehaviour
{
    // ------------------------------ tweaks ------------------------------
    public const float ChargeTime = 1.7f;      // seconds of build-up before the flash
    const float FlashTime = 0.18f;             // how fast it goes to full white
    const float ArriveFadeTime = 1.0f;         // fade-in on the other side
    const float SlowMoScale = 0.35f;           // time scale at the peak (1 = no slow-mo)
    const float FovKick = 55f;                 // extra camera FOV at the peak (warp feel)
    const float ShakeAmount = 0.12f;           // camera shake in world units at the peak
    const float EntrantDamping = 2.2f;         // how hard the car/player rigidbody is slowed
    const float EntrantPull = 4f;              // gentle pull towards the portal centre
    static readonly Color Tint = new Color(0.35f, 0.85f, 1f);
    // --------------------------------------------------------------------------

    static TimeMachineTransition instance;
    static Material particleMat;
    static bool warnedNoShader;

    public static bool IsBusy => instance != null && instance.busy;

    bool busy;
    Canvas canvas;
    Image vignette;
    Image flash;
    RectTransform vignetteRT;
    AudioSource audioSrc;

    // ------------------------------ public API ------------------------------

    public static void Warmup()
    {
        Get();
        TimeMachineSfx.Charge();
        TimeMachineSfx.Arrive();
        GetParticleMaterial();
    }

    public static bool Begin(string sceneName, Vector3 center, float radius, Rigidbody entrant)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[TimeMachine] Scene \"{sceneName}\" can't be loaded. " +
                           "Is it added in File > Build Settings > Scenes In Build, and spelled exactly right?");
            return false;
        }

        var t = Get();
        if (t.busy) return false;

        t.busy = true;
        t.StartCoroutine(t.Run(sceneName, center, Mathf.Clamp(radius, 1f, 8f), entrant));
        return true;
    }

    // ------------------------------ setup ------------------------------

    static TimeMachineTransition Get()
    {
        if (instance == null)
        {
            var go = new GameObject("[TimeMachineTransition]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<TimeMachineTransition>();
            instance.Build();
        }
        return instance;
    }

    void Build()
    {
        var cgo = new GameObject("Overlay");
        cgo.transform.SetParent(transform, false);
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;   // above everything

        vignette = MakeImage("Vignette", cgo.transform, MakeVignetteSprite());
        vignetteRT = vignette.rectTransform;
        vignetteRT.anchorMin = vignetteRT.anchorMax = new Vector2(0.5f, 0.5f);
        vignetteRT.sizeDelta = Vector2.zero;

        flash = MakeImage("Flash", cgo.transform, null);
        var frt = flash.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = frt.offsetMax = Vector2.zero;

        canvas.enabled = false;

        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;   // 2D - always heard at full volume
        audioSrc.volume = 1f;
    }

    static Image MakeImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        img.color = Color.clear;
        return img;
    }

   
    static Sprite MakeVignetteSprite()
    {
        const int N = 128;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontUnloadUnusedAsset
        };
        var px = new Color32[N * N];
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                float dx = (x + 0.5f) / N - 0.5f;
                float dy = (y + 0.5f) / N - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float u = Mathf.InverseLerp(0.1f, 0.5f, d);
                float a = u * u * (3f - 2f * u);
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
        sp.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return sp;
    }

    // ------------------------------ the sequence ------------------------------

    IEnumerator Run(string scene, Vector3 center, float radius, Rigidbody rb)
    {
        float baseScale = Time.timeScale;
        float baseFixed = Time.fixedDeltaTime;
        bool slowMo = baseScale > 0.01f;    

        canvas.enabled = true;
        float big = Mathf.Max(Screen.width, Screen.height) * 2.5f;
        vignetteRT.sizeDelta = new Vector2(big, big);
        SetOverlay(0f, 1f, 0f, 0f);

        GameObject fxRoot = BuildPortalFx(center, radius, out Light glow);
        TimeMachineCamFx camFx = AttachCamFx(Camera.main);

        audioSrc.PlayOneShot(TimeMachineSfx.Charge());

        
        AsyncOperation op = SceneManager.LoadSceneAsync(scene);
        op.allowSceneActivation = false;

        // ---------- 1) charge-up ----------
        float t = 0f;
        while (t < ChargeTime)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            float k = Mathf.Clamp01(t / ChargeTime);
            float e = k * k;

            if (slowMo) SetTimeScale(Mathf.Lerp(baseScale, baseScale * SlowMoScale, Smooth(k)), baseScale, baseFixed);

            SetOverlay(Mathf.Lerp(0f, 0.95f, Mathf.Clamp01(k * 1.6f)),
                       Mathf.Lerp(1f, 0.6f, e),
                       e,
                       Mathf.Clamp01((k - 0.7f) / 0.3f) * 0.35f);

            if (camFx != null) { camFx.fovAdd = FovKick * e; camFx.shake = ShakeAmount * e; }

            if (glow != null)
                glow.intensity = Mathf.Lerp(0f, 10f, e) * (0.8f + 0.4f * Mathf.PerlinNoise(Time.unscaledTime * 25f, 0f));

            DampEntrant(rb, center, dt);
            yield return null;
        }

        
        t = 0f;
        while (t < FlashTime)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            float f = Mathf.Clamp01(t / FlashTime);

            SetOverlay(0.95f, 0.6f, 1f, Mathf.Lerp(0.35f, 1f, f));
            if (camFx != null) { camFx.fovAdd = FovKick * (1f + 0.6f * f); camFx.shake = ShakeAmount * (1f + f); }
            DampEntrant(rb, center, dt);
            yield return null;
        }
        SetOverlay(1f, 0.6f, 1f, 1f);

        
        while (op.progress < 0.9f)
        {
            DampEntrant(rb, center, Time.unscaledDeltaTime);
            yield return null;
        }
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        if (camFx != null) camFx.Release();
        if (fxRoot != null) Destroy(fxRoot);

        
        yield return null;
        yield return null;

        // ---------- 4) arrival ----------
        audioSrc.PlayOneShot(TimeMachineSfx.Arrive());
        TimeMachineCamFx camFx2 = AttachCamFx(Camera.main);

        t = 0f;
        while (t < ArriveFadeTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / ArriveFadeTime);
            float inv = 1f - k;

            if (slowMo) SetTimeScale(Mathf.Lerp(baseScale * SlowMoScale, baseScale, Smooth(k)), baseScale, baseFixed);

            SetOverlay(inv * 0.9f, Mathf.Lerp(0.6f, 1f, k), 1f, Mathf.Clamp01(1f - k * 1.8f));
            if (camFx2 != null) { camFx2.fovAdd = FovKick * 0.8f * inv * inv; camFx2.shake = ShakeAmount * inv; }
            yield return null;
        }

        // ---------- cleanup ----------
        if (camFx2 != null) camFx2.Release();
        if (slowMo) SetTimeScale(baseScale, baseScale, baseFixed);
        SetOverlay(0f, 1f, 0f, 0f);
        canvas.enabled = false;
        busy = false;
    }

    // ------------------------------ helpers ------------------------------

    static float Smooth(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    static void SetTimeScale(float scale, float baseScale, float baseFixed)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = baseFixed * (scale / baseScale);
    }

    void SetOverlay(float vigAlpha, float vigScale, float vigGlow, float flashAlpha)
    {
        Color c = Color.Lerp(new Color(0.02f, 0.12f, 0.25f), Color.white, vigGlow);
        c.a = vigAlpha;
        vignette.color = c;
        vignetteRT.localScale = Vector3.one * vigScale;

        Color f = Color.Lerp(Tint, Color.white, 0.85f);
        f.a = flashAlpha;
        flash.color = f;
    }

    
    static void DampEntrant(Rigidbody rb, Vector3 center, float dt)
    {
        if (rb == null || rb.isKinematic) return;

#if UNITY_6000_0_OR_NEWER
        Vector3 v = rb.linearVelocity;
#else
        Vector3 v = rb.velocity;
#endif
        v *= Mathf.Exp(-EntrantDamping * dt);

        Vector3 to = center - rb.worldCenterOfMass;
        to.y = 0f;
        if (to.sqrMagnitude > 0.01f) v += to.normalized * EntrantPull * dt;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
    }

    // ------------------------------ camera fx ------------------------------

    static TimeMachineCamFx AttachCamFx(Camera cam)
    {
        if (cam == null) return null;
        var fx = new TimeMachineCamFx { cam = cam };
        fx.early = cam.gameObject.AddComponent<TimeMachineCamFxEarly>();
        fx.early.fx = fx;
        fx.late = cam.gameObject.AddComponent<TimeMachineCamFxLate>();
        fx.late.fx = fx;
        return fx;
    }

    // ------------------------------ particles + light ------------------------------

    static GameObject BuildPortalFx(Vector3 center, float radius, out Light glow)
    {
        var root = new GameObject("[TimeMachineFx]");
        root.transform.position = center;

        Material mat = GetParticleMaterial();
        if (mat != null)
        {
            MakeVortex(root.transform, radius, mat);
            MakeCore(root.transform, radius, mat);
        }

        var lgo = new GameObject("Glow");
        lgo.transform.SetParent(root.transform, false);
        glow = lgo.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = Tint;
        glow.range = radius * 8f;
        glow.intensity = 0f;
        glow.shadows = LightShadows.None;

        return root;
    }

    
    static void MakeVortex(Transform parent, float radius, Material mat)
    {
        var go = new GameObject("Vortex");
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        const float life = 0.9f;
        float shellR = radius * 1.7f;

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = ChargeTime + FlashTime;
        main.startLifetime = life;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(radius * 0.05f, radius * 0.14f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.3f, 0.8f, 1f), new Color(0.8f, 0.5f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 1500;

        var em = ps.emission;
        em.rateOverTime = new ParticleSystem.MinMaxCurve(500f, AnimationCurve.EaseInOut(0f, 0.15f, 1f, 1f));

        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = shellR;
        sh.radiusThickness = 0f;    // surface only

        
        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.x = new ParticleSystem.MinMaxCurve(0f);
        vol.y = new ParticleSystem.MinMaxCurve(0f);
        vol.z = new ParticleSystem.MinMaxCurve(0f);
        vol.orbitalX = new ParticleSystem.MinMaxCurve(0f);
        vol.orbitalY = new ParticleSystem.MinMaxCurve(4f);
        vol.orbitalZ = new ParticleSystem.MinMaxCurve(0f);
        vol.radial = new ParticleSystem.MinMaxCurve(-shellR / life);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.2f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0.1f)));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f),
                    new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.1f;
        r.lengthScale = 2.5f;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        ps.Play();
    }

    
    static void MakeCore(Transform parent, float radius, Material mat)
    {
        var go = new GameObject("Core");
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float total = ChargeTime + FlashTime;

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = total;
        main.startLifetime = total;
        main.startSpeed = 0f;
        main.startSize = radius * 0.6f;
        main.startColor = new Color(0.6f, 0.9f, 1f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 1;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var sh = ps.shape;
        sh.enabled = false;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.1f, 1f, 4f));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 1f) });
        col.color = g;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        ps.Play();
    }

    
    static Material GetParticleMaterial()
    {
        if (particleMat != null) return particleMat;

        Shader sh = null;
        string[] names = { "Legacy Shaders/Particles/Additive", "Mobile/Particles/Additive", "Sprites/Default" };
        for (int i = 0; i < names.Length; i++)
        {
            Shader cand = Shader.Find(names[i]);
            if (cand != null && cand.isSupported) { sh = cand; break; }
        }

        if (sh == null)
        {
            if (!warnedNoShader)
            {
                warnedNoShader = true;
                Debug.LogWarning("[TimeMachine] No usable particle shader found - skipping particles.");
            }
            return null;
        }

        particleMat = new Material(sh)
        {
            mainTexture = MakeSoftTexture(),
            hideFlags = HideFlags.DontUnloadUnusedAsset
        };
        return particleMat;
    }

    static Texture2D MakeSoftTexture()
    {
        const int N = 64;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontUnloadUnusedAsset
        };
        var px = new Color32[N * N];
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                float dx = (x + 0.5f) / N - 0.5f;
                float dy = (y + 0.5f) / N - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 0.5f;
                float a = Mathf.Clamp01(1f - d);
                a *= a;
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }
}



public class TimeMachineCamFx
{
    public Camera cam;
    public float fovAdd;
    public float shake;
    public TimeMachineCamFxEarly early;
    public TimeMachineCamFxLate late;

    float appliedFov;
    Vector3 appliedOffset;

    public void Undo()
    {
        if (cam == null) return;
        cam.fieldOfView -= appliedFov;
        cam.transform.position -= appliedOffset;
        appliedFov = 0f;
        appliedOffset = Vector3.zero;
    }

    public void Apply()
    {
        if (cam == null) return;
        appliedFov = fovAdd;
        appliedOffset = Random.insideUnitSphere * shake;
        cam.fieldOfView += appliedFov;
        cam.transform.position += appliedOffset;
    }

    public void Release()
    {
        Undo();
        if (early != null) Object.Destroy(early);
        if (late != null) Object.Destroy(late);
    }
}

[DefaultExecutionOrder(-32000)]
public class TimeMachineCamFxEarly : MonoBehaviour
{
    public TimeMachineCamFx fx;
    void Update() { if (fx != null) fx.Undo(); }
}

[DefaultExecutionOrder(32000)]
public class TimeMachineCamFxLate : MonoBehaviour
{
    public TimeMachineCamFx fx;
    void LateUpdate() { if (fx != null) fx.Apply(); }
}



public static class TimeMachineSfx
{
    const int SR = 44100;
    const double Tau = System.Math.PI * 2.0;

    static AudioClip charge, arrive;

    public static AudioClip Charge()
    {
        if (charge == null) charge = BuildCharge();
        return charge;
    }

    public static AudioClip Arrive()
    {
        if (arrive == null) arrive = BuildArrive();
        return arrive;
    }

   
    static AudioClip BuildCharge()
    {
        float ct = TimeMachineTransition.ChargeTime;
        const float tail = 1.6f;
        int n = (int)((ct + tail) * SR);
        var d = new float[n];
        var rng = new System.Random(7);

        double ph1 = 0, ph2 = 0, phBoom = 0;
        float lp = 0f, lp2 = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float tb = t - ct;                                  // time since the boom
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float s = 0f;

            if (tb < 0.15f)
            {
                float k = Mathf.Clamp01(t / ct);
                double f = 60.0 * System.Math.Pow(24.0, k);     // ~60 Hz -> ~1.4 kHz
                ph1 += Tau * f / SR;
                ph2 += Tau * f * 1.007 / SR;

                float tone = (float)(System.Math.Sin(ph1) + 0.5 * System.Math.Sin(ph1 * 2.0) + System.Math.Sin(ph2)) * 0.33f;
                float wob = 1f + 0.35f * Mathf.Sin(6.2831853f * (5f + 18f * k) * t);
                float env = Mathf.Pow(k, 1.6f) * (tb > 0f ? Mathf.Exp(-tb * 30f) : 1f);

                lp += (noise - lp) * Mathf.Lerp(0.015f, 0.45f, k);   // wind that gets brighter
                s += (tone * wob * 0.55f + lp * 0.9f) * env;
            }

            if (tb >= 0f)
            {
                double fb = 30.0 + 90.0 * System.Math.Exp(-tb * 7.0);  // pitch-dropping sub boom
                phBoom += Tau * fb / SR;
                lp2 += (noise - lp2) * 0.05f;

                s += (float)System.Math.Sin(phBoom) * Mathf.Exp(-tb * 2.2f) * 0.95f;
                s += noise * Mathf.Exp(-tb * 14f) * 0.45f;              // crack
                s += lp2 * Mathf.Exp(-tb * 3f) * 2.2f;                  // rumble

                double chime = System.Math.Sin(Tau * 880.0 * tb)
                             + 0.7 * System.Math.Sin(Tau * 1320.0 * tb)
                             + 0.5 * System.Math.Sin(Tau * 1760.0 * tb);
                s += (float)chime * Mathf.Exp(-tb * 3.5f) * 0.07f;      // sparkle
            }

            float fade = Mathf.Clamp01((n - i) / (0.05f * SR));         // 50 ms tail fade, no click
            d[i] = (float)System.Math.Tanh(s * 1.1f) * fade;            // soft clip
        }

        return MakeClip("TimeMachine_Charge", d);
    }

    
    static AudioClip BuildArrive()
    {
        const float len = 1.8f;
        int n = (int)(len * SR);
        var d = new float[n];
        var rng = new System.Random(11);

        double ph = 0;
        float lp = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);

            lp += (noise - lp) * Mathf.Lerp(0.5f, 0.02f, Mathf.Clamp01(t / 1.2f));
            float s = lp * Mathf.Exp(-t * 2.2f) * 1.4f;                 // wind falling away

            double f = 1500.0 * System.Math.Pow(0.06, Mathf.Clamp01(t));  // 1.5 kHz -> ~90 Hz
            ph += Tau * f / SR;
            s += (float)System.Math.Sin(ph) * Mathf.Exp(-t * 3.2f) * 0.4f;

            s += (float)System.Math.Sin(Tau * 48.0 * t) * Mathf.Exp(-t * 9f) * 0.8f;   // thump

            double chime = System.Math.Sin(Tau * 1320.0 * t) + 0.6 * System.Math.Sin(Tau * 1980.0 * t);
            s += (float)chime * Mathf.Exp(-t * 4.5f) * 0.07f;

            float attack = Mathf.Clamp01(t / 0.005f);
            float fade = Mathf.Clamp01((n - i) / (0.05f * SR));
            d[i] = (float)System.Math.Tanh(s * 1.1f) * attack * fade;
        }

        return MakeClip("TimeMachine_Arrive", d);
    }

    static AudioClip MakeClip(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, SR, false);
        clip.SetData(data, 0);
        clip.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return clip;
    }
}