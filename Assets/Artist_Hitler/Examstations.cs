using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class ExamTarget : MonoBehaviour, IDamageable
{
    public abstract WeaponType Required { get; }
    public abstract string ToolName { get; }

    protected virtual bool Accepts { get { return true; } }
    protected abstract void OnCorrectHit(float amount, Vector3 point);

    float lastWrong = -10f;
    Collider cachedCol;

    
    public void TakeDamage(float amount)
    {
        if (!Accepts) return;

        Vector3 p = HitPoint();
        if (ArtExamDirector.CurrentWeaponType == Required)
        {
            OnCorrectHit(amount, p);
            return;
        }

        if (Time.time - lastWrong < 1.2f) return;
        lastWrong = Time.time;
        OnWrongTool(ArtExamDirector.CurrentWeaponType, p);
    }

    protected virtual void OnWrongTool(WeaponType used, Vector3 p)
    {
        ExamLabel.Pop(p + Vector3.up * 0.6f, "Wrong tool! Needs the " + ToolName, new Color(1f, 0.55f, 0.3f));
        ExamAudio.Play(ExamAudio.Thud(), p, 0.6f);
    }

    protected virtual Vector3 HitPoint()
    {
        if (cachedCol == null) cachedCol = GetComponentInChildren<Collider>();
        return cachedCol != null ? cachedCol.bounds.center : transform.position;
    }
}

// ===========================================================================
//  BAT: marble sculpture
// ===========================================================================
public class ExamMarbleBlock : ExamTarget
{
    public override WeaponType Required { get { return WeaponType.Fist; } }
    public override string ToolName { get { return "BAT (key 1)"; } }
    protected override bool Accepts { get { return !Done; } }

    public int hitsNeeded = 8;
    public int Hits { get; private set; }
    public bool Done { get; private set; }
    public System.Action<int, int> OnProgress;

    static readonly Color Marble = new Color(0.88f, 0.88f, 0.9f);
    static readonly Color Gold = new Color(0.95f, 0.75f, 0.2f);

    Transform block, statue;
    Vector3 restPos, baseScale;
    Material blockMat;
    ExamMarker marker;
    float shakeUntil, lastHit;

    public static ExamMarbleBlock Create(Vector3 ground, float yaw)
    {
        GameObject root = new GameObject("Station_MarbleSculpture");
        root.transform.SetPositionAndRotation(ground, Quaternion.Euler(0f, yaw, 0f));

        ExamUtil.Prim(PrimitiveType.Cube, "Pedestal", root.transform, new Vector3(0f, 0.3f, 0f),
                      new Vector3(1.7f, 0.6f, 1.7f), new Color(0.25f, 0.25f, 0.28f), Vector3.zero, true);
        GameObject block = ExamUtil.Prim(PrimitiveType.Cube, "MarbleBlock", root.transform, new Vector3(0f, 1.45f, 0f),
                                         new Vector3(1.2f, 1.7f, 1.2f), Marble, Vector3.zero, true, 0.1f, 0.6f);

        ExamMarbleBlock mb = root.AddComponent<ExamMarbleBlock>();
        mb.block = block.transform;
        mb.restPos = block.transform.localPosition;
        mb.baseScale = block.transform.localScale;
        mb.blockMat = block.GetComponent<Renderer>().material;   
        mb.marker = ExamMarker.Create(root.transform, new Vector3(0f, 3.3f, 0f), ExamUtil.WeaponColor(WeaponType.Fist), false);

        ExamInteractable it = root.AddComponent<ExamInteractable>();
        it.radius = 3.5f;
        it.verb = "Inspect the marble";
        it.thought = () => mb.Done
            ? "A fine bust, if I say so myself."
            : "A solid block of marble. A good swing of the bat should chip it down.";

        return mb;
    }

    protected override Vector3 HitPoint()
    {
        return block != null ? block.position : transform.position + Vector3.up * 1.5f;
    }

    protected override void OnCorrectHit(float amount, Vector3 point)
    {
        if (Time.time - lastHit < 0.3f) return;
        lastHit = Time.time;

        Hits++;
        float t = Mathf.Clamp01(Hits / (float)hitsNeeded);

        
        block.localScale = new Vector3(baseScale.x * (1f - 0.45f * t), baseScale.y * (1f - 0.4f * t), baseScale.z * (1f - 0.45f * t));
        restPos = new Vector3(restPos.x, 0.6f + block.localScale.y * 0.5f, restPos.z);
        block.localPosition = restPos;
        blockMat.color = Color.Lerp(Marble, new Color(0.9f, 0.85f, 0.7f), t);

        ExamFx.Burst(point, Marble, 9, 4f, 0.09f, 3f);
        ExamAudio.Play(ExamAudio.Chip(), point, 0.9f);
        shakeUntil = Time.time + 0.2f;
        ExamLabel.Pop(point + Vector3.up * 1.2f, Mathf.Min(Hits, hitsNeeded) + "/" + hitsNeeded, ExamUtil.WeaponColor(WeaponType.Fist));

        if (OnProgress != null) OnProgress(Hits, hitsNeeded);
        if (Hits >= hitsNeeded) StartCoroutine(Reveal());
    }

