using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;



public static class ExamUtil
{
    static Material baseMat;
    static Font font;
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static readonly RaycastHit[] hitBuf = new RaycastHit[32];
    static bool warnedFallback;

    
    public static Material Mat(Color c, float metallic = 0f, float smooth = 0.3f)
    {
        string key = ColorUtility.ToHtmlStringRGBA(c) + "_" + metallic.ToString("0.0") + "_" + smooth.ToString("0.0");
        Material m;
        if (mats.TryGetValue(key, out m) && m != null) return m;

        if (baseMat == null)
        {
            
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMat = tmp.GetComponent<Renderer>().sharedMaterial;
            Object.DestroyImmediate(tmp);
        }

        m = new Material(baseMat);
        m.name = "Exam_" + key;
        m.hideFlags = HideFlags.HideAndDontSave;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        mats[key] = m;
        return m;
    }

    
    public static Material Glow(Color c, float alpha = 0.3f)
    {
        Shader s = Shader.Find("Sprites/Default");
        Material m = s != null ? new Material(s) : new Material(Mat(c));
        m.color = new Color(c.r, c.g, c.b, alpha);
        return m;
    }

    public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale,
                                  Color color, Vector3 euler, bool keepCollider, float metallic = 0f, float smooth = 0.3f)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;

        if (!keepCollider)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        Transform t = go.transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(euler);
        t.localScale = scale;

        go.GetComponent<Renderer>().sharedMaterial = Mat(color, metallic, smooth);
        return go;
    }

    public static Font UIFont
    {
        get
        {
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Helvetica", "Liberation Sans" }, 16);
            return font;
        }
    }

    
    public static bool FindNavSpot(Vector3 center, float minR, float maxR, float refY, out Vector3 result)
    {
        for (int i = 0; i < 40; i++)
        {
            float ang = Random.value * Mathf.PI * 2f;
            float r = Random.Range(minR, maxR);
            Vector3 p = center + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * r;
            p.y = refY;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(p, out hit, 5f, NavMesh.AllAreas))
            {
                if (Mathf.Abs(hit.position.y - refY) < 5f)
                {
                    result = hit.position;
                    return true;
                }
            }
        }

        result = Vector3.zero;
        return false;
    }

    
    public static bool Ground(Vector3 p, float refY, out Vector3 ground)
    {
        ground = p;
        Vector3 origin = new Vector3(p.x, refY + 25f, p.z);
        int n = Physics.RaycastNonAlloc(origin, Vector3.down, hitBuf, 60f, ~0, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        bool found = false;
        for (int i = 0; i < n; i++)
        {
            RaycastHit h = hitBuf[i];
            if (h.normal.y < 0.6f) continue;
            float d = Mathf.Abs(h.point.y - refY);
            if (d < best && d < 5f)
            {
                best = d;
                ground = h.point;
                found = true;
            }
        }
        return found;
    }

    public static Vector3 FindSpot(Vector3 center, float minR, float maxR, float clear, float refY)
    {
        return FindSpotInternal(center, minR, maxR, clear, refY, false, Vector3.zero);
    }

    
    public static Vector3 FindSpotLos(Vector3 center, float minR, float maxR, float clear, float refY, Vector3 losFrom)
    {
        return FindSpotInternal(center, minR, maxR, clear, refY, true, losFrom);
    }

    static Vector3 FindSpotInternal(Vector3 center, float minR, float maxR, float clear, float refY, bool los, Vector3 losFrom)
    {
        for (int i = 0; i < 70; i++)
        {
            float ang = Random.value * Mathf.PI * 2f;
            float r = Random.Range(minR, maxR);
            Vector3 p = center + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * r;

            Vector3 g;
            if (!Ground(p, refY, out g)) continue;

            
            if (Physics.CheckBox(g + Vector3.up * 1.25f, new Vector3(clear, 0.9f, clear),
                                 Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) continue;

            if (los && Physics.Linecast(losFrom + Vector3.up * 1.2f, g + Vector3.up * 1.2f, ~0, QueryTriggerInteraction.Ignore)) continue;

            return g;
        }

        if (!warnedFallback)
        {
            warnedFallback = true;
            Debug.LogWarning("[ArtExam] Couldn't find a perfectly clear spot for a station - placing it at the nearest ground. "
                             + "If something looks buried, the level is very tight around the spawn point.");
        }

        Vector3 fb = center + Vector3.forward * minR;
        Vector3 gg;
        if (Ground(fb, refY, out gg)) return gg;
        return new Vector3(fb.x, refY, fb.z);
    }

    // ------------------------------------------------------------ misc
    public static string Compass(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return "nearby";
        float a = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        if (a < 0f) a += 360f;
        string[] names = { "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west" };
        return names[Mathf.RoundToInt(a / 45f) % 8];
    }

    public static string WeaponTag(WeaponType t)
    {
        switch (t)
        {
            case WeaponType.Rifle: return "RIFLE 2";
            case WeaponType.Bazooka: return "BAZOOKA 3";
            case WeaponType.Stone: return "STONE 4";
        }
        return "BAT 1";
    }

    public static string WeaponName(WeaponType t)
    {
        switch (t)
        {
            case WeaponType.Rifle: return "RIFLE (key 2)";
            case WeaponType.Bazooka: return "BAZOOKA (key 3)";
            case WeaponType.Stone: return "thrown STONE (key 4)";
        }
        return "BAT (key 1)";
    }

    public static Color WeaponColor(WeaponType t)
    {
        switch (t)
        {
            case WeaponType.Rifle: return new Color(0.35f, 0.72f, 1f);
            case WeaponType.Bazooka: return new Color(1f, 0.35f, 0.28f);
            case WeaponType.Stone: return new Color(1f, 0.9f, 0.2f);
        }
        return new Color(0.75f, 0.45f, 1f);
    }
}

// ===========================================================================
//  Procedural sound effects (no audio files needed)
// ===========================================================================
public static class ExamAudio
{
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    static AudioClip Make(string key, float seconds, System.Func<float, float> f)
    {
        AudioClip c;
        if (cache.TryGetValue(key, out c) && c != null) return c;

        int rate = 22050;
        int n = Mathf.Max(1, (int)(seconds * rate));
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
            data[i] = Mathf.Clamp(f(i / (float)rate), -1f, 1f);

        c = AudioClip.Create("Exam_" + key, n, 1, rate, false);
        c.SetData(data, 0);
        cache[key] = c;
        return c;
    }

    static float Noise() { return Random.value * 2f - 1f; }

    public static AudioClip Chip()
    {
        return Make("chip", 0.15f, t => Noise() * Mathf.Exp(-t * 45f) * 0.7f
                                       + Mathf.Sin(6.2832f * 1900f * t) * Mathf.Exp(-t * 70f) * 0.3f);
    }

    public static AudioClip Splat()
    {
        return Make("splat", 0.25f, t => Noise() * Mathf.Exp(-t * 16f) * 0.6f
                                        + Mathf.Sin(6.2832f * (260f - 500f * t) * t) * Mathf.Exp(-t * 14f) * 0.4f);
    }

    public static AudioClip Boom()
    {
        return Make("boom", 1.2f, t => (Noise() * 0.55f + Mathf.Sin(6.2832f * 55f * t) * 0.8f) * Mathf.Exp(-t * 3.2f));
    }

    public static AudioClip Gong()
    {
        return Make("gong", 3.2f, t => (Mathf.Sin(6.2832f * 180f * t)
                                       + 0.6f * Mathf.Sin(6.2832f * 271f * t)
                                       + 0.4f * Mathf.Sin(6.2832f * 398f * t)
                                       + (t < 0.03f ? Noise() * 0.8f : 0f)) * 0.33f * Mathf.Exp(-t * 1.3f));
    }

    public static AudioClip Thud()
    {
        return Make("thud", 0.2f, t => Mathf.Sin(6.2832f * 110f * t) * Mathf.Exp(-t * 20f) * 0.6f);
    }

    public static AudioClip Pop()
    {
        return Make("pop", 0.2f, t => Mathf.Sin(6.2832f * (500f + 2500f * t) * t) * Mathf.Exp(-t * 14f) * 0.45f);
    }

    public static AudioClip Fanfare()
    {
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        return Make("fanfare", 1.3f, t =>
        {
            int k = Mathf.Min(3, (int)(t / 0.2f));
            float lt = t - k * 0.2f;
            float env = k == 3 ? Mathf.Exp(-lt * 2.5f) : Mathf.Exp(-lt * 6f);
            return (Mathf.Sin(6.2832f * notes[k] * t) + 0.4f * Mathf.Sin(12.566f * notes[k] * t)) * env * 0.3f;
        });
    }

    public static void Play(AudioClip clip, Vector3 pos, float volume)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, pos, volume);
    }
}

