using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[DisallowMultipleComponent]
public class LevelIntroSequence : MonoBehaviour
{
    [Header("Optional overrides - everything is found automatically")]
    public HitlerNPC hitler;
    public MainCameraController mainCamera;
    public PlayerScript player;
    [Tooltip("Legacy subtitle element. No longer needed - it is simply hidden.")]
    public TMP_Text introText;

    [Header("Pacing")]
    [Tooltip("Seconds per typed character.")]
    public float typeSpeed = 0.018f;
    [Tooltip("How long a caption stays after it finished typing.")]
    public float holdAfterTyping = 1.8f;
    public float titleHold = 3.2f;
    [Tooltip("If the next shot is further than this from the camera, cut through black instead of flying across the map.")]
    public float cutDistance = 14f;
    public KeyCode skipKey = KeyCode.Return;

    [Header("Camera")]
    [Tooltip("Higher = the camera glides to its position faster.")]
    public float cameraFollow = 2.4f;
    public float cameraTurn = 4f;

    [Header("Debug")]
    public bool debugLogging = true;

    [Header("Painting shot (optional)")]
    [Tooltip("Drag a painting (or an empty placed next to the paintings) here. Leave empty to auto-frame all paintings.")]
    public Transform paintingFocus;
    public float paintingShotDistance = 10f;
    public float paintingLookHeight = 1.2f;

    
    class Shot
    {
        public Transform focus;
        public float lookHeight = 1.4f;
        public float distance = 6f;
        public float pitch = 14f;
        public bool followFacing;           
        public float yawOffset;
        public float sweepFrom = 18f, sweepTo = -18f;   
        public float baseYaw;               

        public string chip, title, body, thought;
        public Color color = Color.white;

        public int hotbar = -2;             
        public bool showSpace, showControls;
        public bool callHitler;

        public string labelText;
        public Vector3 labelOffset = new Vector3(0f, 3.5f, 0f);
    }

    class KeyUI
    {
        public RectTransform root;
        public Image border;
        public Text label;
    }

    const float BarHeight = 120f;
    static readonly Color Gold = new Color(1f, 0.85f, 0.3f);

    // ------------------------------------------------------------------ state
    Camera cam;
    Transform playerT;
    ArtistLevelPlayer artistPlayer;

    bool hitlerStarted, skipAll, rigActive, snapNext, shotRunning;
    Shot currentShot;
    float shotTime, shotLen = 8f;

    readonly List<GameObject> temp = new List<GameObject>();
    ExamLabel tempLabel;
    Transform previewRival;
    readonly RaycastHit[] hits = new RaycastHit[24];

    // ------------------------------------------------------------------ UI
    Canvas canvas;
    Image fade, panelBg, accent, chipBg;
    RectTransform barTop, barBottom, spaceKeyRect;
    Text chipText, titleText, bodyText, skipHint, titleBig, titleSmall, titleTag, beginText;
    CanvasGroup panelGroup, hotbarGroup, controlsGroup, spaceGroup, titleGroup, beginGroup;
    readonly KeyUI[] slots = new KeyUI[4];
    readonly float[] slotAlpha = { 0.3f, 0.3f, 0.3f, 0.3f };
    readonly Color[] slotColor = new Color[4];

    float panelTarget, hotbarTarget, controlsTarget, spaceTarget, titleTarget, beginTarget;
    float barH, barTarget;
    bool hintVisible;
    int hotbarMode = -2;

    // ====================================================================== START
    void Start()
    {
        if (hitler == null) hitler = FindObjectOfType<HitlerNPC>();
        if (mainCamera == null) mainCamera = FindObjectOfType<MainCameraController>();
        ResolvePlayer();

        if (introText != null) introText.enabled = false;

        StartCoroutine(PlayIntro());
    }

    void ResolvePlayer()
    {
        artistPlayer = FindObjectOfType<ArtistLevelPlayer>();

        if (player == null && artistPlayer != null) player = artistPlayer.GetComponent<PlayerScript>();
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.GetComponent<PlayerScript>();
        }
        if (player == null) player = FindObjectOfType<PlayerScript>();

