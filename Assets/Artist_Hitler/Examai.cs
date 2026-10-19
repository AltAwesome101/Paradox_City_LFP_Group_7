using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UEA = UnityEditor.Animations;
#endif



[System.Serializable]
public class ExamParamValue
{
    public string name;
    public int kind;        // 0 float, 1 int, 2 bool, 3 trigger
    public float value;
}

[System.Serializable]
public class ExamAIConfig
{
    [Tooltip("Drag your animated character prefab here. Used for rivals and the registration clerk. Empty = simple built-in figures.")]
    public GameObject prefab;
    [Tooltip("Extra scale applied to the model.")]
    public float scale = 1f;
    [Tooltip("Rotate the model if it walks sideways / backwards (degrees).")]
    public float yawOffset = 0f;
    [Tooltip("Approx. height of the model in metres at scale 1 (markers float above this).")]
    public float headHeight = 1.9f;
    [Tooltip("ON: the director reads your prefab + controller itself (finds the idle / run / attack / hit / death states, "
           + "measures the model's height). Turn OFF if you want to type those values yourself.")]
    public bool autoSetup = true;

    [Header("Animator (names match your controller)")]
    public int animatorLayer = 0;
    public string idleState = "IdleBreatheS&S";
    public string runState = "RunForwardS&S";
    public string attackState = "SwordAttack1S&S";
    public string hitState = "GetHitLightFrontS&S";
    public string deathState = "DeathBack1hMelee";
    public float attackTime = 0.9f;
    public float hitTime = 0.7f;

    // filled automatically by the Editor scan of the controller
    [HideInInspector] public ExamParamValue[] idleHold = new ExamParamValue[0];
    [HideInInspector] public ExamParamValue[] runHold = new ExamParamValue[0];
    [HideInInspector] public string scanSignature = "";

    public bool HasPrefab { get { return prefab != null; } }

#if UNITY_EDITOR
    /// <summary>
    /// Reads the prefab's Animator Controller and works out which parameter values keep the animator
    /// in the idle / run state (taken from the Idle&lt;-&gt;Run transition conditions). Returns true if anything changed.
    /// </summary>
    public bool Scan()
    {
        List<ExamParamValue> toRun = new List<ExamParamValue>();
        List<ExamParamValue> toIdle = new List<ExamParamValue>();
        string log = "no prefab";
        string setupBefore = SetupSignature();

        if (prefab != null)
        {
            if (autoSetup) MeasureHeight();

            Animator a = prefab.GetComponentInChildren<Animator>(true);
            UEA.AnimatorController ctrl = null;

            if (a != null && a.runtimeAnimatorController != null)
            {
                ctrl = a.runtimeAnimatorController as UEA.AnimatorController;
                if (ctrl == null)
                {
                    AnimatorOverrideController ov = a.runtimeAnimatorController as AnimatorOverrideController;
                    if (ov != null) ctrl = ov.runtimeAnimatorController as UEA.AnimatorController;
                }
            }

            if (ctrl == null || ctrl.layers.Length == 0)
            {
                log = "prefab has no Animator Controller";
                Debug.LogWarning("[ExamAI] The AI prefab has no Animator / Animator Controller - it will be shown without animations.");
            }
            else
            {
                int layer = Mathf.Clamp(animatorLayer, 0, ctrl.layers.Length - 1);
                UEA.AnimatorStateMachine root = ctrl.layers[layer].stateMachine;
                if (autoSetup) DetectNames(root);

                Dictionary<string, AnimatorControllerParameterType> types = new Dictionary<string, AnimatorControllerParameterType>();
                foreach (UnityEngine.AnimatorControllerParameter p in ctrl.parameters) types[p.name] = p.type;

                UEA.AnimatorState idle = FindState(root, idleState);
                UEA.AnimatorState run = FindState(root, runState);

                if (idle == null || run == null)
                {
                    log = "states not found";
                    Debug.LogWarning("[ExamAI] Couldn't find the states '" + idleState + "' / '" + runState
                                     + "' in the controller. Check the names under ArtExamDirector > Ai.");
                }
                else
                {
                    CollectConditions(idle, run, types, toRun);
                    CollectConditions(run, idle, types, toIdle);

                    log = "idle needs: " + Describe(toIdle) + " | run needs: " + Describe(toRun);
                }
            }
        }

        bool setupChanged = setupBefore != SetupSignature();
        if (log == scanSignature && idleHold != null && runHold != null && !setupChanged) return false;

        scanSignature = log;
        idleHold = toIdle.ToArray();
        runHold = toRun.ToArray();
        Debug.Log("[ExamAI] Auto setup -> idle '" + idleState + "', run '" + runState + "', attack '" + attackState
                  + "', hit '" + hitState + "', death '" + deathState + "', height " + headHeight.ToString("0.00") + " m");
        Debug.Log("[ExamAI] Animator scan: " + log);
        return true;
    }