// ===========================================================================
//  World-space text (billboard) - used for station signs and floating pop-ups
// ===========================================================================
public class ExamLabel : MonoBehaviour
{
    Text text;
    Transform camT;
    float baseScale = 0.01f;
    float life = -1f;
    float age;
    bool rise;
    Color color = Color.white;

    public static ExamLabel Create(Transform parent, Vector3 localPos, string msg, Color color, int fontSize, float scale)
    {
        GameObject go = new GameObject("ExamLabel");
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        Canvas cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.WorldSpace;
        cv.sortingOrder = 40;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(900f, 220f);
        go.transform.localScale = Vector3.one * scale;

        GameObject tgo = new GameObject("Text", typeof(RectTransform));
        RectTransform trt = tgo.GetComponent<RectTransform>();
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        Text t = tgo.AddComponent<Text>();
        t.font = ExamUtil.UIFont;
        t.supportRichText = true;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontSize = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.color = color;
        t.text = msg;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        Outline o = tgo.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.9f);
        o.effectDistance = new Vector2(2f, -2f);

        ExamLabel l = go.AddComponent<ExamLabel>();
        l.text = t;
        l.baseScale = scale;
        l.color = color;
        return l;
    }

    
    public static void Pop(Vector3 worldPos, string msg, Color color)
    {
        ExamLabel l = Create(null, Vector3.zero, msg, color, 48, 0.012f);
        l.transform.position = worldPos;
        l.life = 1.6f;
        l.rise = true;
    }

    public void SetText(string s) { if (text != null) text.text = s; }

    public void SetColor(Color c)
    {
        color = c;
        if (text != null) text.color = c;
    }

    void Update()
    {
        if (life <= 0f) return;

        age += Time.deltaTime;
        if (rise) transform.position += Vector3.up * 0.9f * Time.deltaTime;

        float k = Mathf.Clamp01(age / life);
        if (text != null) text.color = new Color(color.r, color.g, color.b, 1f - k * k);
        if (age >= life) Destroy(gameObject);
    }

    void LateUpdate()
    {
        if (camT == null)
        {
            Camera c = Camera.main;
            if (c == null) return;
            camT = c.transform;
        }

        transform.rotation = camT.rotation;
        float d = Vector3.Distance(camT.position, transform.position);
        transform.localScale = Vector3.one * baseScale * Mathf.Clamp(d / 8f, 0.7f, 5f);
    }
}