    IEnumerator Reveal()
    {
        Done = true;
        if (marker != null) marker.Remove();

        ExamFx.Burst(block.position, Marble, 30, 7f, 0.14f, 4f);
        ExamAudio.Play(ExamAudio.Boom(), block.position, 0.4f);
        ExamLabel.Pop(block.position + Vector3.up * 1.4f, "SCULPTED!", new Color(0.4f, 1f, 0.5f));
        Destroy(block.gameObject);

        
        GameObject s = new GameObject("Statue");
        statue = s.transform;
        statue.SetParent(transform, false);
        statue.localPosition = new Vector3(0f, 0.6f, 0f);

        ExamUtil.Prim(PrimitiveType.Cylinder, "Plinth", statue, new Vector3(0f, 0.15f, 0f), new Vector3(0.7f, 0.15f, 0.7f), Gold, Vector3.zero, false, 0.8f, 0.7f);
        ExamUtil.Prim(PrimitiveType.Cube, "Shoulders", statue, new Vector3(0f, 0.55f, 0f), new Vector3(0.75f, 0.45f, 0.4f), Gold, Vector3.zero, false, 0.8f, 0.7f);
        ExamUtil.Prim(PrimitiveType.Cylinder, "Neck", statue, new Vector3(0f, 0.9f, 0f), new Vector3(0.2f, 0.12f, 0.2f), Gold, Vector3.zero, false, 0.8f, 0.7f);
        ExamUtil.Prim(PrimitiveType.Sphere, "Head", statue, new Vector3(0f, 1.2f, 0f), new Vector3(0.5f, 0.55f, 0.5f), Gold, Vector3.zero, false, 0.8f, 0.7f);
        ExamUtil.Prim(PrimitiveType.Cylinder, "Beret", statue, new Vector3(0.05f, 1.5f, 0f), new Vector3(0.5f, 0.04f, 0.5f), new Color(0.7f, 0.55f, 0.1f), new Vector3(0f, 0f, 8f), false, 0.8f, 0.7f);

        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            statue.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, t / 0.6f);
            yield return null;
        }
    }

    void Update()
    {
        if (block != null && Time.time < shakeUntil)
            block.localPosition = restPos + Random.insideUnitSphere * 0.035f;

        if (statue != null) statue.Rotate(0f, 35f * Time.deltaTime, 0f);
    }
}

// ===========================================================================
//  RIFLE: rival applicants (uses the AI prefab if one is assigned)
// ===========================================================================
public class ExamRival : ExamTarget
{
    public override WeaponType Required { get { return WeaponType.Rifle; } }
    public override string ToolName { get { return "RIFLE (key 2)"; } }
    protected override bool Accepts { get { return state != State.Down; } }

    enum State { Approach, Retreat, Down }
    State state = State.Approach;

    public System.Action<ExamRival> OnSabotage;
    public System.Action<ExamRival> OnDisqualified;

    static readonly Color[] Shirts =
    {
        new Color(0.55f, 0.2f, 0.2f), new Color(0.2f, 0.35f, 0.55f),
        new Color(0.25f, 0.45f, 0.3f), new Color(0.45f, 0.3f, 0.5f)
    };
    static readonly Color[] Paints =
    {
        new Color(0.95f, 0.25f, 0.25f), new Color(0.25f, 0.55f, 0.95f),
        new Color(0.95f, 0.8f, 0.2f), new Color(0.35f, 0.85f, 0.4f)
    };
    static readonly float[] Turns = { 0f, 45f, -45f, 90f, -90f, 135f, -135f };

    Transform target, visual, stars;
    NavMeshAgent agent;
    ExamActorAnim actor;        
    ExamMarker marker;
    float bodyHeight = 1.95f;
    Color paint;
    const float Speed = 3.6f;
    Vector3 retreatPoint;
    float retreatUntil, nextSabotageOk, dodgeUntil;
    bool moving;

    
    const float StuckLimit = 3f;        
    const float MinProgress = 0.15f;    
    Vector3 lastCheckPos;
    float nextStuckCheck, stuckTime;

    public static ExamRival Create(Vector3 ground, int variant, Transform target)
    {
        GameObject root = new GameObject("RivalApplicant_" + (variant + 1));
        root.transform.position = ground;

        Color paintColor = Paints[variant % Paints.Length];

        GameObject v = new GameObject("Visual");
        v.transform.SetParent(root.transform, false);
        Transform vt = v.transform;

        ExamAIConfig cfg = ArtExamDirector.AI;
        ExamActorAnim actor = null;
        float height = 1.95f;

        if (cfg != null && cfg.HasPrefab)
        {
            actor = ExamActorAnim.Create(cfg, vt, "RivalModel");
            height = cfg.headHeight * cfg.scale;
        }
        else
        {
            
            Color shirt = Shirts[variant % Shirts.Length];
            ExamUtil.Prim(PrimitiveType.Capsule, "Body", vt, new Vector3(0f, 0.85f, 0f), new Vector3(0.6f, 0.85f, 0.6f), shirt, Vector3.zero, false);
            ExamUtil.Prim(PrimitiveType.Sphere, "Head", vt, new Vector3(0f, 1.78f, 0f), new Vector3(0.28f, 0.3f, 0.28f), new Color(0.9f, 0.75f, 0.62f), Vector3.zero, false);
            ExamUtil.Prim(PrimitiveType.Cylinder, "Beret", vt, new Vector3(0.03f, 1.95f, 0f), new Vector3(0.38f, 0.03f, 0.38f), new Color(0.12f, 0.12f, 0.14f), new Vector3(0f, 0f, 6f), false);
            ExamUtil.Prim(PrimitiveType.Cylinder, "Bucket", vt, new Vector3(0.4f, 1.0f, 0.3f), new Vector3(0.22f, 0.11f, 0.22f), new Color(0.5f, 0.5f, 0.55f), Vector3.zero, false, 0.6f, 0.5f);
            ExamUtil.Prim(PrimitiveType.Cylinder, "BucketPaint", vt, new Vector3(0.4f, 1.115f, 0.3f), new Vector3(0.19f, 0.01f, 0.19f), paintColor, Vector3.zero, false);
            ExamUtil.Prim(PrimitiveType.Cube, "Portfolio", vt, new Vector3(0f, 1.05f, -0.23f), new Vector3(0.45f, 0.55f, 0.05f), new Color(0.35f, 0.25f, 0.15f), Vector3.zero, false);
        }

        CapsuleCollider cc = root.AddComponent<CapsuleCollider>();
        cc.height = height;
        cc.center = new Vector3(0f, height * 0.5f, 0f);
        cc.radius = Mathf.Min(0.38f * Mathf.Max(1f, height / 1.9f), height * 0.45f);

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        ExamRival r = root.AddComponent<ExamRival>();
        r.visual = vt;
        r.actor = actor;
        r.target = target;
        r.paint = paintColor;
        r.bodyHeight = height;
        r.marker = ExamMarker.Create(root.transform, new Vector3(0f, height + 0.6f, 0f), ExamUtil.WeaponColor(WeaponType.Rifle), false);
        return r;
    }