    string SetupSignature()
    {
        return idleState + "|" + runState + "|" + attackState + "|" + hitState + "|" + deathState + "|" + headHeight.ToString("0.00");
    }

    // ------------------------------------------------------------ auto setup: state names
    /// <summary>Finds the idle / run / attack / hit / death states by keyword. Names that already exist in the controller are kept.</summary>
    void DetectNames(UEA.AnimatorStateMachine root)
    {
        List<string> names = new List<string>();
        CollectNames(root, names);

        idleState = Pick(names, idleState, new[] { "idle" }, new[] { "breathe", "stand" }, new[] { "crouch", "combo", "attack", "hit", "death" });
        runState = Pick(names, runState, new[] { "run" }, new[] { "forward" }, new[] { "back", "left", "right", "strafe", "attack", "jump" });
        attackState = Pick(names, attackState, new[] { "attack" }, new[] { "1", "light" }, new[] { "combo", "run", "heavy" });
        hitState = Pick(names, hitState, new[] { "gethit", "hit", "damage" }, new[] { "light", "front" }, new[] { "attack" });
        deathState = Pick(names, deathState, new[] { "death", "die", "dead" }, new[] { "back" }, new string[0]);
    }

    static void CollectNames(UEA.AnimatorStateMachine m, List<string> result)
    {
        foreach (UEA.ChildAnimatorState cs in m.states) result.Add(cs.state.name);
        foreach (UEA.ChildAnimatorStateMachine sub in m.stateMachines) CollectNames(sub.stateMachine, result);
    }

    static string Pick(List<string> names, string current, string[] anyOf, string[] prefer, string[] avoid)
    {
        if (!string.IsNullOrEmpty(current) && names.Contains(current)) return current;   // already valid: leave it

        string best = current;
        int bestScore = 0;
        foreach (string n in names)
        {
            string l = n.ToLowerInvariant();

            bool match = false;
            foreach (string k in anyOf) if (l.Contains(k)) { match = true; break; }
            if (!match) continue;

            int score = 10;
            foreach (string k in prefer) if (l.Contains(k)) score += 3;
            foreach (string k in avoid) if (l.Contains(k)) score -= 5;
            score -= n.Length / 20;                                                      // shorter names win ties

            if (score > bestScore)
            {
                bestScore = score;
                best = n;
            }
        }
        return best;
    }

    // ------------------------------------------------------------ auto setup: model height
    void MeasureHeight()
    {
        GameObject inst = UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (inst == null) return;
        inst.hideFlags = HideFlags.HideAndDontSave;

        Bounds b = new Bounds();
        bool any = false;
        foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue;
            if (!any) { b = r.bounds; any = true; }
            else b.Encapsulate(r.bounds);
        }
        Object.DestroyImmediate(inst);