public class ExamBeacon : MonoBehaviour
{
    
    public static bool ShowLabels = false;

    Material mat;
    Color color;
    ExamLabel label;

    public static ExamBeacon Create(Vector3 ground, Color c, string text)
    {
        GameObject root = new GameObject("ExamBeacon");
        root.transform.position = ground;

        GameObject pillar = ExamUtil.Prim(PrimitiveType.Cylinder, "Pillar", root.transform,
                                          new Vector3(0f, 9f, 0f), new Vector3(0.35f, 9f, 0.35f), c, Vector3.zero, false);
        ExamBeacon b = root.AddComponent<ExamBeacon>();
        b.color = c;
        b.mat = ExamUtil.Glow(c);

        Renderer r = pillar.GetComponent<Renderer>();
        r.sharedMaterial = b.mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        if (ShowLabels) b.label = ExamLabel.Create(root.transform, new Vector3(0f, 4.2f, 0f), text, c, 46, 0.012f);
        return b;
    }

    public void SetTarget(Vector3 ground, string text)
    {
        transform.position = ground;
        if (label != null) label.SetText(text);
        gameObject.SetActive(true);
    }

    public void Remove()
    {
        Destroy(gameObject);
    }

    void Update()
    {
        if (mat == null) return;

        float alpha = 0.3f;
        ArtistLevelPlayer p = ArtExamDirector.Player;
        if (p != null)
        {
            Vector3 d = p.transform.position - transform.position;
            d.y = 0f;
            alpha = Mathf.Clamp01((d.magnitude - 4f) / 10f) * 0.3f + 0.04f;   // fades when you're close
        }

        float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 3f);
        mat.color = new Color(color.r, color.g, color.b, alpha * pulse);
    }
}

// ===========================================================================
//  Particle-free effects: debris bursts + explosion / spark prefabs generated in code
// ===========================================================================
public class ExamFx : MonoBehaviour
{
    public enum Kind { Explosion, Spark }
    public Kind kind;

  
    public static GameObject MakeTemplate(Kind k)
    {
        GameObject go = new GameObject("ExamFxTemplate_" + k);
        go.transform.position = new Vector3(0f, -5000f, 0f);
        ExamFx fx = go.AddComponent<ExamFx>();
        fx.kind = k;
        return go;
    }