    void Start()
    {
        
        TryCreateAgent();

        lastCheckPos = transform.position;
        nextStuckCheck = Time.time + 2f;

        if (actor != null) actor.Idle();
    }

    
    void TryCreateAgent()
    {
        if (agent != null) return;

        NavMeshHit nh;
        if (!NavMesh.SamplePosition(transform.position, out nh, 1.5f, NavMesh.AllAreas)) return;

        transform.position = nh.position;
        agent = gameObject.AddComponent<NavMeshAgent>();
        agent.speed = Speed;
        agent.radius = 0.35f;
        agent.height = 1.8f;
        agent.angularSpeed = 360f;
        agent.acceleration = 12f;
    }

    void Update()
    {
        if (stars != null) stars.Rotate(0f, 360f * Time.deltaTime, 0f);
        if (state == State.Down) return;
        if (target == null) return;

        moving = false;

        if (state == State.Approach)
        {
            Vector3 to = target.position - transform.position;
            to.y = 0f;
            if (to.magnitude <= 2.6f && Time.time >= nextSabotageOk) Sabotage();
            else if (to.magnitude > 2.6f) Move(target.position, Speed);
        }
        else
        {
            Vector3 to = retreatPoint - transform.position;
            to.y = 0f;
            if (to.magnitude < 1.5f || Time.time > retreatUntil) state = State.Approach;
            else Move(retreatPoint, Speed * 1.25f);
        }

        Watchdog();
        Animate();
    }

   
    void Watchdog()
    {
        if (Time.time < nextStuckCheck) return;
        nextStuckCheck = Time.time + 0.5f;

        Vector3 p = transform.position;
        Vector3 tp = target.position;

        Vector3 flatToTarget = tp - p;
        flatToTarget.y = 0f;

        
        bool lost = p.y < tp.y - 12f || p.y > tp.y + 25f || flatToTarget.magnitude > 90f;
        if (!lost && agent != null && agent.enabled && !agent.isOnNavMesh) lost = true;

        if (lost)
        {
            Respawn("left the level");
            return;
        }

        
        Vector3 moved = p - lastCheckPos;
        moved.y = 0f;
        lastCheckPos = p;

        if (!moving)
        {
            stuckTime = 0f;
            return;
        }

        bool noPath = agent != null && agent.enabled && agent.isOnNavMesh
                      && !agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathInvalid;

        if (moved.magnitude < MinProgress || noPath) stuckTime += 0.5f;
        else stuckTime = 0f;

        if (stuckTime >= StuckLimit) Respawn("stuck");
    }

    
    void Respawn(string reason)
    {
        Vector3 c = target.position;

        Vector3 spot;
        if (!ExamUtil.FindNavSpot(c, 18f, 30f, c.y, out spot))
            spot = ExamUtil.FindSpot(c, 18f, 30f, 0.8f, c.y);

        Debug.Log("[ArtExam] " + name + " was " + reason + " - respawning at " + spot);

        bool warped = false;
        if (agent != null && agent.enabled) warped = agent.Warp(spot);
        if (!warped) transform.position = spot;

        if (agent == null) TryCreateAgent();                     
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.ResetPath();

        state = State.Approach;
        stuckTime = 0f;
        lastCheckPos = transform.position;
        nextStuckCheck = Time.time + 1.5f;
    }

    void Move(Vector3 dest, float spd)
    {
        moving = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = spd;
            agent.isStopped = false;
            agent.SetDestination(dest);
            return;
        }