        if (any && b.size.y > 0.3f && b.size.y < 10f)
            headHeight = Mathf.Round(b.size.y * 100f) / 100f;
    }

    // ------------------------------------------------------------ auto setup: find the prefab in the project
    static UEA.AnimatorController GetController(Animator a)
    {
        if (a == null || a.runtimeAnimatorController == null) return null;
        UEA.AnimatorController ctrl = a.runtimeAnimatorController as UEA.AnimatorController;
        if (ctrl == null)
        {
            AnimatorOverrideController ov = a.runtimeAnimatorController as AnimatorOverrideController;
            if (ov != null) ctrl = ov.runtimeAnimatorController as UEA.AnimatorController;
        }
        return ctrl;
    }

    /// <summary>
    /// If no prefab is assigned, searches the project for a prefab whose Animator Controller has the idle, run and death
    /// states (the names from your controller) and assigns it. Returns true if one was assigned.
    /// </summary>
    public bool TryAutoFindPrefab()
    {
        if (prefab != null) return false;

        List<GameObject> matches = new List<GameObject>();
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Packages/")) continue;

            GameObject go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            UEA.AnimatorController ctrl = GetController(go.GetComponentInChildren<Animator>(true));
            if (ctrl == null) continue;

            foreach (UEA.AnimatorControllerLayer layer in ctrl.layers)
            {
                if (FindState(layer.stateMachine, idleState) != null
                    && FindState(layer.stateMachine, runState) != null
                    && FindState(layer.stateMachine, deathState) != null)
                {
                    matches.Add(go);
                    break;
                }
            }
        }

        if (matches.Count == 0)
        {
            Debug.Log("[ExamAI] No prefab in the project has the states '" + idleState + "', '" + runState + "' and '" + deathState
                      + "'. Drag your character prefab into Ai > Prefab by hand.");
            return false;
        }

        prefab = matches[0];
        string others = "";
        for (int i = 1; i < matches.Count; i++) others += " " + matches[i].name;
        Debug.Log("[ExamAI] Auto-assigned the AI prefab '" + prefab.name + "'."
                  + (matches.Count > 1 ? " (Other matches:" + others + " - drag a different one in if this isn't the right model.)" : ""));
        return true;
    }

    static void CollectConditions(UEA.AnimatorState from, UEA.AnimatorState to,
                                  Dictionary<string, AnimatorControllerParameterType> types, List<ExamParamValue> result)
    {
        // transitions are OR'd - the first one that has conditions is enough
        foreach (UEA.AnimatorStateTransition t in from.transitions)
        {
            if (t.destinationState != to || t.conditions.Length == 0) continue;
            foreach (UEA.AnimatorCondition c in t.conditions) result.Add(Satisfy(c, types));
            return;
        }
    }

    static ExamParamValue Satisfy(UEA.AnimatorCondition c, Dictionary<string, AnimatorControllerParameterType> types)
    {
        AnimatorControllerParameterType pt;
        if (!types.TryGetValue(c.parameter, out pt)) pt = AnimatorControllerParameterType.Float;

        ExamParamValue v = new ExamParamValue();
        v.name = c.parameter;

        switch (pt)
        {
            case AnimatorControllerParameterType.Int: v.kind = 1; break;
            case AnimatorControllerParameterType.Bool: v.kind = 2; break;
            case AnimatorControllerParameterType.Trigger: v.kind = 3; break;
            default: v.kind = 0; break;
        }

        switch (c.mode)
        {
            case UEA.AnimatorConditionMode.If: v.value = 1f; break;
            case UEA.AnimatorConditionMode.IfNot: v.value = 0f; break;
            case UEA.AnimatorConditionMode.Greater: v.value = c.threshold + 1f; break;
            case UEA.AnimatorConditionMode.Less: v.value = c.threshold > 0.01f ? 0f : c.threshold - 1f; break;
            case UEA.AnimatorConditionMode.Equals: v.value = c.threshold; break;
            case UEA.AnimatorConditionMode.NotEqual: v.value = c.threshold + 1f; break;
        }
        return v;
    }

    static string Describe(List<ExamParamValue> list)
    {
        if (list.Count == 0) return "(nothing)";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (ExamParamValue v in list) sb.Append(v.name).Append("=").Append(v.value).Append(" ");
        return sb.ToString();
    }

    static UEA.AnimatorState FindState(UEA.AnimatorStateMachine m, string name)
    {
        foreach (UEA.ChildAnimatorState cs in m.states)
            if (cs.state.name == name) return cs.state;

        foreach (UEA.ChildAnimatorStateMachine sub in m.stateMachines)
        {
            UEA.AnimatorState r = FindState(sub.stateMachine, name);
            if (r != null) return r;
        }
        return null;
    }
#endif
}


public class ExamActorAnim : MonoBehaviour
{
    enum Mode { None, Idle, Run }

    Animator animator;
    ExamAIConfig cfg;
    int layer;
    bool ready, dead;
    Mode desired = Mode.None, playing = Mode.None;
    int idleH, runH, attackH, hitH, deathH;
    float busyUntil, lastPlay, driftSince = -1f;

    readonly HashSet<int> validParams = new HashSet<int>();
    readonly List<int> heurFloats = new List<int>();
    readonly List<int> heurBools = new List<int>();

    static readonly string[] MoveWords = { "speed", "move", "velocity", "run", "walk", "forward", "locomotion" };

