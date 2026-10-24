using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;



[DisallowMultipleComponent]
public class ArtExamDirector : MonoBehaviour
{

    [Header("AI character (rivals + registration clerk)")]
    public ExamAIConfig ai = new ExamAIConfig();

    [Header("Level intro")]
    [Tooltip("Play the cinematic tour (needs a LevelIntroSequence in the scene).")]
    public bool playIntro = true;
    [Tooltip("Replay the intro after the exam is failed and the scene reloads.")]
    public bool playIntroOnRetry = false;


    [Header("HUD layout")]
    [Tooltip("How far below the top of the screen the message text sits (in 1920x1080 reference pixels). Bigger = lower.")]
    public float toastYOffset = 230f;
    [Tooltip("Font size of the message text at the top.")]
    public int toastFontSize = 30;

    [Header("Rivals")]
    [Tooltip("Rivals start spawning once this many paintings are corrected.")]
    public int paintingsBeforeRivals = 1;
    [Tooltip("Seconds between that painting being finished and the first rival spawning.")]
    public float rivalDelayAfterPainting = 4f;

    [Header("Station placement")]
    [Tooltip("The marble block and Rejection Wall are never placed where anything on these layers is in the way. Default = buildings/props.")]
    public LayerMask avoidLayers = 1;

    [Header("Manual station placement (optional)")]
    [Tooltip("Empties where the marble block can spawn. One is picked. Leave empty to use automatic placement.")]
    public Transform[] marbleSpawnPoints;
    [Tooltip("Empties where the Rejection Wall can spawn. One is picked. Leave empty to use automatic placement.")]
    public Transform[] wallSpawnPoints;
    [Tooltip("If several empties are listed, pick one at random. Off = always use the first.")]
    public bool randomPointFromList = true;
    [Tooltip("Drop each empty down onto the ground below it.")]
    public bool snapSpawnsToGround = true;


    static ArtistLevelPlayer cachedPlayer;


    public static ExamAIConfig AI { get; private set; }
    static System.Reflection.FieldInfo uiDirtyField;

    public static ArtistLevelPlayer Player
    {
        get
        {
            if (cachedPlayer == null) cachedPlayer = Object.FindObjectOfType<ArtistLevelPlayer>();
            return cachedPlayer;
        }
    }

    public static Collider PlayerCollider
    {
        get { ArtistLevelPlayer p = Player; return p != null ? p.CC : null; }
    }


    public static WeaponType CurrentWeaponType
    {
        get
        {
            ArtistLevelPlayer p = Player;
            if (p == null || p.weapons == null || p.weapons.Length == 0) return WeaponType.Fist;
            return p.CurrentWeapon.type;
        }
    }

    public static int TotalAmmo(WeaponType type)
    {
        ArtistLevelPlayer p = Player;
        if (p == null) return 0;
        int n = 0;
        foreach (WeaponSlot w in p.weapons)
            if (w.type == type) n += w.ammoInClip + w.reserveAmmo;
        return n;
    }