    void Start()
    {
        if (transform.position.y < -4000f) return;   

        Vector3 p = transform.position;
        if (kind == Kind.Explosion)
        {
            Burst(p, new Color(1f, 0.55f, 0.1f), 22, 9f, 0.28f, 1.2f);
            Burst(p, new Color(0.22f, 0.22f, 0.22f), 14, 5f, 0.4f, 2f);

            GameObject l = new GameObject("ExplosionLight");
            l.transform.position = p + Vector3.up;
            Light lt = l.AddComponent<Light>();
            lt.type = LightType.Point;
            lt.color = new Color(1f, 0.6f, 0.2f);
            lt.range = 18f;
            lt.intensity = 6f;
            Destroy(l, 0.25f);

            ExamAudio.Play(ExamAudio.Boom(), p, 1f);
        }
        else
        {
            Burst(p, new Color(1f, 0.9f, 0.4f), 5, 3f, 0.05f, 0.6f);
            ExamAudio.Play(ExamAudio.Chip(), p, 0.35f);
        }
    }

    public static void Burst(Vector3 pos, Color color, int count, float speed, float size, float life)
    {
        Collider playerCol = ArtExamDirector.PlayerCollider;

        for (int i = 0; i < count; i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Debris";

            float shade = 0.85f + 0.15f * Random.Range(0, 3);
            go.GetComponent<Renderer>().sharedMaterial =
                ExamUtil.Mat(new Color(color.r * shade, color.g * shade, color.b * shade, 1f));

            go.transform.position = pos + Random.insideUnitSphere * 0.15f;
            go.transform.rotation = Random.rotation;
            go.transform.localScale = Vector3.one * size * Random.Range(0.5f, 1.2f);

            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.05f;
            rb.AddForce(Random.onUnitSphere * speed * Random.Range(0.4f, 1f) + Vector3.up * speed * 0.4f, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 6f, ForceMode.VelocityChange);

            Collider c = go.GetComponent<Collider>();
            if (playerCol != null && c != null) Physics.IgnoreCollision(c, playerCol);

            Destroy(go, life * Random.Range(0.8f, 1.2f));
        }
    }
}



public class ExamMarker : MonoBehaviour
{
    Vector3 basePos;
    Transform shape;
    Transform camT;
    float phase;

    /// <param name="exclamation">true = yellow-style "!" sign, false = spinning diamond</param>
    public static ExamMarker Create(Transform parent, Vector3 localPos, Color color, bool exclamation)
    {
        GameObject go = new GameObject("ExamMarker");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        ExamMarker m = go.AddComponent<ExamMarker>();
        m.basePos = localPos;
        m.phase = Random.value * 6.28f;

        if (exclamation)
        {
            ExamLabel.Create(go.transform, Vector3.zero, "!", color, 110, 0.005f);
        }
        else
        {
            GameObject d = ExamUtil.Prim(PrimitiveType.Cube, "Diamond", go.transform, Vector3.zero,
                                         Vector3.one * 0.3f, color, new Vector3(45f, 0f, 45f), false);
            d.GetComponent<Renderer>().sharedMaterial = ExamUtil.Glow(color, 1f);
            m.shape = d.transform;
        }
        return m;
    }

    public void Remove()
    {
        Destroy(gameObject);
    }

    void Update()
    {
        transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * 2.5f + phase) * 0.12f;

        if (shape == null) return;

        shape.Rotate(0f, 150f * Time.deltaTime, 0f, Space.World);

        if (camT == null)
        {
            Camera c = Camera.main;
            if (c != null) camT = c.transform;
        }
        if (camT != null)
        {
            float d = Vector3.Distance(camT.position, transform.position);
            shape.localScale = Vector3.one * 0.3f * Mathf.Clamp(d / 10f, 1f, 3.5f);   
        }
    }
}

// ===========================================================================
//  "Press E" objects. ArtExamDirector shows the prompt and the player's thought.
// ===========================================================================
public class ExamInteractable : MonoBehaviour
{
    static readonly List<ExamInteractable> all = new List<ExamInteractable>();

    public float radius = 3f;
    public string verb = "Inspect";
    public Transform anchor;                      
    public System.Func<string> thought;           
    public System.Action onUse;                   

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    public static ExamInteractable Nearest(Vector3 pos)
    {
        ExamInteractable best = null;
        float bestK = float.MaxValue;

        for (int i = 0; i < all.Count; i++)
        {
            ExamInteractable it = all[i];
            if (it == null) continue;

            Vector3 a = it.anchor != null ? it.anchor.position : it.transform.position;
            Vector3 d = pos - a;
            d.y = 0f;

            float k = d.magnitude / it.radius;
            if (k <= 1f && k < bestK)
            {
                bestK = k;
                best = it;
            }
        }
        return best;
    }

    public string Use()
    {
        if (onUse != null) onUse();
        return thought != null ? thought() : null;
    }
}