        Vector3 flat = dest - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.01f) return;

        Vector3 dir = flat.normalized;
        Vector3 chest = transform.position + Vector3.up * 0.9f;

        bool free = false;
        for (int i = 0; i < Turns.Length; i++)
        {
            Vector3 d = Quaternion.Euler(0f, Turns[i], 0f) * dir;
            if (!Physics.Raycast(chest, d, 0.9f, ~0, QueryTriggerInteraction.Ignore))
            {
                dir = d;
                free = true;
                break;
            }
        }
        if (!free) return;

        Vector3 next = transform.position + dir * spd * Time.deltaTime;
        Vector3 g;
        if (ExamUtil.Ground(next, transform.position.y, out g)) next.y = g.y;

        transform.position = next;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }

    void Animate()
    {
        if (actor != null)
        {
            if (moving) actor.Run();
            else actor.Idle();
            return;
        }

        
        float bob = moving ? Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.07f : 0f;
        float hop = 0f;
        if (Time.time < dodgeUntil)
            hop = Mathf.Sin(Mathf.Clamp01((dodgeUntil - Time.time) / 0.4f) * Mathf.PI) * 0.45f;

        visual.localPosition = new Vector3(0f, bob + hop, 0f);
    }

    void Sabotage()
    {
        Vector3 at = target.position + Vector3.up * 1.6f;
        ExamFx.Burst(at, paint, 14, 5f, 0.12f, 2f);
        ExamLabel.Pop(at + Vector3.up * 0.6f, "SPLASH!", paint);
        ExamAudio.Play(ExamAudio.Splat(), at, 1f);

        if (actor != null) actor.Attack();

        nextSabotageOk = Time.time + 3f;

        
        Vector3 rp;
        if (!ExamUtil.FindNavSpot(target.position, 20f, 30f, target.position.y, out rp))
            rp = ExamUtil.FindSpot(target.position, 20f, 30f, 0.6f, target.position.y);
        retreatPoint = rp;

        retreatUntil = Time.time + 8f;
        state = State.Retreat;

        if (OnSabotage != null) OnSabotage(this);
    }

    protected override void OnWrongTool(WeaponType used, Vector3 p)
    {
        if (actor != null) actor.Hit();
        else dodgeUntil = Time.time + 0.4f;

        ExamLabel.Pop(p + Vector3.up * 0.9f, "Ha! Missed! (needs the rifle)", new Color(1f, 0.55f, 0.3f));
    }

    protected override void OnCorrectHit(float amount, Vector3 point)
    {
        state = State.Down;

        if (marker != null) marker.Remove();
        if (agent != null) Destroy(agent);
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        ExamFx.Burst(point, paint, 18, 6f, 0.11f, 2.5f);
        ExamAudio.Play(ExamAudio.Splat(), point, 1f);
        ExamLabel.Pop(point + Vector3.up * 1f, "DISQUALIFIED", new Color(1f, 0.85f, 0.25f));

        GameObject sg = new GameObject("Stars");
        stars = sg.transform;
        stars.SetParent(transform, false);

        if (actor != null)
        {
            actor.Die();                                              
            stars.localPosition = new Vector3(0f, 0.6f, -0.9f * Mathf.Max(1f, bodyHeight / 1.9f));
        }
        else
        {
            
            for (int i = 0; i < 7; i++)
            {
                Vector2 c = Random.insideUnitCircle.normalized * 0.3f;
                ExamUtil.Prim(PrimitiveType.Sphere, "Splat", visual, new Vector3(c.x, Random.Range(0.6f, 1.5f), c.y),
                              new Vector3(0.2f, 0.2f, 0.2f) * Random.Range(0.7f, 1.2f), paint, Vector3.zero, false);
            }
            stars.localPosition = new Vector3(0f, 0.35f, 1.7f);
            StartCoroutine(Fall());
        }

        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            ExamUtil.Prim(PrimitiveType.Sphere, "Star", stars, new Vector3(Mathf.Cos(a) * 0.25f, 0f, Mathf.Sin(a) * 0.25f),
                          Vector3.one * 0.1f, new Color(1f, 0.92f, 0.3f), Vector3.zero, false);
        }

        if (OnDisqualified != null) OnDisqualified(this);
        Destroy(gameObject, 14f);
    }

    IEnumerator Fall()
    {
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            visual.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 90f, t / 0.35f), 0f, 0f);
            visual.localPosition = Vector3.zero;
            yield return null;
        }
        visual.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }
}

// ===========================================================================
//  BAZOOKA: wall of rejection letters
// ===========================================================================
public class ExamRejectionWall : ExamTarget
{
    public override WeaponType Required { get { return WeaponType.Bazooka; } }
    public override string ToolName { get { return "BAZOOKA (key 3)"; } }
    protected override bool Accepts { get { return !Cleared; } }

    public float hp = 200f;
    public bool Cleared { get; private set; }
    public System.Action OnCleared;

    readonly List<Transform> bricks = new List<Transform>();
    ExamMarker marker;
    float maxHp;
    float shakeUntil;