    public static void GiveAmmo(WeaponType type, int amount)
    {
        ArtistLevelPlayer p = Player;
        if (p == null) return;

        foreach (WeaponSlot w in p.weapons)
            if (w.type == type) w.reserveAmmo += amount;


        if (uiDirtyField == null)
            uiDirtyField = typeof(ArtistLevelPlayer).GetField("uiDirty",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (uiDirtyField != null) uiDirtyField.SetValue(p, true);
    }

    public class IntroInfo
    {
        public Transform marble;
        public int marbleHits;
        public Transform[] gongs;
        public Transform desk;
        public Transform wall;
        public int rivalCount;
        public int paintingsBeforeRivals;
    }

    public static IntroInfo Intro { get; private set; }
    public static bool IntroReady { get; private set; }

    public static bool IntroAllowed
    {
        get
        {
            ArtExamDirector d = instance;
            if (d == null) return true;
            if (!d.playIntro) return false;
            if (d.isRetry && !d.playIntroOnRetry) return false;
            return true;
        }
    }

    public static void SetHudVisible(bool visible)
    {
        ArtExamDirector d = instance;
        if (d == null) return;

        d.hudVisible = visible;
        if (d.canvas != null) d.canvas.enabled = visible;

        // the opening toast was held back until the intro finished
        if (visible && d.pendingStartToast)
        {
            d.pendingStartToast = false;
            d.ShowStartToast();
        }
    }

    static bool retryPending;          // survives the scene reload after a fail
    bool isRetry, hudVisible = true, pendingStartToast;

    void ShowStartToast()
    {
        Toast("ENTRANCE EXAM - get Hitler into the Academy!   Scroll / keys 1-4 switch tools, E inspects.",
              new Color(1f, 0.92f, 0.5f), 7f);
    }

    void BuildIntroInfo()
    {
        IntroInfo info = new IntroInfo();

        info.marble = marble != null ? marble.transform : null;
        info.marbleHits = marble != null ? marble.hitsNeeded : 0;
        info.wall = wall != null ? wall.transform : null;

        if (course != null && course.gongs != null)
        {
            info.gongs = new Transform[course.gongs.Length];   // if gongs is a List, use .Count
            for (int i = 0; i < info.gongs.Length; i++)
                info.gongs[i] = course.gongs[i] != null ? course.gongs[i].transform : null;
        }

        info.desk = FindDesk();
        info.rivalCount = RivalCount;
        info.paintingsBeforeRivals = paintingsBeforeRivals;

        Intro = info;
    }

    // I haven't seen ExamStoneCourse, so this guesses. If it has a real desk field, replace the body with: return course.yourDeskField.transform;
    Transform FindDesk()
    {
        if (course != null)
        {
            foreach (System.Reflection.FieldInfo f in course.GetType().GetFields(
                         System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
            {
                if (!f.Name.ToLower().Contains("desk")) continue;
                Transform t = AsTransform(f.GetValue(course));
                if (t != null) return t;
            }
        }

        foreach (Transform t in Object.FindObjectsOfType<Transform>())
        {
            string n = t.name.ToLower();
            if (n.Contains("desk") || n.Contains("registration")) return t;
        }
        return null;
    }

    static Transform AsTransform(object o)
    {
        Transform t = o as Transform; if (t != null) return t;
        Component c = o as Component; if (c != null) return c.transform;
        GameObject g = o as GameObject; if (g != null) return g.transform;
        return null;
    }

    // ------------------------------------------------------------------ bootstrap
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterHooks()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootCurrentScene()
    {
        TryCreate();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate();
    }

    Transform PickPoint(Transform[] list, string namePrefix)
    {
        List<Transform> valid = new List<Transform>();
        if (list != null)
            foreach (Transform t in list)
                if (t != null) valid.Add(t);

        if (valid.Count == 0)
            foreach (Transform t in Object.FindObjectsOfType<Transform>())
                if (t.name.StartsWith(namePrefix)) valid.Add(t);

        if (valid.Count == 0) return null;
        return randomPointFromList ? valid[Random.Range(0, valid.Count)] : valid[0];
    }

    Vector3 SpawnGround(Transform t)
    {
        Vector3 g;
        if (snapSpawnsToGround && ExamUtil.Ground(t.position, t.position.y, out g)) return g;
        return t.position;
    }

    // Draws the footprint of each station in the Scene view so you can see what will fit.
    void OnDrawGizmos()
    {
        DrawSpawnGizmos(marbleSpawnPoints, new Color(0.75f, 0.45f, 1f), new Vector3(1.8f, 2.4f, 1.8f));
        DrawSpawnGizmos(wallSpawnPoints, new Color(1f, 0.35f, 0.28f), new Vector3(8.8f, 4.8f, 1.2f));
    }

    static void DrawSpawnGizmos(Transform[] points, Color c, Vector3 size)
    {
        if (points == null) return;
        Gizmos.color = c;

        foreach (Transform t in points)
        {
            if (t == null) continue;

            Gizmos.matrix = Matrix4x4.TRS(t.position + Vector3.up * size.y * 0.5f,
                                          Quaternion.Euler(0f, t.eulerAngles.y, 0f), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * (size.z * 0.5f + 1.5f));   // facing direction
        }
        Gizmos.matrix = Matrix4x4.identity;
    }

    static void TryCreate()
    {
        if (Object.FindObjectOfType<ArtExamDirector>() != null) return;
        if (Object.FindObjectOfType<ArtistLevelPlayer>() == null) return;


        Level1ObjectiveTracker t = Object.FindObjectOfType<Level1ObjectiveTracker>();
        if (t != null) t.enabled = false;

        new GameObject("ArtExamDirector").AddComponent<ArtExamDirector>();
    }

    // ------------------------------------------------------------------ objectives
    class Objective
    {
        public string tag;
        public string text;
        public Color color;
        public int cur, max;
        public WeaponType weapon;
        public bool usesWeapon;
        public System.Func<Vector3> where;
        public Text row;
        public bool Done { get { return cur >= max; } }
    }

    readonly List<Objective> objectives = new List<Objective>();
    Objective oBat, oRifle, oBazooka, oStone, oPaint;

    // ------------------------------------------------------------------ state
    ArtistLevelPlayer player;
    Level1ObjectiveTracker tracker;
    HitlerNPC hitler;
    readonly List<PaintingCanvas> paintings = new List<PaintingCanvas>();
    int paintingsDone, paintingsTotal;
    readonly HashSet<PaintingCanvas> donePaintings = new HashSet<PaintingCanvas>();

    ExamMarbleBlock marble;
    ExamRejectionWall wall;
    ExamStoneCourse course;
    ExamHitlerLure lure;
    readonly List<ExamRival> rivals = new List<ExamRival>();
    ExamBeacon marbleBeacon, wallBeacon, stoneBeacon;

    const int RivalCount = 4;
    int rivalsSpawned, rivalsDown;
    float composure = 100f;
    bool finished, failed;
    float celebrationDuration = 3f;
    string futureSceneName = "FutureScene";
    float nextHud, nextSupplyCheck;

    // ------------------------------------------------------------------ HUD
    Canvas canvas;
    Text toastText;
    float toastUntil;
    Color toastColor = Color.white;
    RectTransform composureFill;
    Text composureLabel;
    Image flash;
    GameObject banner;
    Text bannerTitle, bannerSub;
    const float BarWidth = 340f;


    Text promptText;
    Image thoughtBg;
    Text thoughtText;
    float thoughtUntil;

    // ====================================================================== SETUP
    void Awake()
    {
        AI = ai;

        instance = this;
        Intro = null;
        IntroReady = false;          // statics survive scene reloads, so reset them
        isRetry = retryPending;
        retryPending = false;

        tracker = Object.FindObjectOfType<Level1ObjectiveTracker>();
        if (tracker != null)
        {
            tracker.enabled = false;
            celebrationDuration = tracker.celebrationDuration;
            if (!string.IsNullOrEmpty(tracker.futureSceneName)) futureSceneName = tracker.futureSceneName;
            hitler = tracker.hitler;

            if (tracker.paintings != null)
                foreach (PaintingCanvas p in tracker.paintings)
                    if (p != null) paintings.Add(p);
        }

        if (hitler == null) hitler = Object.FindObjectOfType<HitlerNPC>();

        if (paintings.Count == 0)
            foreach (PaintingCanvas p in Object.FindObjectsOfType<PaintingCanvas>())
                paintings.Add(p);
    }

    void Start()
    {
        player = Player;
        if (player == null) { IntroReady = true; return; }

        foreach (WeaponSlot w in player.weapons)
        {
            if (w.effectPrefab != null) continue;
            if (w.type == WeaponType.Rifle) w.effectPrefab = ExamFx.MakeTemplate(ExamFx.Kind.Spark);
            if (w.type == WeaponType.Bazooka) w.effectPrefab = ExamFx.MakeTemplate(ExamFx.Kind.Explosion);
        }

        foreach (PaintingCanvas p in paintings)
        {
            PaintingCanvas captured = p;
            p.OnPaintingComplete += () => OnPaintingDone(captured);
            paintingsTotal++;
        }

        BuildStations();
        BuildObjectives();
        BuildHUD();
        UpdateTrackerText();

        // if an intro will run, IT starts Hitler walking (and the toast) when it finishes
        bool introWillPlay = IntroAllowed && Object.FindObjectOfType<LevelIntroSequence>() != null;

        if (tracker != null && hitler != null && !introWillPlay) hitler.BeginWalkingToEasel();

        if (introWillPlay) pendingStartToast = true;
        else ShowStartToast();

        BuildIntroInfo();
        IntroReady = true;      // the intro script is waiting for this

        StartCoroutine(RivalWave());
    }

    void OnDestroy()
    {
        //foreach (PaintingCanvas p in paintings)
        //    if (p != null) p.OnPaintingComplete -= OnPaintingDone;

        if (canvas != null) Destroy(canvas.gameObject);
    }

    void BuildStations()
    {
        Vector3 pPos = player.transform.position;
        float refY = pPos.y;

        // BAT
        Vector3 mSpot = Vector3.zero;
        float mYaw = Random.Range(0f, 360f);
        Transform mPoint = PickPoint(marbleSpawnPoints, "MarbleSpawn");
        if (mPoint != null)
        {
            mSpot = SpawnGround(mPoint);
            mYaw = mPoint.eulerAngles.y;
        }
        else if (!ExamUtil.FindStationSpot(pPos, 7f, 13f, 1.4f, 1.4f, 3.2f, refY, avoidLayers, false, out mSpot))
        {
            Debug.LogWarning("[ArtExam] No clear spot for the marble block - using the old placement.");
            mSpot = ExamUtil.FindSpot(pPos, 7f, 13f, 1.6f, refY);
        }
        marble = ExamMarbleBlock.Create(mSpot, mYaw);
        marble.OnProgress += OnMarbleProgress;
        marbleBeacon = ExamBeacon.Create(mSpot, ExamUtil.WeaponColor(WeaponType.Fist), "MARBLE\nBAT 1");

        // BAZOOKA
        Vector3 wSpot = Vector3.zero;
        Vector3 wLook = pPos;
        Transform wPoint = PickPoint(wallSpawnPoints, "WallSpawn");
        if (wPoint != null)
        {
            wSpot = SpawnGround(wPoint);
            wLook = wSpot + wPoint.forward;
        }
        else if (!ExamUtil.FindStationSpot(pPos, 20f, 32f, 5.2f, 2.5f, 5.6f, refY, avoidLayers, true, out wSpot))
        {
            Debug.LogWarning("[ArtExam] No clear spot for the rejection wall - using the old placement.");
            wSpot = ExamUtil.FindSpot(pPos, 20f, 32f, 4.5f, refY);
        }
        wall = ExamRejectionWall.Create(wSpot, wLook);
        wall.OnCleared += OnWallCleared;
        wallBeacon = ExamBeacon.Create(wSpot, ExamUtil.WeaponColor(WeaponType.Bazooka), "REJECTION WALL\nBAZOOKA 3");

        // STONE
        Vector3 hPos = hitler != null ? hitler.transform.position : pPos;
        lure = hitler != null ? ExamHitlerLure.Attach(hitler.transform) : null;
        course = ExamStoneCourse.Build(hPos, hPos.y, lure);
        course.OnProgress += OnStoneProgress;
        stoneBeacon = ExamBeacon.Create(course.CurrentTargetPosition, ExamUtil.WeaponColor(WeaponType.Stone), course.CurrentTargetName);

        // starting supplies
        Vector3 s1 = ExamUtil.FindSpot(pPos, 3f, 6f, 0.6f, refY);
        ExamCrate.Create(s1, WeaponType.Rifle, 8);
        Vector3 s2 = ExamUtil.FindSpot(wSpot, 7f, 10f, 0.7f, refY);
        ExamCrate.Create(s2, WeaponType.Bazooka, 3);
        Vector3 s3 = ExamUtil.FindSpot(course.gongs[0].transform.position, 3f, 5f, 0.6f, refY);
        ExamCrate.Create(s3, WeaponType.Stone, 6);
    }

    void BuildObjectives()
    {
        oBat = new Objective
        {
            tag = "BAT 1",
            text = "Chisel the marble block",
            color = ExamUtil.WeaponColor(WeaponType.Fist),
            max = marble.hitsNeeded,
            where = () => marble != null ? marble.transform.position : Vector3.positiveInfinity
        };
        oRifle = new Objective
        {
            tag = "RIFLE 2",
            text = "Disqualify the rival applicants",
            color = ExamUtil.WeaponColor(WeaponType.Rifle),
            max = RivalCount,
            weapon = WeaponType.Rifle,
            usesWeapon = true,
            where = NearestRivalPosition
        };
        oBazooka = new Objective
        {
            tag = "BAZOOKA 3",
            text = "Blast the Wall of Rejection",
            color = ExamUtil.WeaponColor(WeaponType.Bazooka),
            max = 1,
            weapon = WeaponType.Bazooka,
            usesWeapon = true,
            where = () => wall != null ? wall.transform.position : Vector3.positiveInfinity
        };
        oStone = new Objective
        {
            tag = "STONE 4",
            text = "Ring the gongs - lure Hitler to registration",
            color = ExamUtil.WeaponColor(WeaponType.Stone),
            max = 3,
            weapon = WeaponType.Stone,
            usesWeapon = true,
            where = () => course != null ? course.CurrentTargetPosition : Vector3.positiveInfinity
        };

        objectives.Add(oBat);
        objectives.Add(oRifle);
        objectives.Add(oBazooka);
        objectives.Add(oStone);

        if (paintingsTotal > 0)
        {
            oPaint = new Objective
            {
                tag = "ART",
                text = "Correct the paintings",
                color = new Color(1f, 0.85f, 0.3f),
                max = paintingsTotal,
                where = () => Vector3.positiveInfinity
            };
            objectives.Add(oPaint);
        }
    }

    Vector3 NearestRivalPosition()
    {
        Vector3 best = Vector3.positiveInfinity;
        float bestD = float.MaxValue;
        foreach (ExamRival r in rivals)
        {
            if (r == null) continue;
            float d = (r.transform.position - player.transform.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = r.transform.position; }
        }
        return best;
    }

    // ====================================================================== RIVALS
    IEnumerator RivalWave()
    {
        int needed = Mathf.Min(paintingsBeforeRivals, paintingsTotal);
        while (paintingsDone < needed && !finished && !failed) yield return null;

        yield return new WaitForSeconds(rivalDelayAfterPainting);

        while (rivalsSpawned < RivalCount && !finished && !failed)
        {
            SpawnRival();
            yield return new WaitForSeconds(18f);
        }
    }

    void SpawnRival()
    {
        Transform target = hitler != null ? hitler.transform : player.transform;
        Vector3 center = target.position;


        Vector3 spot;
        if (!ExamUtil.FindNavSpot(center, 26f, 38f, center.y, out spot))
            spot = ExamUtil.FindSpot(center, 26f, 38f, 0.8f, center.y);

        ExamRival r = ExamRival.Create(spot, rivalsSpawned, target);
        r.OnSabotage += OnRivalSabotage;
        r.OnDisqualified += OnRivalDisqualified;
        rivals.Add(r);
        rivalsSpawned++;

        Toast("A rival applicant is sneaking in from the " + ExamUtil.Compass(spot - center)
              + " to ruin Hitler's work!  RIFLE (2)", ExamUtil.WeaponColor(WeaponType.Rifle), 5f);
    }

    void OnRivalSabotage(ExamRival r)
    {
        composure = Mathf.Max(0f, composure - 25f);
        flash.color = new Color(0.9f, 0.1f, 0.1f, 0.35f);

        Vector3 at = hitler != null ? hitler.transform.position : player.transform.position;
        ExamLabel.Pop(at + Vector3.up * 2.6f, "Composure -25", new Color(1f, 0.4f, 0.35f));
        Toast("Hitler got splashed with paint!", new Color(1f, 0.45f, 0.35f), 3f);

        if (composure <= 0f && !failed && !finished) StartCoroutine(FailRoutine());
    }

    void OnRivalDisqualified(ExamRival r)
    {
        rivalsDown++;
        oRifle.cur = rivalsDown;


        Vector3 want = r.transform.position + r.transform.forward * 2f;
        Vector3 g;
        if (!ExamUtil.Ground(want, r.transform.position.y, out g)) g = r.transform.position;
        ExamCrate.Create(g, WeaponType.Rifle, 6);

        Toast("Rival disqualified!  " + rivalsDown + "/" + RivalCount, ExamUtil.WeaponColor(WeaponType.Rifle), 3f);
        CheckComplete();
    }

    // ====================================================================== STATION EVENTS
    void OnMarbleProgress(int hits, int needed)
    {
        oBat.cur = hits;
        if (oBat.Done)
        {
            if (marbleBeacon != null) marbleBeacon.Remove();
            Toast("A masterpiece! The sculpture is finished.", ExamUtil.WeaponColor(WeaponType.Fist), 3f);
            CheckComplete();
        }
    }

    void OnWallCleared()
    {
        oBazooka.cur = 1;
        if (wallBeacon != null) wallBeacon.Remove();
        Toast("The Wall of Rejection has fallen - the Academy gate is open!", ExamUtil.WeaponColor(WeaponType.Bazooka), 4f);
        CheckComplete();
    }

    void OnStoneProgress(int step)
    {
        oStone.cur = step;

        if (step < 3)
        {
            stoneBeacon.SetTarget(course.CurrentTargetPosition, course.CurrentTargetName);
            Toast("Hitler followed the sound.  Next: gong " + (step + 1), ExamUtil.WeaponColor(WeaponType.Stone), 3.5f);
        }
        else
        {
            if (stoneBeacon != null) stoneBeacon.Remove();
            Toast("Hitler is at the registration desk!", ExamUtil.WeaponColor(WeaponType.Stone), 3.5f);
        }

        CheckComplete();
    }

    void OnPaintingDone(PaintingCanvas p)
    {
        if (this == null) return;
        if (!donePaintings.Add(p)) return;      // ignore duplicate events

        paintingsDone = donePaintings.Count;
        if (oPaint != null) oPaint.cur = paintingsDone;
        UpdateTrackerText();
        CheckComplete();
    }

    void UpdateTrackerText()
    {
        if (tracker != null && tracker.paintingProgressText != null)
            tracker.paintingProgressText.text = "Paintings Corrected: " + paintingsDone + " / " + paintingsTotal;
    }

    // ====================================================================== COMPLETION / FAILURE
    void CheckComplete()
    {
        if (finished || failed) return;
        foreach (Objective o in objectives)
            if (!o.Done) return;

        StartCoroutine(FinishRoutine());
    }

    IEnumerator FinishRoutine()
    {
        finished = true;

        ShowBanner("APPLICATION ACCEPTED", "Vienna Academy of Fine Arts, 1907\nHitler has been admitted.", new Color(1f, 0.85f, 0.3f));
        ExamAudio.Play(ExamAudio.Fanfare(), player.transform.position, 1f);

        if (WorldStateManager.Instance != null)
            WorldStateManager.Instance.SetLevelCompleted(LevelId.ArtistHitler);

        if (hitler != null) hitler.Celebrate();

        yield return new WaitForSeconds(celebrationDuration);

        if (Application.CanStreamedLevelBeLoaded(futureSceneName))
            SceneManager.LoadScene(futureSceneName);
        else
            Debug.LogError("[ArtExamDirector] Can't find a scene called \"" + futureSceneName
                           + "\". Check the spelling and that it's in File > Build Settings > Scenes In Build.");
    }

    IEnumerator FailRoutine()
    {
        failed = true;
        ShowBanner("EXAM FAILED", "Hitler lost his composure.\nThe rivals ruined his work. Try again.", new Color(1f, 0.4f, 0.35f));

        yield return new WaitForSeconds(3.5f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ====================================================================== UPDATE
    void Update()
    {
        if (player == null) return;


        if (toastText != null && toastText.gameObject.activeSelf)
        {
            float left = toastUntil - Time.time;
            if (left <= 0f) toastText.gameObject.SetActive(false);
            else toastText.color = new Color(toastColor.r, toastColor.g, toastColor.b, Mathf.Clamp01(left / 0.6f));
        }


        if (flash != null && flash.color.a > 0f)
            flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, Mathf.MoveTowards(flash.color.a, 0f, Time.deltaTime * 0.6f));

        HandleInteraction();

        if (thoughtBg != null && thoughtBg.gameObject.activeSelf && Time.time >= thoughtUntil)
            thoughtBg.gameObject.SetActive(false);

        if (Time.time >= nextHud)
        {
            nextHud = Time.time + 0.2f;
            RefreshHUD();
        }

        if (hudVisible) HandleInteraction();

        if (Time.time >= nextSupplyCheck)
        {
            nextSupplyCheck = Time.time + 2f;
            EnsureSupplies();
        }
    }


    void EnsureSupplies()
    {
        if (finished || failed) return;

        foreach (Objective o in objectives)
        {
            if (!o.usesWeapon || o.Done) continue;
            if (TotalAmmo(o.weapon) > 0) continue;
            if (ExamCrate.CountOf(o.weapon) > 0) continue;

            Vector3 want = player.transform.position + player.transform.forward * 3f;
            Vector3 g;
            if (!ExamUtil.Ground(want, player.transform.position.y, out g)) g = player.transform.position;

            int amount = o.weapon == WeaponType.Rifle ? 8 : (o.weapon == WeaponType.Bazooka ? 3 : 6);
            ExamCrate.Create(g, o.weapon, amount);
            Toast("Supply drop! Grab the crate for the " + ExamUtil.WeaponTag(o.weapon).Split(' ')[0] + ".", o.color, 4f);
        }
    }

    // ====================================================================== HUD
    RectTransform NewRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    Image NewImage(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        RectTransform rt = NewRect(name, parent, anchor, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    Text NewText(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size,
                 string content, int fontSize, TextAnchor align, Color color)
    {
        RectTransform rt = NewRect(name, parent, anchor, pos, size);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = ExamUtil.UIFont;
        t.supportRichText = true;
        t.text = content;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    RectTransform FullScreen(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    void BuildHUD()
    {
        GameObject cg = new GameObject("ArtExam_HUD");
        canvas = cg.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        CanvasScaler sc = cg.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;


        RectTransform fr = FullScreen("Flash");
        flash = fr.gameObject.AddComponent<Image>();
        flash.color = new Color(0.9f, 0.1f, 0.1f, 0f);
        flash.raycastTarget = false;


        const float rowH = 34f;
        float panelH = 70f + objectives.Count * rowH;
        Vector2 topLeft = new Vector2(0f, 1f);
        RectTransform panel = NewRect("Objectives", canvas.transform, topLeft, new Vector2(20f + 330f, -(20f + panelH * 0.5f)), new Vector2(660f, panelH));
        Image bg = panel.gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.5f);
        bg.raycastTarget = false;

        Vector2 topCenter = new Vector2(0.5f, 1f);
        NewText("Title", panel, topCenter, new Vector2(0f, -24f), new Vector2(640f, 36f),
                "ENTRANCE EXAM  -  Academy of Fine Arts, Vienna 1907", 22, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.6f)).fontStyle = FontStyle.Bold;

        for (int i = 0; i < objectives.Count; i++)
        {
            objectives[i].row = NewText("Row" + i, panel, topCenter, new Vector2(0f, -(62f + i * rowH)), new Vector2(640f, rowH),
                                        "", 20, TextAnchor.MiddleLeft, Color.white);
        }


        Vector2 topRight = new Vector2(1f, 1f);
        composureLabel = NewText("ComposureLabel", canvas.transform, topRight, new Vector2(-200f, -32f), new Vector2(BarWidth, 26f),
                                 "HITLER'S COMPOSURE", 20, TextAnchor.MiddleCenter, Color.white);
        composureLabel.fontStyle = FontStyle.Bold;

        RectTransform barBg = NewRect("ComposureBG", canvas.transform, topRight, new Vector2(-200f, -62f), new Vector2(BarWidth, 18f));
        Image bgImg = barBg.gameObject.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.6f);
        bgImg.raycastTarget = false;

        GameObject fillGo = new GameObject("Fill", typeof(RectTransform));
        composureFill = fillGo.GetComponent<RectTransform>();
        composureFill.SetParent(barBg, false);
        composureFill.anchorMin = new Vector2(0f, 0f);
        composureFill.anchorMax = new Vector2(0f, 1f);
        composureFill.pivot = new Vector2(0f, 0.5f);
        composureFill.anchoredPosition = Vector2.zero;
        composureFill.sizeDelta = new Vector2(BarWidth, 0f);
        Image fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.4f, 0.9f, 0.4f);
        fillImg.raycastTarget = false;


        toastText = NewText("Toast", canvas.transform, topCenter, new Vector2(0f, -300f), new Vector2(1400f, 50f),
                            "", toastFontSize, TextAnchor.MiddleCenter, Color.white);
        toastText.fontStyle = FontStyle.Bold;
        toastText.gameObject.SetActive(false);
        Outline o = toastText.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.9f);
        o.effectDistance = new Vector2(2f, -2f);


        Vector2 bottomCenter = new Vector2(0.5f, 0f);
        promptText = NewText("Prompt", canvas.transform, bottomCenter, new Vector2(0f, 250f), new Vector2(700f, 40f),
                             "", 28, TextAnchor.MiddleCenter, Color.white);
        promptText.fontStyle = FontStyle.Bold;
        Outline po = promptText.gameObject.AddComponent<Outline>();
        po.effectColor = new Color(0f, 0f, 0f, 0.9f);
        po.effectDistance = new Vector2(2f, -2f);
        promptText.gameObject.SetActive(false);

        thoughtBg = NewImage("ThoughtBG", canvas.transform, bottomCenter, new Vector2(0f, 335f), new Vector2(1300f, 76f), new Color(0f, 0f, 0f, 0.6f));
        thoughtText = NewText("Thought", thoughtBg.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1260f, 70f),
                              "", 26, TextAnchor.MiddleCenter, new Color(0.96f, 0.95f, 0.85f));
        thoughtBg.gameObject.SetActive(false);


        RectTransform br = FullScreen("Banner");
        banner = br.gameObject;
        Image bImg = banner.AddComponent<Image>();
        bImg.color = new Color(0f, 0f, 0f, 0.6f);
        bImg.raycastTarget = false;
        Vector2 mid = new Vector2(0.5f, 0.5f);
        bannerTitle = NewText("BannerTitle", br, mid, new Vector2(0f, 40f), new Vector2(1600f, 120f), "", 72, TextAnchor.MiddleCenter, Color.white);
        bannerTitle.fontStyle = FontStyle.Bold;
        bannerSub = NewText("BannerSub", br, mid, new Vector2(0f, -60f), new Vector2(1600f, 120f), "", 32, TextAnchor.MiddleCenter, Color.white);
        banner.SetActive(false);

        RefreshHUD();
    }

    void RefreshHUD()
    {
        if (canvas == null) return;

        foreach (Objective o in objectives)
        {
            if (o.row == null) continue;

            string tag = "<color=#" + ColorUtility.ToHtmlStringRGB(o.color) + "><b>[" + o.tag + "]</b></color>";
            if (o.Done)
            {
                o.row.text = "<color=#7f9f7f>[x] " + o.text + "  " + o.cur + "/" + o.max + "  DONE</color>";
                continue;
            }

            string dist = "";
            if (o.where != null && player != null)
            {
                Vector3 w = o.where();
                if (!float.IsInfinity(w.x))
                    dist = "   (" + Mathf.RoundToInt(Vector3.Distance(player.transform.position, w)) + " m)";
            }

            o.row.text = tag + " " + o.text + "  " + o.cur + "/" + o.max + dist;
        }

        float k = Mathf.Clamp01(composure / 100f);
        composureFill.sizeDelta = new Vector2(BarWidth * k, 0f);
        composureFill.GetComponent<Image>().color = Color.Lerp(new Color(0.95f, 0.25f, 0.2f), new Color(0.4f, 0.9f, 0.4f), k);
    }

    void Toast(string msg, Color color, float seconds)
    {
        if (toastText == null) return;
        toastColor = color;
        toastText.text = msg;
        toastText.color = color;
        toastText.gameObject.SetActive(true);
        toastUntil = Time.time + seconds;
    }

    // ====================================================================== PRESS E
    void HandleInteraction()
    {
        if (promptText == null) return;

        ExamInteractable it = (finished || failed) ? null : ExamInteractable.Nearest(player.transform.position);
        if (it == null)
        {
            if (promptText.gameObject.activeSelf) promptText.gameObject.SetActive(false);
            return;
        }

        promptText.text = "[E]  " + it.verb;
        if (!promptText.gameObject.activeSelf) promptText.gameObject.SetActive(true);

        if (Input.GetKeyDown(KeyCode.E))
        {
            string t = it.Use();
            if (!string.IsNullOrEmpty(t)) Think(t);
        }
    }

    void Think(string text)
    {
        thoughtText.text = "<i>" + text + "</i>";
        thoughtBg.gameObject.SetActive(true);
        thoughtUntil = Time.time + Mathf.Clamp(2.5f + text.Length * 0.05f, 3.5f, 8f);
    }

#if UNITY_EDITOR
    // ====================================================================== EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;

        UnityEditor.EditorApplication.delayCall -= EditorScan;
        UnityEditor.EditorApplication.delayCall += EditorScan;
    }

    // Reads the AI prefab's Animator Controller whenever something changes in the inspector
    void EditorScan()
    {
        if (this == null || ai == null) return;
        if (ai.Scan()) UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Re-scan AI Animator")]
    void RescanAI()
    {
        ai.scanSignature = "";
        EditorScan();
    }

    // Called when the component is first added: look for the AI prefab and set everything up automatically
    void Reset()
    {
        ai.TryAutoFindPrefab();
        ai.scanSignature = "";
        ai.Scan();
    }

    [ContextMenu("Auto-Setup AI Prefab (find prefab + animations)")]
    void AutoSetupAI()
    {
        ai.TryAutoFindPrefab();
        ai.scanSignature = "";
        EditorScan();
    }

    [UnityEditor.MenuItem("Tools/Art Exam/Add Director To Scene")]
    static void AddToScene()
    {
        ArtExamDirector existing = Object.FindObjectOfType<ArtExamDirector>();
        if (existing != null)
        {
            UnityEditor.Selection.activeObject = existing.gameObject;
            return;
        }

        GameObject go = new GameObject("ArtExamDirector");
        go.AddComponent<ArtExamDirector>();   // Reset() runs now and auto-sets up the AI prefab
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Add ArtExamDirector");
        UnityEditor.Selection.activeObject = go;
    }
#endif

    void ShowBanner(string title, string sub, Color color)
    {
        banner.SetActive(true);
        bannerTitle.text = title;
        bannerTitle.color = color;
        bannerSub.text = sub;
    }

    static ArtExamDirector instance;

    public class PulseTarget
    {
        public int order;
        public string tag;
        public Color color;
        public System.Func<Vector3> where;
    }

    /// Active objectives that currently have a real position (used by the minimap pulse).
    public static string PulseBlockReason { get; private set; }

    public static void CollectPulseTargets(List<PulseTarget> result)
    {
        result.Clear();
        PulseBlockReason = null;

        if (instance == null) instance = Object.FindObjectOfType<ArtExamDirector>();
        ArtExamDirector d = instance;

        if (d == null) { PulseBlockReason = "no ArtExamDirector in the scene"; return; }
        if (d.finished || d.failed) { PulseBlockReason = "exam already finished or failed"; return; }
        if (d.player == null) { PulseBlockReason = "director has no player yet"; return; }

        for (int i = 0; i < d.objectives.Count; i++)
        {
            Objective o = d.objectives[i];
            if (o.Done || o.where == null) continue;
            if (float.IsInfinity(o.where().x)) continue;
            result.Add(new PulseTarget { order = i, tag = o.tag, color = o.color, where = o.where });
        }

        if (result.Count == 0)
            PulseBlockReason = "no active objective has a position yet (" + d.objectives.Count + " objectives)";
    }

    Vector3 NearestPaintingPosition()
    {
        Vector3 best = Vector3.positiveInfinity;
        float bestD = float.MaxValue;
        foreach (PaintingCanvas p in paintings)
        {
            if (p == null || donePaintings.Contains(p)) continue;
            float d = (p.transform.position - player.transform.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = p.transform.position; }
        }
        return best;
    }
}