    public static ExamActorAnim Create(ExamAIConfig cfg, Transform parent, string name)
    {
        GameObject model = Object.Instantiate(cfg.prefab, parent);
        model.name = name;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(0f, cfg.yawOffset, 0f);
        model.transform.localScale = model.transform.localScale * cfg.scale;

        // the model is only a visual: switch off anything that could act on its own
        foreach (Collider c in model.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (Rigidbody rb in model.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
        foreach (NavMeshAgent ag in model.GetComponentsInChildren<NavMeshAgent>(true)) ag.enabled = false;
        foreach (CharacterController cc in model.GetComponentsInChildren<CharacterController>(true)) cc.enabled = false;
        foreach (MonoBehaviour mb in model.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;

        ExamActorAnim a = model.AddComponent<ExamActorAnim>();
        a.cfg = cfg;
        return a;
    }

    void Start()
    {
        animator = GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("[ExamAI] The AI prefab has no Animator Controller - it will stand still.");
            return;
        }

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        layer = Mathf.Clamp(cfg.animatorLayer, 0, Mathf.Max(0, animator.layerCount - 1));

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            validParams.Add(p.nameHash);

            string n = p.name.ToLowerInvariant();
            bool moveish = false;
            for (int i = 0; i < MoveWords.Length; i++)
                if (n.Contains(MoveWords[i])) { moveish = true; break; }

            if (moveish && p.type == AnimatorControllerParameterType.Float) heurFloats.Add(p.nameHash);
            if (moveish && p.type == AnimatorControllerParameterType.Bool) heurBools.Add(p.nameHash);
        }

        idleH = Resolve(cfg.idleState);
        runH = Resolve(cfg.runState);
        attackH = Resolve(cfg.attackState);
        hitH = Resolve(cfg.hitState);
        deathH = Resolve(cfg.deathState);

        ready = true;
    }

    int Resolve(string state)
    {
        if (string.IsNullOrEmpty(state)) return 0;

        string layerName = animator.GetLayerName(layer);
        int full = Animator.StringToHash(layerName + "." + state);
        if (animator.HasState(layer, full)) return full;

        int bare = Animator.StringToHash(state);
        if (animator.HasState(layer, bare)) return bare;

        Debug.LogWarning("[ExamAI] State '" + state + "' not found in the AI controller (layer " + layer
                         + "). Fix the name under ArtExamDirector > Ai.");
        return 0;
    }

    // ------------------------------------------------------------------ API
    public void Idle() { if (!dead) desired = Mode.Idle; }
    public void Run() { if (!dead) desired = Mode.Run; }
    public void Attack() { PlayOnce(attackH, cfg.attackTime, 0.08f); }
    public void Hit() { PlayOnce(hitH, cfg.hitTime, 0.05f); }

    public void Die()
    {
        if (dead) return;
        dead = true;
        desired = Mode.None;
        if (!ready || deathH == 0) return;

        animator.CrossFadeInFixedTime(deathH, 0.1f, layer);
        StartCoroutine(FreezeAfterDeath());
    }

    void PlayOnce(int hash, float duration, float fade)
    {
        if (!ready || dead || hash == 0) return;
        animator.CrossFadeInFixedTime(hash, fade, layer);
        busyUntil = Time.time + duration;
        playing = Mode.None;
        lastPlay = Time.time;
    }

    // ------------------------------------------------------------------ update
    void Update()
    {
        if (!ready || dead || desired == Mode.None) return;

        ApplyHold(desired, false);
        if (Time.time < busyUntil) return;

        int target = desired == Mode.Run ? runH : idleH;
        if (target == 0) return;

        if (playing != desired)
        {
            Enter(desired, target);
            return;
        }

        // safety net: if the controller's own transitions pulled us out of the state, put us back
        if (animator.IsInTransition(layer) || Time.time < lastPlay + 0.4f)
        {
            driftSince = -1f;
            return;
        }

        string stateName = desired == Mode.Run ? cfg.runState : cfg.idleState;
        if (animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName))
        {
            driftSince = -1f;
            return;
        }

        if (driftSince < 0f) driftSince = Time.time;
        if (Time.time - driftSince > 0.5f)
        {
            driftSince = -1f;
            Enter(desired, target);
        }
    }

    void Enter(Mode m, int hash)
    {
        ApplyHold(m, true);
        animator.CrossFadeInFixedTime(hash, 0.15f, layer);
        playing = m;
        lastPlay = Time.time;
    }

    void ApplyHold(Mode m, bool fireTriggers)
    {
        ExamParamValue[] rules = m == Mode.Run ? cfg.runHold : cfg.idleHold;
        bool anyRules = (cfg.runHold != null && cfg.runHold.Length > 0) || (cfg.idleHold != null && cfg.idleHold.Length > 0);

        if (anyRules)
        {
            if (rules == null) return;
            for (int i = 0; i < rules.Length; i++)
            {
                ExamParamValue r = rules[i];
                int h = Animator.StringToHash(r.name);
                if (!validParams.Contains(h)) continue;

                switch (r.kind)
                {
                    case 0: animator.SetFloat(h, r.value); break;
                    case 1: animator.SetInteger(h, Mathf.RoundToInt(r.value)); break;
                    case 2: animator.SetBool(h, r.value > 0.5f); break;
                    case 3: if (fireTriggers) animator.SetTrigger(h); break;
                }
            }
            return;
        }

        // no scan data: guess from parameter names (speed / move / run ...)
        float v = m == Mode.Run ? 1f : 0f;
        for (int i = 0; i < heurFloats.Count; i++) animator.SetFloat(heurFloats[i], v);
        for (int i = 0; i < heurBools.Count; i++) animator.SetBool(heurBools[i], m == Mode.Run);
    }

    IEnumerator FreezeAfterDeath()
    {
        float t = 0f;
        yield return new WaitForSeconds(0.2f);

        while (t < 6f)
        {
            t += Time.deltaTime;
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layer);
            if (!animator.IsInTransition(layer) && info.IsName(cfg.deathState) && info.normalizedTime >= 0.97f)
            {
                animator.speed = 0f;      // stay lying down
                yield break;
            }
            yield return null;
        }
    }
}