    public static ExamRejectionWall Create(Vector3 ground, Vector3 lookAt)
    {
        Vector3 fwd = lookAt - ground;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;

        GameObject root = new GameObject("Station_RejectionWall");
        root.transform.SetPositionAndRotation(ground, Quaternion.LookRotation(fwd.normalized));

        ExamRejectionWall w = root.AddComponent<ExamRejectionWall>();
        w.maxHp = w.hp;

        const int cols = 7, rows = 5;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                bool stamped = (r * cols + c) % 6 == 3;
                Color col = stamped ? new Color(0.85f, 0.25f, 0.2f)
                                    : Color.Lerp(new Color(0.8f, 0.8f, 0.78f), new Color(0.97f, 0.97f, 0.95f), Random.value);

                GameObject b = ExamUtil.Prim(PrimitiveType.Cube, "RejectionLetters", root.transform,
                                             new Vector3((c - (cols - 1) * 0.5f) * 1.0f, 0.3f + r * 0.6f, 0f),
                                             new Vector3(0.98f, 0.58f, 0.45f), col,
                                             new Vector3(0f, Random.Range(-3f, 3f), 0f), true);
                w.bricks.Add(b.transform);
            }
        }

        
        Color stone = new Color(0.35f, 0.34f, 0.33f);
        float half = cols * 0.5f + 0.4f;
        ExamUtil.Prim(PrimitiveType.Cylinder, "PillarL", root.transform, new Vector3(-half, 2.2f, 0f), new Vector3(0.8f, 2.2f, 0.8f), stone, Vector3.zero, true);
        ExamUtil.Prim(PrimitiveType.Cylinder, "PillarR", root.transform, new Vector3(half, 2.2f, 0f), new Vector3(0.8f, 2.2f, 0.8f), stone, Vector3.zero, true);
        ExamUtil.Prim(PrimitiveType.Cube, "Lintel", root.transform, new Vector3(0f, 4.5f, 0f), new Vector3(cols + 2f, 0.55f, 1.0f), stone, Vector3.zero, true);

        w.marker = ExamMarker.Create(root.transform, new Vector3(0f, 6.0f, 0f), ExamUtil.WeaponColor(WeaponType.Bazooka), false);

        ExamInteractable it = root.AddComponent<ExamInteractable>();
        it.radius = 7f;
        it.verb = "Inspect the wall";
        it.thought = () => w.Cleared
            ? "The gate is open. Hitler can finally get through."
            : "Rejection letters, stacked into a wall. It'll take real firepower to bring this down.";

        return w;
    }

    protected override Vector3 HitPoint()
    {
        return transform.position + Vector3.up * 1.5f;
    }

    protected override void OnCorrectHit(float amount, Vector3 point)
    {
        hp -= amount;
        shakeUntil = Time.time + 0.35f;

        ExamFx.Burst(point + transform.forward * 0.5f, new Color(0.95f, 0.95f, 0.9f), 28, 6f, 0.16f, 3f);

        if (hp <= 0f)
        {
            Collapse();
            return;
        }

        ExamLabel.Pop(point + Vector3.up * 1.5f, "CRACK!  " + Mathf.RoundToInt(100f * hp / maxHp) + "%", new Color(1f, 0.8f, 0.3f));
    }

    void Collapse()
    {
        Cleared = true;
        if (marker != null) marker.Remove();

        Collider playerCol = ArtExamDirector.PlayerCollider;
        Vector3 src = transform.position + transform.forward * 1.5f + Vector3.up * 1f;   

        foreach (Transform b in bricks)
        {
            if (b == null) continue;

            Collider bc = b.GetComponent<Collider>();
            if (playerCol != null && bc != null) Physics.IgnoreCollision(bc, playerCol);

            Rigidbody rb = b.gameObject.AddComponent<Rigidbody>();
            rb.mass = 3f;
            rb.AddExplosionForce(1500f, src, 14f, 0.8f);
            rb.AddTorque(Random.onUnitSphere * 6f, ForceMode.VelocityChange);
            Destroy(b.gameObject, 8f);
        }

        ExamFx.Burst(transform.position + Vector3.up * 1.5f, new Color(0.95f, 0.95f, 0.9f), 40, 9f, 0.18f, 4f);
        ExamAudio.Play(ExamAudio.Boom(), transform.position, 1f);
        ExamLabel.Pop(transform.position + Vector3.up * 3f, "THE GATE IS OPEN!", new Color(0.4f, 1f, 0.5f));

        if (OnCleared != null) OnCleared();
    }

    void Update()
    {
        if (Cleared || Time.time >= shakeUntil) return;

        float k = (shakeUntil - Time.time) / 0.35f;
        foreach (Transform b in bricks)
            if (b != null) b.localPosition += new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * 0.01f * k;
    }
}

// ===========================================================================
//  STONE: gongs and the registration desk
// ===========================================================================
public class ExamGong : ExamTarget
{
    public override WeaponType Required { get { return WeaponType.Stone; } }
    public override string ToolName { get { return "thrown STONE (key 4)"; } }

    public int index;
    public Vector3 StandPoint;
    public System.Action<ExamGong> OnRung;
    public ExamInteractable interact;

    Transform pivot;
    Material discMat;
    float lastRing = -10f, swingStart = -10f;

    static readonly Color Brass = new Color(0.85f, 0.62f, 0.2f);
    static readonly Color Dull = new Color(0.5f, 0.4f, 0.22f);

    public static ExamGong Create(Vector3 ground, int index, Vector3 faceTowards)
    {
        Vector3 fwd = faceTowards - ground;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;

        GameObject root = new GameObject("Station_Gong" + index);
        root.transform.SetPositionAndRotation(ground, Quaternion.LookRotation(fwd.normalized));

        Color wood = new Color(0.4f, 0.27f, 0.15f);
        ExamUtil.Prim(PrimitiveType.Cylinder, "PostL", root.transform, new Vector3(-1.2f, 1.8f, 0f), new Vector3(0.2f, 1.8f, 0.2f), wood, Vector3.zero, true);
        ExamUtil.Prim(PrimitiveType.Cylinder, "PostR", root.transform, new Vector3(1.2f, 1.8f, 0f), new Vector3(0.2f, 1.8f, 0.2f), wood, Vector3.zero, true);
        ExamUtil.Prim(PrimitiveType.Cube, "Crossbar", root.transform, new Vector3(0f, 3.55f, 0f), new Vector3(2.7f, 0.16f, 0.18f), wood, Vector3.zero, true);

        GameObject pv = new GameObject("Pivot");
        pv.transform.SetParent(root.transform, false);
        pv.transform.localPosition = new Vector3(0f, 3.47f, 0f);

        ExamUtil.Prim(PrimitiveType.Cylinder, "Rope", pv.transform, new Vector3(0f, -0.45f, 0f), new Vector3(0.03f, 0.45f, 0.03f), new Color(0.6f, 0.5f, 0.35f), Vector3.zero, false);

        
        GameObject disc = ExamUtil.Prim(PrimitiveType.Cylinder, "GongDisc", pv.transform, new Vector3(0f, -1.25f, 0f),
                                        new Vector3(1.4f, 0.05f, 1.4f), Dull, new Vector3(90f, 0f, 0f), true, 0.85f, 0.7f);

        ExamGong g = disc.AddComponent<ExamGong>();
        g.index = index;
        g.pivot = pv.transform;
        g.discMat = disc.GetComponent<Renderer>().material;

        ExamInteractable it = root.AddComponent<ExamInteractable>();
        it.radius = 3.5f;
        it.verb = "Inspect the gong";
        g.interact = it;
        return g;
    }

    
    public void SetCurrent(bool current)
    {
        if (discMat != null) discMat.color = current ? Brass : Dull;
    }