        if (artistPlayer != null) playerT = artistPlayer.transform;
        else if (player != null) playerT = player.transform;
    }

    void StartHitler()
    {
        if (hitlerStarted) return;
        hitlerStarted = true;
        if (hitler != null) hitler.BeginWalkingToEasel();
    }

    void LockPlayer(bool control)
    {
        if (control)
        {
            if (artistPlayer != null) artistPlayer.enabled = true;
            if (player != null) player.SetControl(true);
        }
        else
        {
            if (player != null) player.SetControl(false);
            if (artistPlayer != null) artistPlayer.enabled = false;   
        }
    }

    void Log(string msg)
    {
        if (debugLogging) Debug.Log("[LevelIntro] " + msg);
    }

    // ====================================================================== MAIN FLOW
    IEnumerator PlayIntro()
    {
        
        if (!ArtExamDirector.IntroAllowed)
        {
            StartHitler();
            Destroy(this);
            yield break;
        }

        cam = Camera.main;
        if (cam == null && mainCamera != null) cam = mainCamera.GetComponentInChildren<Camera>();
        if (cam == null && mainCamera != null) cam = mainCamera.GetComponentInParent<Camera>();

        if (cam == null || mainCamera == null)
        {
            Debug.LogError("[LevelIntro] Couldn't find the MainCameraController / main camera - skipping the intro.");
            StartHitler();
            Destroy(this);
            yield break;
        }

        BuildUI();   

        
        bool hasDirector = FindObjectOfType<ArtExamDirector>() != null;
        float wait = 0f;
        while (hasDirector && !ArtExamDirector.IntroReady && wait < 3f)
        {
            wait += Time.deltaTime;
            yield return null;
        }

        if (playerT == null) ResolvePlayer();

        LockPlayer(false);
        ArtExamDirector.SetHudVisible(false);
        TakeCamera();

        List<Shot> shots = BuildShots();
        Log("Intro started with " + shots.Count + " shots.");

        barTarget = BarHeight;
        hintVisible = true;

        if (shots.Count > 0)
        {
            
            InitShot(shots[0]);
            currentShot = shots[0];
            shotRunning = false;
            snapNext = true;
            yield return null;

            yield return TitleCard();

            StartCoroutine(Fade(0f, 1.2f));   

            for (int i = 0; i < shots.Count && !skipAll; i++)
            {
                Shot s = shots[i];
                if (i > 0) yield return Transition(s);
                if (skipAll) break;
                yield return PlayShot(s);
            }
        }

        yield return Outro();
    }

    IEnumerator TitleCard()
    {
        titleTarget = 1f;
        ExamAudio.Play(ExamAudio.Gong(), cam.transform.position, 0.6f);

        float t = 0f;
        while (t < titleHold && !skipAll)
        {
            t += Time.deltaTime;
            if (t > 0.8f && AdvancePressed()) break;
            yield return null;
        }

        titleTarget = 0f;
    }

    IEnumerator Transition(Shot s)
    {
        shotRunning = false;
        panelTarget = 0f;
        InitShot(s);
        shotTime = 0f;
        shotLen = 8f;

        Vector3 pos, look;
        ComputePose(s, 0f, out pos, out look);

        if (Vector3.Distance(cam.transform.position, pos) > cutDistance)
        {
            
            yield return Fade(1f, 0.22f);
            currentShot = s;
            snapNext = true;
            yield return null;
            yield return null;
            yield return Fade(0f, 0.35f);
        }
        else
        {
            
            currentShot = s;
            float t = 0f;
            while (t < 0.9f && !skipAll)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
    }

    IEnumerator PlayShot(Shot s)
    {
        if (s.callHitler) StartHitler();

        ApplyShotUI(s);

        string full = s.body;
        if (!string.IsNullOrEmpty(s.thought))
            full += "\n<i><color=#E6E1C8>\"" + s.thought + "\"</color></i>";

        int total = VisibleLength(full);
        float typeDur = total * typeSpeed;

        shotLen = typeDur + holdAfterTyping + 0.5f;
        shotTime = 0f;
        shotRunning = true;
        panelTarget = 1f;
        bodyText.text = Reveal(full, 0);

        ExamAudio.Play(ExamAudio.Pop(), cam.transform.position, 0.45f);

        float t = 0f;
        while (!skipAll)
        {
            t += Time.deltaTime;

            if (AdvancePressed())
            {
                if (t < typeDur) t = typeDur;   
                else break;                     
            }

            int n = typeDur > 0.001f ? Mathf.Clamp((int)(t / typeSpeed), 0, total) : total;
            bodyText.text = Reveal(full, n);

            if (t >= typeDur + holdAfterTyping) break;
            yield return null;
        }

        shotRunning = false;
        ClearLabel();
    }

    IEnumerator Outro()
    {
        panelTarget = 0f;
        hotbarTarget = 0f;
        controlsTarget = 0f;
        spaceTarget = 0f;
        titleTarget = 0f;
        hintVisible = false;
        shotRunning = false;
        ClearLabel();

        yield return Fade(1f, 0.5f);

        
        barTarget = 0f;
        barH = 0f;
        ApplyBars();

        StartHitler();
        ReleaseCamera();
        ArtExamDirector.SetHudVisible(true);
        LockPlayer(true);

        foreach (GameObject g in temp) if (g != null) Destroy(g);
        temp.Clear();
        if (previewRival != null) Destroy(previewRival.gameObject);

        yield return null;
        yield return null;   

        beginTarget = 1f;
        ExamAudio.Play(ExamAudio.Fanfare(), cam.transform.position, 0.8f);
        yield return Fade(0f, 0.7f);

        yield return new WaitForSeconds(1.3f);
        beginTarget = 0f;
        yield return new WaitForSeconds(0.8f);

        Log("Intro finished - player in control.");
        Destroy(this);
    }

    bool AdvancePressed()
    {
        return Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
    }

    // ====================================================================== CAMERA
    void TakeCamera()
    {
        mainCamera.inputEnabled = false;
        mainCamera.enabled = false;     
        rigActive = true;
    }

    void ReleaseCamera()
    {
        rigActive = false;
        mainCamera.enabled = true;
        mainCamera.inputEnabled = true;
    }

    void InitShot(Shot s)
    {
        Vector3 from = playerT != null ? playerT.position : cam.transform.position;
        Vector3 d = s.focus.position - from;
        d.y = 0f;
        s.baseYaw = d.sqrMagnitude > 0.25f ? Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg : cam.transform.eulerAngles.y;
    }

    
    void ComputePose(Shot s, float k, out Vector3 pos, out Vector3 look)
    {
        float e = Mathf.SmoothStep(0f, 1f, k);
        float sweep = Mathf.Lerp(s.sweepFrom, s.sweepTo, e);
        float yaw = (s.followFacing ? s.focus.eulerAngles.y : s.baseYaw) + s.yawOffset + sweep;

        look = s.focus.position + Vector3.up * s.lookHeight;
        Vector3 desired = look + Quaternion.Euler(s.pitch, yaw, 0f) * new Vector3(0f, 0f, -s.distance);
        pos = ClearLine(look, desired, s.focus);
    }

    
    Vector3 ClearLine(Vector3 look, Vector3 desired, Transform focus)
    {
        Vector3 dir = desired - look;
        float dist = dir.magnitude;
        if (dist < 0.05f) return desired;
        dir /= dist;

        int n = Physics.RaycastNonAlloc(look, dir, hits, dist, ~0, QueryTriggerInteraction.Ignore);
        float nearest = dist;

        for (int i = 0; i < n; i++)
        {
            RaycastHit h = hits[i];
            if (h.distance < 0.01f) continue;

            Collider c = h.collider;
            if (focus != null && c.transform.IsChildOf(focus)) continue;
            if (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic) continue;   
            if (c.GetComponentInParent<HitlerNPC>() != null) continue;
            if (c.GetComponentInParent<ArtistLevelPlayer>() != null) continue;
            if (c.GetComponentInParent<ExamRival>() != null) continue;

            if (h.distance < nearest) nearest = h.distance;
        }

        if (nearest >= dist) return desired;
        return look + dir * Mathf.Max(Mathf.Min(1.8f, dist), nearest - 0.4f);
    }

    void LateUpdate()
    {
        if (!rigActive || cam == null || currentShot == null || currentShot.focus == null) return;

        float k = shotLen > 0.01f ? Mathf.Clamp01(shotTime / shotLen) : 0f;

        Vector3 pos, look;
        ComputePose(currentShot, k, out pos, out look);

        Transform ct = cam.transform;
        float dt = Time.deltaTime;

        if (snapNext)
        {
            ct.position = pos;
            Vector3 d0 = look - pos;
            if (d0.sqrMagnitude > 0.01f) ct.rotation = Quaternion.LookRotation(d0.normalized);
            snapNext = false;
            return;
        }

        ct.position = Vector3.Lerp(ct.position, pos, 1f - Mathf.Exp(-cameraFollow * dt));

        Vector3 dir = look - ct.position;
        if (dir.sqrMagnitude < 0.01f) dir = ct.forward;
        Quaternion want = Quaternion.LookRotation(dir.normalized);
        ct.rotation = Quaternion.Slerp(ct.rotation, want, 1f - Mathf.Exp(-cameraTurn * dt));
    }

    // ====================================================================== SHOTS
    static string Hex(Color c) { return ColorUtility.ToHtmlStringRGB(c); }
    static string Hi(string text) { return "<b><color=#FFD84A>" + text + "</color></b>"; }
    static string Tool(WeaponType t, string text) { return "<b><color=#" + Hex(ExamUtil.WeaponColor(t)) + ">" + text + "</color></b>"; }

    List<Shot> BuildShots()
    {
        List<Shot> list = new List<Shot>();
        ArtExamDirector.IntroInfo info = ArtExamDirector.Intro;

        Color cBat = ExamUtil.WeaponColor(WeaponType.Fist);
        Color cRifle = ExamUtil.WeaponColor(WeaponType.Rifle);
        Color cBazooka = ExamUtil.WeaponColor(WeaponType.Bazooka);
        Color cStone = ExamUtil.WeaponColor(WeaponType.Stone);
        Color cParkour = new Color(0.4f, 0.95f, 0.85f);

        // ---- 1. Hitler ------------------------------------------------------------
        if (hitler != null)
        {
            list.Add(new Shot
            {
                focus = hitler.transform,
                lookHeight = 1.4f,
                distance = 5f,
                pitch = 12f,
                followFacing = true,
                sweepFrom = 22f,
                sweepTo = -12f,
                callHitler = true,
                color = Gold,
                chip = "YOUR MISSION",
                title = "THE YOUNG ARTIST",
                body = "Meet young Hitler, on his way to his easel. Your mission: get him accepted into the "
                       + Hi("Vienna Academy of Fine Arts") + " by clearing everything that stands in his way.",
                thought = "This is the young artist Hitler. I should follow him to his paintings."
            });
        }

        // ---- 2. Paintings ---------------------------------------------------------
        PaintingCanvas[] paintings = FindObjectsOfType<PaintingCanvas>();
        if (paintingFocus != null || paintings.Length > 0)
        {
            Transform focus;
            float dist, lookH;

            if (paintingFocus != null)
            {
                
                focus = paintingFocus;
                dist = paintingShotDistance;
                lookH = paintingLookHeight;
            }
            else
            {
                
                Vector3 c = Vector3.zero;
                foreach (PaintingCanvas p in paintings) c += p.transform.position;
                c /= paintings.Length;

                float r = 0f;
                foreach (PaintingCanvas p in paintings)
                {
                    Vector3 d = p.transform.position - c;
                    d.y = 0f;
                    r = Mathf.Max(r, d.magnitude);
                }

                GameObject fg = new GameObject("IntroFocus_Paintings");
                fg.transform.position = c;
                temp.Add(fg);

                focus = fg.transform;
                dist = Mathf.Clamp(r * 1.7f + 5f, 7f, 28f);
                lookH = 0.6f;
            }

            int need = info != null ? Mathf.Max(1, info.paintingsBeforeRivals) : 1;
            string when = need == 1 ? "Finish the first one" : "Finish " + need + " of them";
            string count = paintings.Length > 0 ? " - there are " + paintings.Length + " to fix" : "";

            list.Add(new Shot
            {
                focus = focus,
                lookHeight = lookH,
                distance = dist,
                pitch = 16f,
                color = Gold,
                labelText = "PAINTINGS",
                labelOffset = new Vector3(0f, 3.4f, 0f),
                chip = "TASK  -  ART",
                title = "THE PAINTINGS",
                body = "His paintings are flawed. Go to each canvas and " + Hi("correct it") + count + ". "
                       + when + " and the " + Hi("rival applicants") + " will start to arrive.",
                thought = "I should correct all of these and return to the future."
            });
        }

       
        if (info != null)
        {
            if (info.marble != null)
            {
                list.Add(new Shot
                {
                    focus = info.marble,
                    lookHeight = 1.4f,
                    distance = 6.5f,
                    pitch = 18f,
                    color = cBat,
                    hotbar = 0,
                    labelText = "MARBLE SCULPTURE",
                    labelOffset = new Vector3(0f, 4.2f, 0f),
                    chip = "BAT  -  KEY 1",
                    title = "THE MARBLE SCULPTURE",
                    body = "A block of marble waits to be carved. Equip the " + Tool(WeaponType.Fist, "BAT") + " (key " + Hi("1")
                           + ") and strike it " + Hi(info.marbleHits.ToString()) + " times to reveal the finished sculpture."
                });
            }

            if (info.gongs != null && info.gongs.Length > 0 && info.gongs[0] != null)
            {
                list.Add(new Shot
                {
                    focus = info.gongs[0],
                    lookHeight = 2.4f,
                    distance = 9f,
                    pitch = 12f,
                    color = cStone,
                    hotbar = 3,
                    labelText = "GONG 1",
                    labelOffset = new Vector3(0f, 4.6f, 0f),
                    chip = "STONE  -  KEY 4",
                    title = "THE GONGS",
                    body = "Hitler follows sound. Equip the " + Tool(WeaponType.Stone, "STONE") + " (key " + Hi("4")
                           + ") and throw it at the glowing gong. There are " + Hi("three gongs") + " - ring them " + Hi("in order")
                           + ", and each one draws Hitler a little closer."
                });
            }

            if (info.desk != null)
            {
                list.Add(new Shot
                {
                    focus = info.desk,
                    lookHeight = 1.6f,
                    distance = 7.5f,
                    pitch = 14f,
                    color = cStone,
                    hotbar = 3,
                    labelText = "REGISTRATION DESK",
                    labelOffset = new Vector3(0f, 4.4f, 0f),
                    chip = "STONE  -  FINAL STOP",
                    title = "THE REGISTRATION DESK",
                    body = "The third gong sends Hitler to this " + Hi("registration desk") + " to enroll. Get him here and the stone task is complete."
                });
            }

            if (info.wall != null)
            {
                list.Add(new Shot
                {
                    focus = info.wall,
                    lookHeight = 2.4f,
                    distance = 17f,
                    pitch = 9f,
                    sweepFrom = 15f,
                    sweepTo = -15f,
                    color = cBazooka,
                    hotbar = 2,
                    labelText = "WALL OF REJECTION",
                    labelOffset = new Vector3(0f, 7.2f, 0f),
                    chip = "BAZOOKA  -  KEY 3",
                    title = "THE WALL OF REJECTION",
                    body = "A wall of rejection letters blocks the Academy gate. Only the " + Tool(WeaponType.Bazooka, "BAZOOKA") + " (key "
                           + Hi("3") + ") can bring it down - keep firing until it collapses. Ammo is limited, so grab the crates nearby."
                });
            }

            
            Transform around = hitler != null ? hitler.transform : playerT;
            if (around != null)
            {
                previewRival = MakePreviewRival(around);
                if (previewRival != null)
                {
                    list.Add(new Shot
                    {
                        focus = previewRival,
                        lookHeight = 1.2f,
                        distance = 5.5f,
                        pitch = 8f,
                        color = cRifle,
                        hotbar = 1,
                        labelText = "RIVAL APPLICANT",
                        labelOffset = new Vector3(0f, 3.3f, 0f),
                        chip = "RIFLE  -  KEY 2",
                        title = "RIVAL APPLICANTS",
                        body = Hi("Rivals") + " will sneak in to splash paint over Hitler's work. Each splash costs him " + Hi("composure")
                               + " - if it hits zero, the exam is failed. Shoot them with the " + Tool(WeaponType.Rifle, "RIFLE") + " (key "
                               + Hi("2") + ") to disqualify them. " + info.rivalCount + " rivals will come."
                    });
                }
            }

            if (playerT != null)
            {
                list.Add(new Shot
                {
                    focus = playerT,
                    lookHeight = 1.3f,
                    distance = 4.6f,
                    pitch = 10f,
                    followFacing = true,
                    sweepFrom = -30f,
                    sweepTo = 0f,
                    color = Gold,
                    hotbar = 4,
                    chip = "YOUR ARSENAL",
                    title = "CHOOSE THE RIGHT TOOL",
                    body = "Four tools, four jobs. Switch with keys " + Hi("1-4") + " or the " + Hi("scroll wheel")
                           + ". The wrong tool does nothing, so match the tool to the task. Pick up ammo crates with " + Hi("E")
                           + " - and a supply drop appears if you ever run dry."
                });
            }
        }

        
        if (playerT != null)
        {
            list.Add(new Shot
            {
                focus = playerT,
                lookHeight = 1.2f,
                distance = 5.2f,
                pitch = 8f,
                followFacing = true,
                sweepFrom = 30f,
                sweepTo = 8f,
                color = cParkour,
                showSpace = true,
                chip = "PARKOUR",
                title = "VAULT OVER OBSTACLES",
                body = "Obstacles in your way? Press " + Hi("SPACE") + " to " + Hi("vault") + " over them. Use it to take shortcuts and to catch rivals before they reach Hitler."
            });

            list.Add(new Shot
            {
                focus = playerT,
                lookHeight = 1.3f,
                distance = 4.4f,
                pitch = 10f,
                followFacing = true,
                sweepFrom = 8f,
                sweepTo = 0f,
                color = Gold,
                showControls = true,
                chip = "BEFORE YOU GO",
                title = "QUICK TIPS",
                body = "Press " + Hi("E") + " to inspect things and pick up crates. Your " + Hi("minimap") + " pulses one objective at a time - "
                       + "watch it to find your next task. Protect Hitler's composure, and good luck!"
            });
        }

        
        list.RemoveAll(s => s.focus == null);
        return list;
    }

    Transform MakePreviewRival(Transform around)
    {
        Vector3 c = around.position;
        Vector3 spot;
        if (!ExamUtil.FindNavSpot(c, 9f, 14f, c.y, out spot))
            spot = ExamUtil.FindSpot(c, 9f, 14f, 0.8f, c.y);

        ExamRival r = ExamRival.Create(spot, 0, around);
        r.enabled = false;   

        Vector3 to = c - spot;
        to.y = 0f;
        if (to.sqrMagnitude > 0.01f) r.transform.rotation = Quaternion.LookRotation(to);
        return r.transform;
    }

    void ApplyShotUI(Shot s)
    {
        chipText.text = s.chip;
        chipBg.color = new Color(s.color.r, s.color.g, s.color.b, 0.95f);
        chipBg.rectTransform.sizeDelta = new Vector2(Mathf.Max(150f, s.chip.Length * 15f + 44f), 34f);
        accent.color = s.color;
        titleText.text = s.title;
        bodyText.text = "";

        hotbarMode = s.hotbar;
        hotbarTarget = s.hotbar >= -1 ? 1f : 0f;
        spaceTarget = s.showSpace ? 1f : 0f;
        controlsTarget = s.showControls ? 1f : 0f;

        ClearLabel();
        if (!string.IsNullOrEmpty(s.labelText) && s.focus != null)
            tempLabel = ExamLabel.Create(s.focus, s.labelOffset, s.labelText, s.color, 46, 0.012f);
    }

    void ClearLabel()
    {
        if (tempLabel != null) Destroy(tempLabel.gameObject);
        tempLabel = null;
    }

    // ====================================================================== TEXT HELPERS
    static int VisibleLength(string s)
    {
        int n = 0;
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '<')
            {
                int end = s.IndexOf('>', i);
                if (end > i) { i = end; continue; }
            }
            n++;
        }
        return n;
    }

    
    static string TagName(string tag)
    {
        int i = 1;
        while (i < tag.Length && tag[i] != '=' && tag[i] != '>' && tag[i] != ' ') i++;
        return tag.Substring(1, i - 1);
    }

    
    static string Reveal(string s, int count)
    {
        StringBuilder sb = new StringBuilder(s.Length + 60);
        List<string> open = new List<string>();   
        int shown = 0;
        bool hiding = false;

        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];
            if (ch == '<')
            {
                int end = s.IndexOf('>', i);
                if (end > i)
                {
                    string tag = s.Substring(i, end - i + 1);
                    bool closing = tag.StartsWith("</");
                    string name = TagName(closing ? "<" + tag.Substring(2) : tag);

                    if (closing)
                    {
                        for (int k = open.Count - 1; k >= 0; k--)
                            if (TagName(open[k]) == name) { open.RemoveAt(k); break; }
                    }
                    else open.Add(tag);

                    
                    if (!(hiding && name == "color")) sb.Append(tag);
                    i = end;
                    continue;
                }
            }

            if (!hiding && shown >= count)
            {
                
                for (int k = open.Count - 1; k >= 0; k--) sb.Append("</" + TagName(open[k]) + ">");

                
                sb.Append("<color=#00000000>");
                for (int k = 0; k < open.Count; k++)
                    if (TagName(open[k]) != "color") sb.Append(open[k]);

                hiding = true;
            }

            sb.Append(ch);
            shown++;
        }

        if (hiding) sb.Append("</color>");
        return sb.ToString();
    }

    // ====================================================================== UI BUILDING
    static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
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

    static RectTransform Full(string name, Transform parent)
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

    static Image Img(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
    {
        RectTransform rt = Rect(name, parent, anchor, pivot, pos, size);
        Image im = rt.gameObject.AddComponent<Image>();
        im.color = color;
        im.raycastTarget = false;
        return im;
    }

    static Text Txt(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
                    string content, int fontSize, TextAnchor align, Color color, bool bold, bool wrap)
    {
        RectTransform rt = Rect(name, parent, anchor, pivot, pos, size);
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

    static CanvasGroup MakeGroup(RectTransform rt)
    {
        CanvasGroup g = rt.gameObject.AddComponent<CanvasGroup>();
        g.alpha = 0f;
        g.interactable = false;
        g.blocksRaycasts = false;
        return g;
    }

    static void AddOutline(Text t)
    {
        Outline o = t.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.9f);
        o.effectDistance = new Vector2(2f, -2f);
    }

    
    static KeyUI MakeKey(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color color, int fontSize)
    {
        Vector2 mid = new Vector2(0.5f, 0.5f);
        KeyUI k = new KeyUI();

        Image border = Img(name, parent, anchor, mid, pos, size, color);
        k.root = border.rectTransform;
        k.border = border;

        Img("Face", k.root, mid, mid, Vector2.zero, size - new Vector2(10f, 10f), new Color(0.07f, 0.07f, 0.09f, 0.94f));
        k.label = Txt("Label", k.root, mid, mid, Vector2.zero, size, label, fontSize, TextAnchor.MiddleCenter, Color.white, true, false);
        return k;
    }

    RectTransform MakeBar(bool top)
    {
        GameObject go = new GameObject(top ? "BarTop" : "BarBottom", typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        Image im = go.AddComponent<Image>();
        im.color = Color.black;
        im.raycastTarget = false;
        return rt;
    }

    void BuildUI()
    {
        GameObject cg = new GameObject("LevelIntro_UI");
        canvas = cg.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        CanvasScaler sc = cg.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;

        Vector2 mid = new Vector2(0.5f, 0.5f);
        Vector2 topLeft = new Vector2(0f, 1f);
        Vector2 topCenter = new Vector2(0.5f, 1f);
        Vector2 botCenter = new Vector2(0.5f, 0f);

        
        barTop = MakeBar(true);
        barBottom = MakeBar(false);

        
        RectTransform hb = Rect("Hotbar", canvas.transform, topCenter, topCenter, new Vector2(0f, -150f), new Vector2(1000f, 90f));
        hotbarGroup = MakeGroup(hb);

        WeaponType[] types = { WeaponType.Fist, WeaponType.Rifle, WeaponType.Bazooka, WeaponType.Stone };
        string[] names = { "1   BAT", "2   RIFLE", "3   BAZOOKA", "4   STONE" };
        for (int i = 0; i < 4; i++)
        {
            slotColor[i] = ExamUtil.WeaponColor(types[i]);
            slots[i] = MakeKey(hb, "Slot" + i, names[i], mid, new Vector2((i - 1.5f) * 235f, 0f), new Vector2(215f, 72f), slotColor[i], 30);
        }

        
        Image cardBg = Img("Controls", canvas.transform, topCenter, topCenter, new Vector2(0f, -150f), new Vector2(900f, 260f), new Color(0f, 0f, 0f, 0.55f));
        controlsGroup = MakeGroup(cardBg.rectTransform);
        string[] keys = { "SPACE", "E", "1-4" };
        float[] keyW = { 210f, 70f, 130f };
        string[] what = { "Vault over obstacles", "Inspect things / pick up crates", "Switch tool  (or scroll wheel)" };
        for (int i = 0; i < 3; i++)
        {
            float y = 80f - i * 80f;
            MakeKey(cardBg.rectTransform, "CardKey" + i, keys[i], mid, new Vector2(-290f, y), new Vector2(keyW[i], 66f), Gold, 30);
            Txt("CardText" + i, cardBg.rectTransform, mid, new Vector2(0f, 0.5f), new Vector2(-150f, y), new Vector2(560f, 50f),
                what[i], 30, TextAnchor.MiddleLeft, Color.white, true, false);
        }

        
        RectTransform sr = Rect("SpaceCard", canvas.transform, topCenter, topCenter, new Vector2(0f, -160f), new Vector2(760f, 260f));
        spaceGroup = MakeGroup(sr);
        KeyUI big = MakeKey(sr, "BigSpace", "SPACE", mid, new Vector2(0f, 40f), new Vector2(560f, 130f), Gold, 66);
        spaceKeyRect = big.root;
        Text spaceSub = Txt("SpaceSub", sr, mid, mid, new Vector2(0f, -80f), new Vector2(760f, 50f),
                            "VAULT OVER OBSTACLES", 36, TextAnchor.MiddleCenter, Gold, true, false);
        AddOutline(spaceSub);

        
        RectTransform pr = Rect("Panel", canvas.transform, botCenter, botCenter, new Vector2(0f, 150f), new Vector2(1500f, 262f));
        panelBg = pr.gameObject.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.62f);
        panelBg.raycastTarget = false;
        panelGroup = MakeGroup(pr);

        accent = Img("Accent", pr, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(8f, 262f), Gold);
        chipBg = Img("Chip", pr, topLeft, topLeft, new Vector2(30f, -20f), new Vector2(260f, 34f), Gold);
        chipText = Txt("ChipText", chipBg.rectTransform, mid, mid, Vector2.zero, new Vector2(500f, 34f), "", 20,
                       TextAnchor.MiddleCenter, new Color(0.05f, 0.05f, 0.07f, 0.95f), true, false);
        titleText = Txt("Title", pr, topLeft, topLeft, new Vector2(30f, -62f), new Vector2(1440f, 56f), "", 42,
                        TextAnchor.UpperLeft, Color.white, true, false);
        bodyText = Txt("Body", pr, topLeft, topLeft, new Vector2(30f, -124f), new Vector2(1440f, 130f), "", 26,
                       TextAnchor.UpperLeft, new Color(0.95f, 0.95f, 0.92f), false, true);

        
        RectTransform fr = Full("Fade", canvas.transform);
        fade = fr.gameObject.AddComponent<Image>();
        fade.color = Color.black;
        fade.raycastTarget = false;

        
        RectTransform tr = Full("TitleCard", canvas.transform);
        titleGroup = MakeGroup(tr);
        titleBig = Txt("TitleBig", tr, mid, mid, new Vector2(0f, 50f), new Vector2(1700f, 140f), "THE ENTRANCE EXAM", 96,
                       TextAnchor.MiddleCenter, Gold, true, false);
        AddOutline(titleBig);
        titleSmall = Txt("TitleSmall", tr, mid, mid, new Vector2(0f, -40f), new Vector2(1500f, 50f), "VIENNA   -   1907", 38,
                         TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.9f), true, false);
        titleTag = Txt("TitleTag", tr, mid, mid, new Vector2(0f, -110f), new Vector2(1500f, 40f),
                       "Fix his future. Get him into the Academy.", 28, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.65f), false, false);

        
        RectTransform br = Full("Begin", canvas.transform);
        beginGroup = MakeGroup(br);
        beginText = Txt("BeginText", br, mid, mid, Vector2.zero, new Vector2(1700f, 200f), "BEGIN!", 120,
                        TextAnchor.MiddleCenter, Gold, true, false);
        AddOutline(beginText);

        
        string keyName = skipKey == KeyCode.Return ? "ENTER" : skipKey.ToString().ToUpper();
        skipHint = Txt("SkipHint", canvas.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 36f), new Vector2(900f, 34f),
                       "SPACE  next        " + keyName + "  skip intro", 20, TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0f), false, false);

        barH = 0f;
        barTarget = 0f;
    }

    // ====================================================================== PER-FRAME UI
    void Update()
    {
        if (canvas == null) return;
        float dt = Time.deltaTime;

        if (Input.GetKeyDown(skipKey) || Input.GetKeyDown(KeyCode.KeypadEnter)) skipAll = true;

        panelGroup.alpha = Mathf.MoveTowards(panelGroup.alpha, panelTarget, dt * 3.5f);
        hotbarGroup.alpha = Mathf.MoveTowards(hotbarGroup.alpha, hotbarTarget, dt * 3f);
        controlsGroup.alpha = Mathf.MoveTowards(controlsGroup.alpha, controlsTarget, dt * 3f);
        spaceGroup.alpha = Mathf.MoveTowards(spaceGroup.alpha, spaceTarget, dt * 3f);
        titleGroup.alpha = Mathf.MoveTowards(titleGroup.alpha, titleTarget, dt * 1.4f);
        beginGroup.alpha = Mathf.MoveTowards(beginGroup.alpha, beginTarget, dt * 4f);

        Color hc = skipHint.color;
        hc.a = Mathf.MoveTowards(hc.a, hintVisible ? 0.75f : 0f, dt * 2f);
        skipHint.color = hc;

        barH = Mathf.MoveTowards(barH, barTarget, dt * 260f);
        ApplyBars();

        
        int active = hotbarMode == 4 ? ((int)(Time.time / 1.2f)) % 4 : hotbarMode;
        for (int i = 0; i < 4; i++)
        {
            slotAlpha[i] = Mathf.MoveTowards(slotAlpha[i], i == active ? 1f : 0.3f, dt * 5f);
            Color c = slotColor[i];
            slots[i].border.color = new Color(c.r, c.g, c.b, slotAlpha[i]);
            slots[i].label.color = new Color(1f, 1f, 1f, slotAlpha[i]);
            slots[i].root.localScale = Vector3.one * Mathf.Lerp(1f, 1.14f, Mathf.InverseLerp(0.3f, 1f, slotAlpha[i]));
        }

        
        spaceKeyRect.localScale = Vector3.one * (1f + 0.07f * Mathf.Sin(Time.time * 6.5f));
        beginText.rectTransform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(Time.time * 8f));

        if (shotRunning) shotTime += dt;
    }

    void ApplyBars()
    {
        if (barTop != null) barTop.sizeDelta = new Vector2(0f, barH);
        if (barBottom != null) barBottom.sizeDelta = new Vector2(0f, barH);
    }

    void SetFade(float a)
    {
        if (fade != null) fade.color = new Color(0f, 0f, 0f, a);
    }

    IEnumerator Fade(float to, float time)
    {
        float from = fade.color.a;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            SetFade(Mathf.Lerp(from, to, Mathf.Clamp01(t / time)));
            yield return null;
        }
        SetFade(to);
    }

    
    void OnDestroy()
    {
        if (rigActive && mainCamera != null) ReleaseCamera();   

        ClearLabel();
        foreach (GameObject g in temp) if (g != null) Destroy(g);
        if (previewRival != null) Destroy(previewRival.gameObject);
        if (canvas != null) Destroy(canvas.gameObject);
    }
}