    protected override void OnCorrectHit(float amount, Vector3 point) { }

    protected override void OnWrongTool(WeaponType used, Vector3 p)
    {
        ExamLabel.Pop(p + Vector3.up * 0.8f, "Only a thrown stone rings this!", new Color(1f, 0.55f, 0.3f));
        ExamAudio.Play(ExamAudio.Thud(), p, 0.5f);
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.collider.GetComponentInParent<StoneProjectile>() == null) return;
        if (c.relativeVelocity.magnitude < 2.5f) return;
        if (Time.time - lastRing < 1.5f) return;
        lastRing = Time.time;
        swingStart = Time.time;

        Vector3 p = transform.position;
        ExamAudio.Play(ExamAudio.Gong(), p, 1f);
        ExamLabel.Pop(p + Vector3.up * 1f, "BONNNG!", Brass);

        if (OnRung != null) OnRung(this);
    }

    void Update()
    {
        float t = Time.time - swingStart;
        if (pivot != null && t >= 0f && t < 4f)
        {
            float a = Mathf.Sin(t * 5f) * 18f * Mathf.Exp(-t * 1.1f);
            pivot.localRotation = Quaternion.Euler(a, 0f, a * 0.4f);
        }
    }
}

public class ExamDesk : MonoBehaviour
{
    public Vector3 StandPoint;
    public bool Registered { get; private set; }
    ExamMarker marker;

    public static ExamDesk Create(Vector3 ground, Vector3 faceTowards)
    {
        Vector3 fwd = faceTowards - ground;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;

        GameObject root = new GameObject("Station_RegistrationDesk");
        root.transform.SetPositionAndRotation(ground, Quaternion.LookRotation(fwd.normalized));

        Color wood = new Color(0.45f, 0.3f, 0.17f);
        ExamUtil.Prim(PrimitiveType.Cube, "Desk", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(2.4f, 0.9f, 1f), wood, Vector3.zero, true);
        ExamUtil.Prim(PrimitiveType.Cube, "Papers", root.transform, new Vector3(0.4f, 0.93f, 0f), new Vector3(0.5f, 0.05f, 0.7f), new Color(0.95f, 0.95f, 0.9f), new Vector3(0f, 12f, 0f), false);
        ExamUtil.Prim(PrimitiveType.Cylinder, "InkWell", root.transform, new Vector3(-0.6f, 0.95f, 0.1f), new Vector3(0.12f, 0.05f, 0.12f), new Color(0.05f, 0.05f, 0.12f), Vector3.zero, false);

        
        const float clerkZ = -1.0f;
        float head = 1.95f;
        ExamAIConfig cfg = ArtExamDirector.AI;

        if (cfg != null && cfg.HasPrefab)
        {
            GameObject holder = new GameObject("Clerk");
            holder.transform.SetParent(root.transform, false);
            holder.transform.localPosition = new Vector3(0f, 0f, clerkZ);

            ExamActorAnim clerk = ExamActorAnim.Create(cfg, holder.transform, "ClerkModel");
            clerk.Idle();
            head = cfg.headHeight * cfg.scale;
        }
        else
        {
            ExamUtil.Prim(PrimitiveType.Capsule, "Clerk", root.transform, new Vector3(0f, 0.95f, clerkZ), new Vector3(0.6f, 0.85f, 0.6f), new Color(0.3f, 0.3f, 0.35f), Vector3.zero, false);
            ExamUtil.Prim(PrimitiveType.Sphere, "ClerkHead", root.transform, new Vector3(0f, 1.88f, clerkZ), new Vector3(0.28f, 0.3f, 0.28f), new Color(0.9f, 0.75f, 0.62f), Vector3.zero, false);
        }

        ExamDesk d = root.AddComponent<ExamDesk>();
        d.marker = ExamMarker.Create(root.transform, new Vector3(0f, head + 0.55f, clerkZ), ExamUtil.WeaponColor(WeaponType.Stone), true);   // yellow "!"

        ExamInteractable it = root.AddComponent<ExamInteractable>();
        it.radius = 4.5f;
        it.verb = "Inspect the desk";
        it.thought = () => d.Registered
            ? "Hitler is registered. Only the acceptance letter is left."
            : "This is where Hitler needs to be once I finish all my tasks.";

        Vector3 stand;
        Vector3 want = ground + fwd.normalized * 2.4f;
        d.StandPoint = ExamUtil.Ground(want, ground.y, out stand) ? stand : want;
        return d;
    }

    public void Complete()
    {
        Registered = true;
        if (marker != null) marker.Remove();

        ExamLabel.Pop(transform.position + Vector3.up * 3f, "ENROLLED!", new Color(0.4f, 1f, 0.5f));
        ExamFx.Burst(transform.position + Vector3.up * 2f, new Color(1f, 0.85f, 0.25f), 24, 5f, 0.1f, 3f);
        ExamFx.Burst(transform.position + Vector3.up * 2f, new Color(0.4f, 1f, 0.5f), 16, 5f, 0.1f, 3f);
        ExamAudio.Play(ExamAudio.Pop(), transform.position, 1f);
    }
}


public class ExamStoneCourse : MonoBehaviour
{
    public ExamGong[] gongs = new ExamGong[3];
    public ExamDesk desk;
    public int Step { get; private set; }          
    public System.Action<int> OnProgress;
    public System.Action OnDone;

    ExamHitlerLure lure;
    bool walking;

    public static ExamStoneCourse Build(Vector3 hitlerPos, float refY, ExamHitlerLure lure)
    {
        GameObject root = new GameObject("Station_GongCourse");
        ExamStoneCourse c = root.AddComponent<ExamStoneCourse>();
        c.lure = lure;

        Vector3 prev = hitlerPos;
        for (int i = 0; i < 3; i++)
        {
            Vector3 spot = ExamUtil.FindSpotLos(prev, 9f, 12f, 1.8f, refY, prev);
            ExamGong g = ExamGong.Create(spot, i + 1, prev);

            Vector3 toPrev = prev - spot;
            toPrev.y = 0f;
            toPrev.Normalize();
            Vector3 want = spot + toPrev * 2.6f;
            Vector3 stand;
            g.StandPoint = ExamUtil.Ground(want, refY, out stand) ? stand : want;

            g.OnRung += c.HandleRung;
            g.interact.thought = () => c.GongThought(g);
            c.gongs[i] = g;
            prev = spot;
        }

        Vector3 deskSpot = ExamUtil.FindSpotLos(prev, 5f, 7f, 1.8f, refY, prev);
        c.desk = ExamDesk.Create(deskSpot, prev);

        c.RefreshHighlights();
        return c;
    }

    public Vector3 CurrentTargetPosition
    {
        get
        {
            if (Step < 3 && gongs[Step] != null) return gongs[Step].transform.position;
            return Vector3.positiveInfinity;
        }
    }

    public string CurrentTargetName
    {
        get { return Step < 3 ? "GONG " + (Step + 1) + "\nSTONE 4" : ""; }
    }

    string GongThought(ExamGong g)
    {
        if (Step >= 3) return "That gong did its job. Hitler is all set.";

        if (g.index == Step + 1)
            return Step == 0
                ? "Maybe I can use this gong to lure Hitler. A thrown stone should ring it."
                : "Another gong. Ring it and Hitler should follow the sound.";

        if (g.index > Step + 1) return "Another gong... I should ring them in order.";
        return "Hitler already followed this one.";
    }

    void RefreshHighlights()
    {
        for (int i = 0; i < 3; i++)
            if (gongs[i] != null) gongs[i].SetCurrent(i == Step);
    }

    void HandleRung(ExamGong g)
    {
        if (Step >= 3 || walking) return;

        Vector3 p = g.transform.position;
        if (g.index != Step + 1)
        {
            ExamLabel.Pop(p + Vector3.up * 1.6f, "Not that one - ring gong " + (Step + 1) + " first", new Color(1f, 0.9f, 0.4f));
            return;
        }

        NoiseSystem.Emit(p, 35f);   

        walking = true;
        Vector3 dest = Step < 2 ? g.StandPoint : desk.StandPoint;   

        if (lure != null)
        {
            ExamLabel.Pop(p + Vector3.up * 1.6f, "Hitler heard it!", new Color(0.6f, 1f, 0.7f));
            lure.GoTo(dest, Arrived);
        }
        else
        {
            Arrived();
        }
    }

    void Arrived()
    {
        walking = false;
        Step++;

        if (Step >= 3) desk.Complete();
        RefreshHighlights();

        if (OnProgress != null) OnProgress(Step);
        if (Step >= 3 && OnDone != null) OnDone();
    }
}


public class ExamHitlerLure : MonoBehaviour
{
    const float WalkSpeed = 2.6f;
    const float Timeout = 35f;

    static readonly string[] FloatNames = { "speed", "movementvalue", "movespeed", "moveamount", "velocity", "walkspeed" };
    static readonly string[] BoolNames = { "iswalking", "walking", "walk", "ismoving", "moving" };

    NavMeshAgent agent;
    CharacterController cc;
    Animator anim;
    MonoBehaviour brain;

    readonly List<int> floatParams = new List<int>();
    readonly List<int> boolParams = new List<int>();

    bool active, brainWasOn;
    Vector3 dest;
    float startTime;
    System.Action onArrive;

    bool UseAgent { get { return agent != null && agent.enabled && agent.isOnNavMesh; } }

    public static ExamHitlerLure Attach(Transform hitler)
    {
        ExamHitlerLure l = hitler.GetComponent<ExamHitlerLure>();
        if (l == null) l = hitler.gameObject.AddComponent<ExamHitlerLure>();
        return l;
    }

    void Awake()
    {
        agent = GetComponentInChildren<NavMeshAgent>();
        if (agent == null) agent = GetComponentInParent<NavMeshAgent>();
        cc = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
        brain = GetComponent<HitlerNPC>();

        if (anim != null && anim.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter p in anim.parameters)
            {
                string n = p.name.ToLowerInvariant();
                if (p.type == AnimatorControllerParameterType.Float && System.Array.IndexOf(FloatNames, n) >= 0) floatParams.Add(p.nameHash);
                if (p.type == AnimatorControllerParameterType.Bool && System.Array.IndexOf(BoolNames, n) >= 0) boolParams.Add(p.nameHash);
            }
        }
    }

    public void GoTo(Vector3 point, System.Action arrived)
    {
        if (!active && brain != null)
        {
            brainWasOn = brain.enabled;
            brain.enabled = false;     
        }

        active = true;
        dest = point;
        startTime = Time.time;
        onArrive = arrived;

        if (UseAgent)
        {
            NavMeshHit nh;
            Vector3 d = NavMesh.SamplePosition(point, out nh, 4f, NavMesh.AllAreas) ? nh.position : point;
            agent.isStopped = false;
            agent.speed = WalkSpeed;
            agent.stoppingDistance = 0.4f;
            agent.SetDestination(d);
        }
    }

    void Update()
    {
        if (!active) return;

        Vector3 flat = dest - transform.position;
        flat.y = 0f;
        float dist = flat.magnitude;

        if (dist < 1.3f)
        {
            Finish();
            return;
        }

        if (Time.time - startTime > Timeout)
        {
            Warp();       
            Finish();
            return;
        }

        float norm;
        if (UseAgent)
        {
            norm = Mathf.Clamp01(agent.velocity.magnitude / WalkSpeed);
        }
        else
        {
            Vector3 dir = flat / dist;
            Vector3 step = dir * WalkSpeed * Time.deltaTime;

            if (cc != null && cc.enabled)
            {
                cc.Move(step + Vector3.down * 0.2f);
            }
            else
            {
                Vector3 n = transform.position + step;
                Vector3 g;
                if (ExamUtil.Ground(n, transform.position.y, out g)) n.y = g.y;
                transform.position = n;
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
            norm = 1f;
        }

        SetAnim(norm);
    }

    void Warp()
    {
        if (UseAgent)
        {
            agent.Warp(dest);
            return;
        }

        bool ccWas = cc != null && cc.enabled;
        if (cc != null) cc.enabled = false;
        transform.position = dest;
        if (cc != null) cc.enabled = ccWas;
    }

    void Finish()
    {
        active = false;
        SetAnim(0f);
        if (UseAgent) agent.ResetPath();
        if (brain != null) brain.enabled = brainWasOn;

        System.Action cb = onArrive;
        onArrive = null;
        if (cb != null) cb();
    }

    void SetAnim(float norm)
    {
        if (anim == null) return;
        for (int i = 0; i < floatParams.Count; i++) anim.SetFloat(floatParams[i], norm);
        for (int i = 0; i < boolParams.Count; i++) anim.SetBool(boolParams[i], norm > 0.1f);
    }
}


public class ExamCrate : MonoBehaviour
{
    static readonly List<ExamCrate> all = new List<ExamCrate>();

    WeaponType type;
    int amount;
    Transform spin;

    public static int CountOf(WeaponType t)
    {
        int n = 0;
        for (int i = 0; i < all.Count; i++)
            if (all[i] != null && all[i].type == t) n++;
        return n;
    }

    public static ExamCrate Create(Vector3 ground, WeaponType type, int amount)
    {
        GameObject root = new GameObject("AmmoCrate_" + type);
        root.transform.position = ground;

        Color tint = ExamUtil.WeaponColor(type);
        GameObject box = ExamUtil.Prim(PrimitiveType.Cube, "Box", root.transform, new Vector3(0f, 0.55f, 0f),
                                       new Vector3(0.6f, 0.45f, 0.45f), new Color(0.5f, 0.35f, 0.2f), Vector3.zero, false);
        ExamUtil.Prim(PrimitiveType.Cube, "Band", box.transform, new Vector3(0f, 0.2f, 0f),
                      new Vector3(1.02f, 0.18f, 1.02f), tint, Vector3.zero, false);

        ExamCrate c = root.AddComponent<ExamCrate>();
        c.type = type;
        c.amount = amount;
        c.spin = box.transform;

        string line;
        switch (type)
        {
            case WeaponType.Rifle: line = "Rifle rounds. These will come in handy."; break;
            case WeaponType.Bazooka: line = "Rockets for the bazooka. I'll make them count."; break;
            default: line = "A pile of stones. Good for throwing."; break;
        }

        ExamInteractable it = root.AddComponent<ExamInteractable>();
        it.radius = 2.5f;
        it.verb = "Pick up";
        it.thought = () => line;
        it.onUse = c.Take;
        return c;
    }

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    void Take()
    {
        ArtExamDirector.GiveAmmo(type, amount);

        string unit = type == WeaponType.Bazooka ? "rockets" : (type == WeaponType.Stone ? "stones" : "rounds");
        ExamLabel.Pop(transform.position + Vector3.up * 1.6f, "+" + amount + " " + unit, ExamUtil.WeaponColor(type));
        ExamAudio.Play(ExamAudio.Pop(), transform.position, 0.8f);
        Destroy(gameObject);
    }

    void Update()
    {
        spin.Rotate(0f, 90f * Time.deltaTime, 0f);
    }
}