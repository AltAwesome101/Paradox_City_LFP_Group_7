using System.Collections.Generic;
using UnityEngine;

using System.Text.RegularExpressions;

public class NpcOutfit : MonoBehaviour
{
    public bool verbose;
    public bool Built { get; private set; }

    private readonly List<GameObject> _before = new List<GameObject>();
    private readonly List<GameObject> _after = new List<GameObject>();
    private readonly List<Mesh> _meshes = new List<Mesh>();
    private bool _completed;

    private static readonly Dictionary<int, Material> s_materials = new Dictionary<int, Material>();
    private static Material s_poofMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_materials.Clear();
        s_poofMaterial = null;
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

    public void Build(OutfitLook before, OutfitLook after, Material template, int seed)
    {
        if (Built) return;

        
        Animator anim = GetComponentInChildren<Animator>();
        var rig = new Rig(anim, transform);

        if (!rig.valid)
        {
            Debug.LogWarning("[NpcOutfit] '" + name + "': couldn't find these bones: " + rig.missing +
                             "\nBones in this model: " + rig.boneNames, this);
            return;
        }

        if (verbose) Debug.Log(rig.Report(name), this);

        var body = new Body(gameObject, rig);
        if (verbose) Debug.Log(body.Describe(name), this);

        Construct(before, "Before", rig, body, seed, template, _before);
        Construct(after, "After", rig, body, seed + 7919, template, _after);

        Built = true;
        ApplyVisibility();
    }

    public void SetCompleted(bool completed, bool playEffect = false)
    {
        bool changed = completed != _completed;
        _completed = completed;
        if (!Built) return;

        ApplyVisibility();
        if (changed && playEffect) SpawnPoof();
    }

    private void ApplyVisibility()
    {
        foreach (var go in _before) if (go != null) go.SetActive(!_completed);
        foreach (var go in _after) if (go != null) go.SetActive(_completed);
    }

    private void OnDestroy()
    {
        foreach (var m in _meshes) if (m != null) Destroy(m);
    }

    private void Construct(OutfitLook look, string label, Rig rig, Body body, int seed,
                           Material template, List<GameObject> output)
    {
        if (look == null) return;

        var builder = new Builder(look, rig, body, new System.Random(seed));
        builder.Run();

        foreach (var kv in builder.meshes)
        {
            Transform bone = kv.Key;
            MeshBuilder mb = kv.Value;
            if (bone == null || mb.verts.Count == 0) continue;

            var go = new GameObject("Outfit_" + label + "_" + bone.name);
            go.layer = gameObject.layer;
            go.transform.SetParent(bone, false);

            Mesh mesh = mb.ToMesh(bone);
            mesh.name = go.name;
            _meshes.Add(mesh);

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();

            var mats = new Material[mb.keyOrder.Count];
            for (int i = 0; i < mats.Length; i++) mats[i] = GetMaterial(mb.keyOrder[i], template);
            mr.sharedMaterials = mats;

            output.Add(go);
        }
    }

    // =========================================================
    // MATERIALS / EFFECT
    // =========================================================

    private static int Key(Color c)
    {
        Color32 q = c;
        return q.r | (q.g << 8) | (q.b << 16);
    }

    private static Color Unkey(int k)
    {
        return new Color32((byte)(k & 255), (byte)((k >> 8) & 255), (byte)((k >> 16) & 255), 255);
    }

    private static Material GetMaterial(int key, Material template)
    {
        if (s_materials.TryGetValue(key, out Material cached) && cached != null) return cached;

        Material mat;
        if (template != null)
        {
            mat = new Material(template);
        }
        else
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            mat = new Material(sh);
        }

        Color c = Unkey(key);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.2f);
        mat.name = "Outfit_" + key.ToString("X6");

        s_materials[key] = mat;
        return mat;
    }

    private void SpawnPoof()
    {
        var fx = new GameObject("OutfitChangeFX");
        fx.transform.position = transform.position + Vector3.up;

        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = 0.7f;
        main.startSpeed = 2.5f;
        main.startSize = 0.35f;
        main.startColor = new Color(1f, 0.92f, 0.65f, 0.85f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.1f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)30) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        if (s_poofMaterial == null)
        {
            Shader sh = Shader.Find("Particles/Standard Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh != null) s_poofMaterial = new Material(sh);
        }

        if (s_poofMaterial != null)
            fx.GetComponent<ParticleSystemRenderer>().sharedMaterial = s_poofMaterial;

        ps.Play();
        Destroy(fx, 2f);
    }

    // =========================================================
    // SKELETON
    // =========================================================

    
        private class Rig
        {
            public bool valid;
            public string missing = "";
            public string boneNames = "";
            public bool usedAvatar;

            public Transform hips, spine, chest, upperChest, neck, head;
            public readonly Transform[] shoulder = new Transform[2];
            public readonly Transform[] upperArm = new Transform[2];
            public readonly Transform[] lowerArm = new Transform[2];
            public readonly Transform[] hand = new Transform[2];
            public readonly Transform[] upperLeg = new Transform[2];
            public readonly Transform[] lowerLeg = new Transform[2];
            public readonly Transform[] foot = new Transform[2];
            public readonly Transform[] toes = new Transform[2];
            public Vector3 right, fwd;

            // ---------- name matching ----------

            private class Info
            {
                public Transform t;
                public string joined;
                public string[] tokens;
                public int side;      
                public int depth;
                public bool ignore;

                public bool Has(string s) { return joined.Contains(s); }

                public bool HasAny(params string[] list)
                {
                    foreach (string s in list)
                        if (joined.Contains(s)) return true;
                    return false;
                }
            }

            
            private static readonly Regex s_split =
                new Regex(@"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])|[^A-Za-z0-9]+");

            private static readonly HashSet<string> s_ignore = new HashSet<string>
        {
            "end", "nub", "twist", "roll", "ik", "pole", "ctrl", "ctl", "control",
            "helper", "target", "jiggle", "corrective", "weapon", "prop"
        };

            private static Info Analyse(Transform t, Transform root)
            {
                string n = t.name;
                int colon = n.LastIndexOf(':');
                if (colon >= 0 && colon < n.Length - 1) n = n.Substring(colon + 1);

                var tokens = new List<string>();
                foreach (string p in s_split.Split(n))
                    if (p.Length > 0) tokens.Add(p.ToLowerInvariant());

                string joined = string.Concat(tokens);

                int side = 0;
                foreach (string tk in tokens)
                {
                    if (tk == "left" || tk == "l") { side = -1; break; }
                    if (tk == "right" || tk == "r") { side = 1; break; }
                }
                if (side == 0)
                {
                    if (joined.StartsWith("left")) side = -1;
                    else if (joined.StartsWith("right")) side = 1;
                }

                bool ignore = false;
                foreach (string tk in tokens)
                    if (s_ignore.Contains(tk)) { ignore = true; break; }

                int depth = 0;
                for (Transform p = t.parent; p != null && p != root; p = p.parent) depth++;

                return new Info
                {
                    t = t,
                    joined = joined,
                    tokens = tokens.ToArray(),
                    side = side,
                    depth = depth,
                    ignore = ignore
                };
            }

            private static Transform Shallowest(List<Info> pool, System.Func<Info, bool> pred)
            {
                Transform best = null;
                int bestDepth = int.MaxValue;
                foreach (Info i in pool)
                    if (i.depth < bestDepth && pred(i)) { best = i.t; bestDepth = i.depth; }
                return best;
            }

            private static List<Info> Collect(List<Info> pool, System.Func<Info, bool> pred)
            {
                var r = new List<Info>();
                foreach (Info i in pool) if (pred(i)) r.Add(i);
                return r;
            }

            private static bool IsLowerArm(Info i) { return i.HasAny("forearm", "lowerarm", "lowarm"); }
            private static bool IsUpperLeg(Info i) { return i.HasAny("thigh", "upleg", "upperleg"); }

            private static bool IsLowerLeg(Info i)
            {
                return i.HasAny("calf", "shin", "lowerleg", "lowleg") ||
                       (i.Has("leg") && !IsUpperLeg(i) && !i.HasAny("foot", "toe"));
            }

            

            public Rig(Animator a, Transform root)
            {
                if (a != null && a.isHuman)
                {
                    usedAvatar = true;
                    MapFromAvatar(a);
                }
                else
                {
                    MapFromNames(root);
                }

                missing = ListMissing();
                valid = missing.Length == 0;
                if (!valid) return;

                if (neck == null) neck = head;

                
                right = upperArm[1].position - upperArm[0].position;
                right.y = 0f;
                if (right.sqrMagnitude < 1e-6f) right = Vector3.right;
                right.Normalize();
                fwd = Vector3.Cross(right, Vector3.up).normalized;
            }

            private void MapFromAvatar(Animator a)
            {
                hips = a.GetBoneTransform(HumanBodyBones.Hips);
                spine = a.GetBoneTransform(HumanBodyBones.Spine);
                chest = a.GetBoneTransform(HumanBodyBones.Chest);
                upperChest = a.GetBoneTransform(HumanBodyBones.UpperChest);
                neck = a.GetBoneTransform(HumanBodyBones.Neck);
                head = a.GetBoneTransform(HumanBodyBones.Head);

                for (int s = 0; s < 2; s++)
                {
                    bool l = s == 0;
                    shoulder[s] = a.GetBoneTransform(l ? HumanBodyBones.LeftShoulder : HumanBodyBones.RightShoulder);
                    upperArm[s] = a.GetBoneTransform(l ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
                    lowerArm[s] = a.GetBoneTransform(l ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
                    hand[s] = a.GetBoneTransform(l ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
                    upperLeg[s] = a.GetBoneTransform(l ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
                    lowerLeg[s] = a.GetBoneTransform(l ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
                    foot[s] = a.GetBoneTransform(l ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
                    toes[s] = a.GetBoneTransform(l ? HumanBodyBones.LeftToes : HumanBodyBones.RightToes);
                }
            }

            private void MapFromNames(Transform root)
            {
                
                var skeletonSet = new HashSet<Transform>();
                foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    foreach (Transform b in smr.bones)
                        for (Transform t = b; t != null && t != root && t.IsChildOf(root); t = t.parent)
                            if (!skeletonSet.Add(t)) break;

                var skeleton = new List<Info>();
                var others = new List<Info>();   

                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t == root) continue;

                    Info info = Analyse(t, root);
                    if (info.ignore) continue;

                    if (skeletonSet.Contains(t)) skeleton.Add(info);
                    else if (t.GetComponent<Renderer>() == null) others.Add(info);
                }

                Transform Find(System.Func<Info, bool> pred)
                {
                    Transform r = Shallowest(skeleton, pred);
                    if (r != null) return r;
                    return Shallowest(others, pred);
                }

                
                System.Func<Info, bool> isSpine = i => i.side == 0 && i.HasAny("spine", "chest", "torso");
                List<Info> spines = Collect(skeleton, isSpine);
                if (spines.Count == 0) spines = Collect(others, isSpine);
                spines.Sort((x, y) => x.depth.CompareTo(y.depth));

                if (spines.Count > 0) spine = spines[0].t;
                if (spines.Count > 1) chest = spines[spines.Count / 2].t;
                if (spines.Count > 2) upperChest = spines[spines.Count - 1].t;

                hips = Find(i => i.side == 0 && i.HasAny("hip", "pelvis"));
                neck = Find(i => i.side == 0 && i.Has("neck"));
                head = Find(i => i.side == 0 && i.Has("head") && !i.Has("headtop"));

                for (int s = 0; s < 2; s++)
                {
                    int sd = s == 0 ? -1 : 1;

                    shoulder[s] = Find(i => i.side == sd && i.HasAny("shoulder", "clavicle"));

                    hand[s] = Find(i => i.side == sd && i.Has("hand") &&
                                        !i.HasAny("thumb", "index", "middle", "ring", "pinky", "finger"));

                    lowerArm[s] = Find(i => i.side == sd && IsLowerArm(i));

                    upperArm[s] = Find(i => i.side == sd && !IsLowerArm(i) &&
                                            (i.Has("upperarm") ||
                                             (i.Has("arm") && !i.HasAny("hand", "shoulder", "clavicle", "armpit"))));

                    upperLeg[s] = Find(i => i.side == sd && IsUpperLeg(i));
                    lowerLeg[s] = Find(i => i.side == sd && IsLowerLeg(i));

                    foot[s] = Find(i => i.side == sd && i.HasAny("foot", "ankle") && !i.HasAny("toe", "ball"));
                    toes[s] = Find(i => i.side == sd && i.HasAny("toe", "ball"));
                }

                
                var names = new List<string>();
                foreach (Info i in skeleton)
                {
                    if (names.Count >= 60) break;
                    names.Add(i.t.name);
                }
                boneNames = string.Join(", ", names);
            }

            private string ListMissing()
            {
                var m = new List<string>();
                if (hips == null) m.Add("hips");
                if (spine == null) m.Add("spine");
                if (head == null) m.Add("head");

                string[] sn = { "left", "right" };
                for (int s = 0; s < 2; s++)
                {
                    if (upperArm[s] == null) m.Add(sn[s] + " upper arm");
                    if (lowerArm[s] == null) m.Add(sn[s] + " lower arm");
                    if (upperLeg[s] == null) m.Add(sn[s] + " upper leg");
                    if (lowerLeg[s] == null) m.Add(sn[s] + " lower leg");
                    if (foot[s] == null) m.Add(sn[s] + " foot");
                }
                return string.Join(", ", m);
            }

            public Vector3 HandPos(int s)
            {
                return hand[s] != null
                    ? hand[s].position
                    : lowerArm[s].position + (lowerArm[s].position - upperArm[s].position);
            }

            private static string N(Transform t) { return t != null ? t.name : "-"; }

            public string Report(string n)
            {
                return "[NpcOutfit] " + n + " bones (" + (usedAvatar ? "humanoid avatar" : "found by name") + "): " +
                       "hips=" + N(hips) + " spine=" + N(spine) + " chest=" + N(chest) + " upperChest=" + N(upperChest) +
                       " neck=" + N(neck) + " head=" + N(head) +
                       " | L: arm=" + N(upperArm[0]) + " forearm=" + N(lowerArm[0]) + " hand=" + N(hand[0]) +
                       " thigh=" + N(upperLeg[0]) + " calf=" + N(lowerLeg[0]) + " foot=" + N(foot[0]) + " toes=" + N(toes[0]) +
                       " | R: arm=" + N(upperArm[1]) + " forearm=" + N(lowerArm[1]) + " hand=" + N(hand[1]) +
                       " thigh=" + N(upperLeg[1]) + " calf=" + N(lowerLeg[1]) + " foot=" + N(foot[1]) + " toes=" + N(toes[1]);
            }
        }
    

    // =========================================================
    // BODY MEASUREMENT (so clothes fit each model)
    // =========================================================

    private class Body
    {
        public float H;
        public float armR, foreR, thighR, calfR, neckR, torsoLen;
        public readonly float[] bandW = new float[4];
        public readonly float[] bandD = new float[4];
        public Bounds head;
        public float footHalfW, footFront, footBack, soleDrop;
        public bool scanned;

        public Body(GameObject obj, Rig rig)
        {
            var verts = ScanVerts(obj, out float minY, out float maxY);
            scanned = verts != null;

            H = scanned
                ? Mathf.Max(0.5f, maxY - minY)
                : Mathf.Max(0.5f, (rig.head.position.y - Mathf.Min(rig.foot[0].position.y, rig.foot[1].position.y)) * 1.2f);

            
            armR = 0.036f * H; foreR = 0.030f * H; thighR = 0.058f * H; calfR = 0.040f * H; neckR = 0.030f * H;
            bandW[0] = 0.095f * H; bandW[1] = 0.085f * H; bandW[2] = 0.095f * H; bandW[3] = 0.115f * H;
            bandD[0] = 0.065f * H; bandD[1] = 0.060f * H; bandD[2] = 0.070f * H; bandD[3] = 0.065f * H;
            footHalfW = 0.028f * H; footFront = 0.10f * H; footBack = -0.04f * H; soleDrop = 0.045f * H;
            head = new Bounds(rig.head.position + Vector3.up * 0.06f * H, new Vector3(0.12f * H, 0.14f * H, 0.14f * H));

            torsoLen = Vector3.Dot(rig.neck.position - rig.hips.position, Vector3.up);
            if (torsoLen < 0.1f * H) torsoLen = 0.3f * H;

            if (!scanned) return;

            
            float a0 = 0, a1 = 0, t0 = 0, c0 = 0;
            for (int s = 0; s < 2; s++)
            {
                a0 += Limb(Gather(verts, rig.upperArm[s]), rig.upperArm[s].position, rig.lowerArm[s].position, armR);
                a1 += Limb(Gather(verts, rig.lowerArm[s]), rig.lowerArm[s].position, rig.HandPos(s), foreR);
                t0 += Limb(Gather(verts, rig.upperLeg[s]), rig.upperLeg[s].position, rig.lowerLeg[s].position, thighR);
                c0 += Limb(Gather(verts, rig.lowerLeg[s]), rig.lowerLeg[s].position, rig.foot[s].position, calfR);
            }
            armR = a0 * 0.5f; foreR = a1 * 0.5f; thighR = t0 * 0.5f; calfR = c0 * 0.5f;

            if (rig.neck != rig.head)
                neckR = Limb(Gather(verts, rig.neck), rig.neck.position, rig.head.position, neckR);

            
            Vector3 hipsPos = rig.hips.position;
            var pts = Gather(verts, rig.hips, rig.spine, rig.chest, rig.upperChest, rig.shoulder[0], rig.shoulder[1]);
            var wl = new List<float>[4];
            var dl = new List<float>[4];
            for (int b = 0; b < 4; b++) { wl[b] = new List<float>(); dl[b] = new List<float>(); }

            foreach (var p in pts)
            {
                Vector3 rel = p - hipsPos;
                float h = Vector3.Dot(rel, Vector3.up) / torsoLen;
                if (h < -0.3f || h > 1.05f) continue;
                int band = Mathf.Clamp((int)(Mathf.Max(h, 0f) * 4f), 0, 3);
                wl[band].Add(Mathf.Abs(Vector3.Dot(rel, rig.right)));
                dl[band].Add(Mathf.Abs(Vector3.Dot(rel, rig.fwd)));
            }
            for (int b = 0; b < 4; b++)
            {
                if (wl[b].Count < 6) continue;
                bandW[b] = Mathf.Clamp(Pct(wl[b], 0.9f), bandW[b] * 0.5f, bandW[b] * 2.2f);
                bandD[b] = Mathf.Clamp(Pct(dl[b], 0.9f), bandD[b] * 0.5f, bandD[b] * 2.2f);
            }

            
            var hp = Gather(verts, rig.head);
            if (hp.Count >= 20)
            {
                head = new Bounds(hp[0], Vector3.zero);
                foreach (var p in hp) head.Encapsulate(p);
            }

            
            var fw = new List<float>();
            var fz = new List<float>();
            float drop = 0f; int dropN = 0;
            for (int s = 0; s < 2; s++)
            {
                var fp = Gather(verts, rig.foot[s], rig.toes[s]);
                if (fp.Count < 10) continue;
                Vector3 fpos = rig.foot[s].position;
                float lowest = float.MaxValue;
                foreach (var p in fp)
                {
                    Vector3 rel = p - fpos;
                    fw.Add(Mathf.Abs(Vector3.Dot(rel, rig.right)));
                    fz.Add(Vector3.Dot(rel, rig.fwd));
                    lowest = Mathf.Min(lowest, p.y);
                }
                drop += fpos.y - lowest; dropN++;
            }
            if (fw.Count >= 10)
            {
                footHalfW = Mathf.Clamp(Pct(fw, 0.9f), 0.015f * H, 0.06f * H);
                footFront = Mathf.Clamp(Pct(fz, 0.97f), 0.05f * H, 0.2f * H);
                footBack = Mathf.Clamp(Pct(fz, 0.03f), -0.1f * H, -0.01f * H);
                if (dropN > 0) soleDrop = Mathf.Clamp(drop / dropN, 0.015f * H, 0.12f * H);
            }
        }

        public string Describe(string n)
        {
            return "[NpcOutfit] " + n + " | scanned=" + scanned + " H=" + H.ToString("F2") +
                   " arm=" + armR.ToString("F3") + " fore=" + foreR.ToString("F3") +
                   " thigh=" + thighR.ToString("F3") + " calf=" + calfR.ToString("F3") +
                   " torsoW=" + bandW[2].ToString("F3") + " torsoD=" + bandD[2].ToString("F3");
        }

        private static float Pct(List<float> l, float p)
        {
            l.Sort();
            return l[Mathf.Clamp(Mathf.FloorToInt(l.Count * p), 0, l.Count - 1)];
        }

        private static float Limb(List<Vector3> pts, Vector3 a, Vector3 b, float fallback)
        {
            Vector3 axis = b - a;
            float len = axis.magnitude;
            if (len < 1e-4f || pts.Count < 12) return fallback;
            axis /= len;

            var d = new List<float>(pts.Count);
            foreach (var p in pts)
            {
                Vector3 rel = p - a;
                float t = Vector3.Dot(rel, axis);
                if (t < len * 0.1f || t > len * 0.9f) continue;
                d.Add((rel - axis * t).magnitude);
            }
            if (d.Count < 8) return fallback;
            return Mathf.Clamp(Pct(d, 0.9f), fallback * 0.4f, fallback * 2.2f);
        }

        private static List<Vector3> Gather(Dictionary<Transform, List<Vector3>> d, params Transform[] bones)
        {
            var res = new List<Vector3>();
            foreach (var b in bones)
                if (b != null && d.TryGetValue(b, out List<Vector3> l)) res.AddRange(l);
            return res;
        }

        
        private static Dictionary<Transform, List<Vector3>> ScanVerts(GameObject obj, out float minY, out float maxY)
        {
            minY = float.MaxValue; maxY = float.MinValue;
            var result = new Dictionary<Transform, List<Vector3>>();
            bool any = false;

            foreach (var smr in obj.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Mesh m = smr.sharedMesh;
                if (m == null) continue;
                if (!m.isReadable)
                {
                    Debug.LogWarning("[NpcOutfit] Mesh '" + m.name + "' is not readable. Enable Read/Write in the model's " +
                                     "import settings for a perfect fit. Using estimated body sizes instead.");
                    continue;
                }

                Vector3[] v = m.vertices;
                BoneWeight[] w = m.boneWeights;
                Matrix4x4[] bind = m.bindposes;
                Transform[] bones = smr.bones;
                if (w.Length != v.Length || bones.Length == 0 || bind.Length != bones.Length) continue;

                var skin = new Matrix4x4[bones.Length];
                for (int i = 0; i < bones.Length; i++)
                    skin[i] = bones[i] != null ? bones[i].localToWorldMatrix * bind[i] : Matrix4x4.identity;

                for (int i = 0; i < v.Length; i++)
                {
                    BoneWeight bw = w[i];
                    float sum = bw.weight0 + bw.weight1 + bw.weight2 + bw.weight3;
                    if (sum < 0.0001f) continue;

                    Vector3 p = skin[bw.boneIndex0].MultiplyPoint3x4(v[i]) * bw.weight0;
                    int best = bw.boneIndex0; float bestW = bw.weight0;

                    if (bw.weight1 > 0f)
                    {
                        p += skin[bw.boneIndex1].MultiplyPoint3x4(v[i]) * bw.weight1;
                        if (bw.weight1 > bestW) { best = bw.boneIndex1; bestW = bw.weight1; }
                    }
                    if (bw.weight2 > 0f)
                    {
                        p += skin[bw.boneIndex2].MultiplyPoint3x4(v[i]) * bw.weight2;
                        if (bw.weight2 > bestW) { best = bw.boneIndex2; bestW = bw.weight2; }
                    }
                    if (bw.weight3 > 0f)
                    {
                        p += skin[bw.boneIndex3].MultiplyPoint3x4(v[i]) * bw.weight3;
                        if (bw.weight3 > bestW) { best = bw.boneIndex3; bestW = bw.weight3; }
                    }
                    p /= sum;

                    Transform bone = bones[best];
                    if (bone == null) continue;

                    if (!result.TryGetValue(bone, out List<Vector3> list))
                    {
                        list = new List<Vector3>();
                        result[bone] = list;
                    }
                    list.Add(p);
                    minY = Mathf.Min(minY, p.y);
                    maxY = Mathf.Max(maxY, p.y);
                    any = true;
                }
            }
            return any ? result : null;
        }
    }

    // =========================================================
    // MESH BUILDING PRIMITIVES 
    // =========================================================

    private struct Ring
    {
        public Vector3 c; public float a, b;
        public Ring(Vector3 c, float a, float b) { this.c = c; this.a = a; this.b = b; }
    }

    private class MeshBuilder
    {
        public readonly List<Vector3> verts = new List<Vector3>();
        public readonly List<int> keyOrder = new List<int>();
        private readonly Dictionary<int, List<int>> _tris = new Dictionary<int, List<int>>();

        private List<int> TrisFor(int key)
        {
            if (!_tris.TryGetValue(key, out List<int> l))
            {
                l = new List<int>();
                _tris[key] = l;
                keyOrder.Add(key);
            }
            return l;
        }

        public void Grid(int key, Vector3[] pts, int rows, int cols, bool wrap)
        {
            List<int> t = TrisFor(key);
            int colSegs = wrap ? cols : cols - 1;

            for (int pass = 0; pass < 2; pass++)
            {
                int b0 = verts.Count;
                verts.AddRange(pts);

                for (int i = 0; i < rows - 1; i++)
                    for (int j = 0; j < colSegs; j++)
                    {
                        int j2 = (j + 1) % cols;
                        int a = b0 + i * cols + j;
                        int b = b0 + i * cols + j2;
                        int c = b0 + (i + 1) * cols + j2;
                        int d = b0 + (i + 1) * cols + j;

                        if (pass == 0) { t.Add(a); t.Add(b); t.Add(c); t.Add(a); t.Add(c); t.Add(d); }
                        else { t.Add(a); t.Add(c); t.Add(b); t.Add(a); t.Add(d); t.Add(c); }
                    }
            }
        }

        public void Fan(int key, Vector3 center, Vector3[] ring)
        {
            List<int> t = TrisFor(key);
            int n = ring.Length;

            for (int pass = 0; pass < 2; pass++)
            {
                int b0 = verts.Count;
                verts.Add(center);
                verts.AddRange(ring);

                for (int j = 0; j < n; j++)
                {
                    int a = b0 + 1 + j;
                    int b = b0 + 1 + (j + 1) % n;
                    if (pass == 0) { t.Add(b0); t.Add(a); t.Add(b); }
                    else { t.Add(b0); t.Add(b); t.Add(a); }
                }
            }
        }

        public void Loft(int key, IList<Ring> rings, Vector3 r, Vector3 f, int seg, bool capA, bool capB)
        {
            int n = rings.Count;
            if (n < 2) return;

            var pts = new Vector3[n * seg];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < seg; j++)
                {
                    float ang = Mathf.PI * 2f * j / seg;
                    pts[i * seg + j] = rings[i].c
                                     + r * (Mathf.Cos(ang) * rings[i].a)
                                     + f * (Mathf.Sin(ang) * rings[i].b);
                }

            Grid(key, pts, n, seg, true);
            if (capA) Fan(key, rings[0].c, Slice(pts, 0, seg));
            if (capB) Fan(key, rings[n - 1].c, Slice(pts, (n - 1) * seg, seg));
        }

        public void Dome(int key, Vector3 c, Vector3 r, Vector3 u, Vector3 f,
                         float rx, float ry, float rz, float phiMax, int seg = 12, int rows = 6)
        {
            int rowsN = rows + 1;
            var pts = new Vector3[rowsN * seg];
            for (int i = 0; i <= rows; i++)
            {
                float phi = phiMax * i / rows;
                float sp = Mathf.Sin(phi), cp = Mathf.Cos(phi);
                for (int j = 0; j < seg; j++)
                {
                    float th = Mathf.PI * 2f * j / seg;
                    pts[i * seg + j] = c + r * (rx * sp * Mathf.Cos(th)) + f * (rz * sp * Mathf.Sin(th)) + u * (ry * cp);
                }
            }
            Grid(key, pts, rowsN, seg, true);
        }

        public void Box(int key, Vector3 c, Vector3 r, Vector3 u, Vector3 f, float hr, float hu, float hf)
        {
            Face(key, c + r * hr, u * hu, f * hf);
            Face(key, c - r * hr, u * hu, f * hf);
            Face(key, c + u * hu, r * hr, f * hf);
            Face(key, c - u * hu, r * hr, f * hf);
            Face(key, c + f * hf, r * hr, u * hu);
            Face(key, c - f * hf, r * hr, u * hu);
        }

        private void Face(int key, Vector3 center, Vector3 s, Vector3 t)
        {
            Grid(key, new[] { center - s - t, center + s - t, center - s + t, center + s + t }, 2, 2, false);
        }

        public void Path(int key, Vector3[] pts, float radius, int seg = 6)
        {
            int n = pts.Length;
            if (n < 2) return;

            var ring = new Vector3[n * seg];
            Vector3 a = Vector3.zero;

            for (int i = 0; i < n; i++)
            {
                Vector3 t = i == 0 ? pts[1] - pts[0]
                          : i == n - 1 ? pts[i] - pts[i - 1]
                          : pts[i + 1] - pts[i - 1];
                t.Normalize();

                if (i == 0)
                    a = Vector3.Cross(t, Mathf.Abs(Vector3.Dot(t, Vector3.up)) > 0.9f ? Vector3.right : Vector3.up).normalized;
                else
                    a = (a - t * Vector3.Dot(a, t)).normalized;   

                Vector3 b = Vector3.Cross(t, a);
                for (int j = 0; j < seg; j++)
                {
                    float ang = Mathf.PI * 2f * j / seg;
                    ring[i * seg + j] = pts[i] + a * (Mathf.Cos(ang) * radius) + b * (Mathf.Sin(ang) * radius);
                }
            }
            Grid(key, ring, n, seg, true);
        }

        private static Vector3[] Slice(Vector3[] src, int start, int len)
        {
            var r = new Vector3[len];
            System.Array.Copy(src, start, r, 0, len);
            return r;
        }

        
        public Mesh ToMesh(Transform bone)
        {
            var m = new Mesh();
            if (verts.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            Matrix4x4 w2l = bone.worldToLocalMatrix;
            var local = new Vector3[verts.Count];
            for (int i = 0; i < local.Length; i++) local[i] = w2l.MultiplyPoint3x4(verts[i]);

            m.vertices = local;
            m.subMeshCount = keyOrder.Count;
            for (int s = 0; s < keyOrder.Count; s++) m.SetTriangles(_tris[keyOrder[s]], s);

            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }

    // =========================================================
    // OUTFIT BUILDER 
    // =========================================================

    private class Builder
    {
        public readonly Dictionary<Transform, MeshBuilder> meshes = new Dictionary<Transform, MeshBuilder>();

        private readonly OutfitLook look;
        private readonly Rig rig;
        private readonly Body body;
        private readonly System.Random rng;
        private readonly float H, pad;
        private readonly Vector3 up = Vector3.up;

        private int cTop, cTrim, cBottom, cShoe, cSole;
        private bool colorBlock;

        // torso
        private List<Transform> chain;
        private Vector3[] P;
        private float[] hs;
        private float torsoLen, thighLen;
        private Vector3 hipsPos;
        private float[] ctlH, ctlW, ctlD;
        private float quiltPeriod, quiltAmp;

        // head
        private float headTop, headH, headHalfW, headHalfD;
        private Vector3 headCentre;
        private bool hasHat;

        public Builder(OutfitLook look, Rig rig, Body body, System.Random rng)
        {
            this.look = look; this.rig = rig; this.body = body; this.rng = rng;
            H = body.H;
            pad = 0.006f * H;
        }

        public void Run()
        {
            cTop = PickColour(look.topColors, Color.gray);
            cTrim = PickColour(look.trimColors, Color.black);
            cBottom = PickColour(look.bottomColors, Color.gray);
            cShoe = PickColour(look.shoeColors, Color.black);
            cSole = Key(look.soleColor);
            colorBlock = Roll(look.colorBlockChance);

            quiltPeriod = look.quilted ? 0.055f * H : 0f;
            quiltAmp = look.quilted ? 0.03f : 0f;

            SetupTorso();
            BuildTop();
            BuildBottoms();
            BuildShoes();
            BuildHeadwear();
            BuildTorsoExtras();
        }

        // ---------- helpers ----------

        private bool Roll(float chance) { return rng.NextDouble() < chance; }
        private float Rf(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

        private int PickColour(Color[] arr, Color fallback)
        {
            Color c = (arr != null && arr.Length > 0) ? arr[rng.Next(arr.Length)] : fallback;
            return Key(c);
        }

        private MeshBuilder For(Transform bone)
        {
            if (!meshes.TryGetValue(bone, out MeshBuilder mb))
            {
                mb = new MeshBuilder();
                meshes[bone] = mb;
            }
            return mb;
        }

        private void Perp(Vector3 axis, out Vector3 r, out Vector3 f)
        {
            Vector3 refv = Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up;
            r = Vector3.Cross(axis, refv).normalized;
            f = Vector3.Cross(axis, r);
        }

        private void Tube(Transform bone, int key, Vector3 p, Vector3 q, float r0, float r1,
                          bool capA, bool capB, bool quilt)
        {
            Vector3 axis = q - p;
            float len = axis.magnitude;
            if (len < 1e-4f) return;
            axis /= len;
            Perp(axis, out Vector3 r, out Vector3 f);

            bool quilting = quilt && quiltPeriod > 0f;
            int steps = quilting ? Mathf.Max(1, Mathf.CeilToInt(len / (quiltPeriod * 0.25f))) : 1;

            var rings = new List<Ring>(steps + 1);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float rad = Mathf.Lerp(r0, r1, t);
                if (quilting) rad *= 1f - quiltAmp * Mathf.Cos(Mathf.PI * 2f * (t * len) / quiltPeriod);
                rings.Add(new Ring(Vector3.Lerp(p, q, t), rad, rad));
            }
            For(bone).Loft(key, rings, r, f, 12, capA, capB);
        }

        private void Sphere(Transform bone, int key, Vector3 c, float r)
        {
            For(bone).Dome(key, c, Vector3.right, up, Vector3.forward, r, r, r, Mathf.PI, 12, 8);
        }

        private void Box(Transform bone, int key, Vector3 c, float hr, float hu, float hf)
        {
            For(bone).Box(key, c, rig.right, up, rig.fwd, hr, hu, hf);
        }

        // ---------- torso profile ----------

        private static float Interp(float[] xs, float[] ys, float x)
        {
            if (x <= xs[0]) return ys[0];
            for (int i = 1; i < xs.Length; i++)
                if (x <= xs[i])
                    return Mathf.Lerp(ys[i - 1], ys[i], (x - xs[i - 1]) / (xs[i] - xs[i - 1]));
            return ys[ys.Length - 1];
        }

        private float Wf(float h) { return Interp(ctlH, ctlW, h); }
        private float Df(float h) { return Interp(ctlH, ctlD, h); }

        private void SetupTorso()
        {
            chain = new List<Transform> { rig.hips, rig.spine };
            if (rig.chest != null && rig.chest != rig.spine) chain.Add(rig.chest);
            if (rig.upperChest != null && rig.upperChest != rig.chest) chain.Add(rig.upperChest);
            int n = chain.Count;

            hipsPos = rig.hips.position;
            torsoLen = body.torsoLen;
            thighLen = Mathf.Max(0.1f * H, Vector3.Distance(rig.upperLeg[0].position, rig.lowerLeg[0].position));

            P = new Vector3[n + 1];
            hs = new float[n + 1];
            for (int i = 0; i < n; i++)
            {
                P[i] = chain[i].position;
                hs[i] = i == 0 ? 0f : Mathf.Max(Vector3.Dot(P[i] - hipsPos, up) / torsoLen, hs[i - 1] + 0.02f);
            }
            P[n] = rig.neck.position;
            hs[n] = Mathf.Max(1f, hs[n - 1] + 0.02f);

            float shoulderH = Vector3.Dot((rig.upperArm[0].position + rig.upperArm[1].position) * 0.5f - hipsPos, up) / torsoLen;
            float hS = Mathf.Clamp(shoulderH, 0.7f, hs[n] - 0.04f);
            float shoulderHalf = Vector3.Distance(rig.upperArm[0].position, rig.upperArm[1].position) * 0.5f;

            ctlH = new[] { 0f, 0.3f, 0.58f, hS, hs[n] };
            ctlW = new[] { body.bandW[0], body.bandW[1], body.bandW[2], Mathf.Max(body.bandW[3], shoulderHalf * 0.95f), body.neckR * 1.5f };
            ctlD = new[] { body.bandD[0], body.bandD[1], body.bandD[2], body.bandD[3], body.neckR * 1.4f };
        }

        private Vector3 CenterAt(float h)
        {
            if (h <= 0f) return hipsPos + up * (h * torsoLen);
            for (int k = 0; k < chain.Count; k++)
                if (h <= hs[k + 1] || k == chain.Count - 1)
                    return Vector3.Lerp(P[k], P[k + 1], Mathf.InverseLerp(hs[k], hs[k + 1], h));
            return P[chain.Count];
        }

        private Ring TorsoRing(float h, float loose)
        {
            if (h < 0f) loose *= 1.04f;   
            float m = quiltPeriod > 0f
                ? 1f - quiltAmp * Mathf.Cos(Mathf.PI * 2f * (h * torsoLen) / quiltPeriod)
                : 1f;
            return new Ring(CenterAt(h), Wf(h) * loose * m + pad, Df(h) * loose * m + pad);
        }

        private Vector3 SurfacePoint(float h, float theta, float loose, float extra)
        {
            return CenterAt(h)
                 + rig.right * (Mathf.Cos(theta) * (Wf(h) * loose + extra))
                 + rig.fwd * (Mathf.Sin(theta) * (Df(h) * loose + extra));
        }

        private Vector3 SurfaceNormal(float h, float theta)
        {
            return (rig.right * (Mathf.Cos(theta) / Wf(h)) + rig.fwd * (Mathf.Sin(theta) / Df(h))).normalized;
        }

        private Transform BoneAtH(float h)
        {
            for (int k = 0; k < chain.Count; k++)
                if (h < hs[k + 1]) return chain[k];
            return chain[chain.Count - 1];
        }

        // ---------- top (jacket + sleeves) ----------

        private void BuildTop()
        {
            float loose = look.topLoose;
            int n = chain.Count;
            float hemH = look.hemDrop > 0.01f ? -(look.hemDrop * thighLen) / torsoLen : 0f;

            for (int i = 0; i < n; i++)
            {
                float h0 = hs[i], h1 = hs[i + 1];
                var list = new List<float>();

                if (i == 0 && hemH < 0f) list.Add(hemH);
                list.Add(h0);

                if (quiltPeriod > 0f)
                {
                    int steps = Mathf.CeilToInt((h1 - h0) * torsoLen / (quiltPeriod * 0.25f));
                    for (int s = 1; s < steps; s++) list.Add(Mathf.Lerp(h0, h1, s / (float)steps));
                }
                foreach (float c in ctlH)
                    if (c > h0 + 0.01f && c < h1 - 0.01f) list.Add(c);
                list.Add(h1);
                list.Sort();

                var rings = new List<Ring>();
                float last = -999f;
                foreach (float h in list)
                {
                    if (h - last < 0.004f) continue;
                    last = h;
                    rings.Add(TorsoRing(h, loose));
                }

                int key = (colorBlock && i == n - 1) ? cTrim : cTop;
                For(chain[i]).Loft(key, rings, rig.right, rig.fwd, 14, false, false);
            }

            // ribbed hem band
            float bandTop = hemH + 0.03f * H / torsoLen;
            var band = new List<Ring> { TorsoRing(hemH, loose * 1.02f), TorsoRing(bandTop, loose * 1.02f) };
            For(rig.hips).Loft(cTrim, band, rig.right, rig.fwd, 14, false, false);

            BuildSleeve(0);
            BuildSleeve(1);
        }

        private void BuildSleeve(int s)
        {
            if (look.sleeveLength < 0.02f) return;

            Transform ua = rig.upperArm[s], la = rig.lowerArm[s];
            Vector3 A = ua.position, Bp = la.position, C = rig.HandPos(s);
            float lenAB = Vector3.Distance(A, Bp), lenBC = Vector3.Distance(Bp, C);
            if (lenAB < 1e-3f || lenBC < 1e-3f) return;
            Vector3 dAB = (Bp - A) / lenAB, dBC = (C - Bp) / lenBC;

            float reach = look.sleeveLength * (lenAB + lenBC);
            float rUp = body.armR * look.topLoose + pad;
            float rFore = body.foreR * look.topLoose + pad;

            float upperReach = Mathf.Min(reach, lenAB);
            Vector3 end1 = A + dAB * upperReach;
            float rEnd1 = Mathf.Lerp(rUp, rFore, upperReach / lenAB);

            Tube(ua, cTop, A - dAB * (0.05f * lenAB), end1, rUp, rEnd1, false, false, true);
            Sphere(ua, cTop, A, rUp * 1.03f);   // shoulder cap

            Transform lastBone = ua; Vector3 lastEnd = end1; Vector3 lastDir = dAB; float lastR = rEnd1;

            if (reach > lenAB + 0.01f)
            {
                float foreReach = Mathf.Min(reach - lenAB, lenBC);
                Vector3 e2 = Bp + dBC * foreReach;
                float rEnd2 = Mathf.Lerp(rFore, rFore * 0.9f, foreReach / lenBC);

                Tube(la, cTop, Bp - dBC * (0.04f * lenBC), e2, rFore, rEnd2, false, false, true);
                Sphere(la, cTop, Bp, rFore * 1.03f);   // elbow joint filler

                lastBone = la; lastEnd = e2; lastDir = dBC; lastR = rEnd2;
            }

            float cuffLen = Mathf.Min(0.035f * H, reach * 0.25f);
            Tube(lastBone, cTrim, lastEnd - lastDir * cuffLen, lastEnd, lastR * 1.05f, lastR * 1.05f, false, false, false);
        }

        // ---------- trousers ----------

        private void BuildBottoms()
        {
            float loose = look.pantsLoose;
            Vector3 top = hipsPos + up * (0.10f * torsoLen);
            Vector3 bot = hipsPos - up * (0.28f * thighLen);
            float w0 = body.bandW[0] * loose + pad;
            float d0 = body.bandD[0] * loose + pad;

            var rings = new List<Ring>
            {
                new Ring(top, w0 * 0.97f, d0 * 0.97f),
                new Ring(hipsPos, w0, d0),
                new Ring(bot, w0 * 0.88f, d0 * 0.90f)
            };
            For(rig.hips).Loft(cBottom, rings, rig.right, rig.fwd, 14, false, false);

            BuildLeg(0);
            BuildLeg(1);
        }

        private void BuildLeg(int s)
        {
            if (look.pantsLength < 0.02f) return;

            Transform ul = rig.upperLeg[s], ll = rig.lowerLeg[s];
            Vector3 A = ul.position, Bp = ll.position, C = rig.foot[s].position;
            float lenAB = Vector3.Distance(A, Bp), lenBC = Vector3.Distance(Bp, C);
            if (lenAB < 1e-3f || lenBC < 1e-3f) return;
            Vector3 dAB = (Bp - A) / lenAB, dBC = (C - Bp) / lenBC;

            float reach = look.pantsLength * (lenAB + lenBC);
            float loose = look.pantsLoose;
            float flare = look.pantsFlare;

            float rT = body.thighR * loose + pad;
            float rK = body.calfR * 1.12f * loose + pad;
            float rA = body.calfR * 0.92f * loose + pad;
            float rKf = rK * Mathf.Lerp(1f, flare, 0.3f);
            float rAf = rA * flare;

            float upperReach = Mathf.Min(reach, lenAB);
            Vector3 end1 = A + dAB * upperReach;
            float rEnd1 = Mathf.Lerp(rT, rKf, upperReach / lenAB);

            Tube(ul, cBottom, A - dAB * (0.05f * lenAB), end1, rT, rEnd1, false, false, false);

            Transform lastBone = ul; Vector3 lastEnd = end1; Vector3 lastDir = dAB; float lastR = rEnd1;

            if (reach > lenAB + 0.01f)
            {
                float lowReach = Mathf.Min(reach - lenAB, lenBC);
                Vector3 e2 = Bp + dBC * lowReach;
                float rEnd2 = Mathf.Lerp(rKf, rAf, lowReach / lenBC);

                Tube(ll, cBottom, Bp - dBC * (0.03f * lenBC), e2, rKf, rEnd2, false, false, false);
                Sphere(ll, cBottom, Bp, rKf * 1.02f);   // knee joint filler

                lastBone = ll; lastEnd = e2; lastDir = dBC; lastR = rEnd2;
            }

            float cuffLen = Mathf.Min(0.03f * H, reach * 0.2f);
            Tube(lastBone, cTrim, lastEnd - lastDir * cuffLen, lastEnd, lastR * 1.03f, lastR * 1.03f, false, false, false);

            if (look.cargoPockets && look.pantsLength > 0.6f)
            {
                float side = s == 0 ? -1f : 1f;
                Vector3 pc = A + dAB * (0.45f * lenAB) + rig.right * (side * (rT + 0.008f * H));
                Box(ul, cTrim, pc, 0.012f * H, 0.045f * H, 0.038f * H);
            }
        }

        // ---------- shoes ----------

        private void BuildShoes()
        {
            float[] tt = { 0f, 0.2f, 0.5f, 0.8f, 1f };
            float[] topF = { 0.70f, 1.00f, 0.55f, 0.40f, 0.30f };
            float[] wF = { 0.80f, 0.97f, 1.04f, 0.98f, 0.72f };

            for (int s = 0; s < 2; s++)
            {
                Transform f = rig.foot[s];
                Vector3 fp = f.position;
                float soleY = fp.y - body.soleDrop;
                float topAnkle = fp.y + 0.015f * H;
                float zBack = body.footBack - 0.012f * H;
                float zFront = body.footFront + 0.018f * H;
                float fw = body.footHalfW * 1.08f + 0.004f * H;
                float soleT = 0.022f * H;

                var upper = new List<Ring>();
                var sole = new List<Ring>();

                for (int i = 0; i < tt.Length; i++)
                {
                    float z = Mathf.Lerp(zBack, zFront, tt[i]);
                    Vector3 basePos = new Vector3(fp.x, soleY, fp.z) + rig.fwd * z;
                    float topY = Mathf.Lerp(soleY, topAnkle, topF[i]);
                    float halfH = Mathf.Max(0.01f * H, (topY - soleY) * 0.5f);

                    upper.Add(new Ring(basePos + up * halfH, fw * wF[i], halfH));
                    sole.Add(new Ring(basePos + up * (soleT * 0.5f), fw * wF[i] * 1.04f, soleT * 0.5f + 0.002f * H));
                }

                var mb = For(f);
                mb.Loft(cShoe, upper, rig.right, up, 14, true, true);
                mb.Loft(cSole, sole, rig.right, up, 14, true, true);
            }
        }

        // ---------- headwear ----------

        private void BuildHeadwear()
        {
            headTop = body.head.max.y;
            headH = Mathf.Clamp(body.head.size.y, 0.08f * H, 0.15f * H);
            headCentre = body.head.center;
            Vector3 e = body.head.extents;
            headHalfW = Mathf.Max(0.035f * H, Mathf.Abs(rig.right.x) * e.x + Mathf.Abs(rig.right.z) * e.z);
            headHalfD = Mathf.Max(0.035f * H, Mathf.Abs(rig.fwd.x) * e.x + Mathf.Abs(rig.fwd.z) * e.z);

            hasHat = look.hatTypes != null && look.hatTypes.Length > 0 && Roll(look.hatChance);
            if (hasHat)
            {
                int hatKey = PickColour(look.hatColors, Color.gray);
                switch (look.hatTypes[rng.Next(look.hatTypes.Length)])
                {
                    case OutfitHat.Beanie: Beanie(hatKey); break;
                    case OutfitHat.Cap: Cap(hatKey); break;
                    case OutfitHat.Beret: Beret(hatKey); break;
                    case OutfitHat.Bucket: Bucket(hatKey); break;
                }
            }

            if (!hasHat && Roll(look.headphonesChance)) Headphones();
            if (Roll(look.sunglassesChance)) Sunglasses();
        }

        private void Beanie(int key)
        {
            var mb = For(rig.head);
            float baseY = headTop - 0.50f * headH;
            Vector3 c = new Vector3(headCentre.x, baseY, headCentre.z);
            float rx = headHalfW * 1.07f + pad, rz = headHalfD * 1.07f + pad, ry = (headTop - baseY) * 1.15f + pad;

            mb.Dome(key, c, rig.right, up, rig.fwd, rx, ry, rz, Mathf.PI * 0.5f, 14, 7);

            var cuff = new List<Ring>
            {
                new Ring(c - up * (0.015f * H), rx * 1.03f, rz * 1.03f),
                new Ring(c + up * (0.035f * H), rx * 1.03f, rz * 1.03f)
            };
            mb.Loft(cTrim, cuff, rig.right, rig.fwd, 16, false, false);
        }

        private void Cap(int key)
        {
            var mb = For(rig.head);
            float baseY = headTop - 0.40f * headH;
            Vector3 c = new Vector3(headCentre.x, baseY, headCentre.z);
            float rx = headHalfW * 1.06f + pad, rz = headHalfD * 1.06f + pad, ry = (headTop - baseY) * 1.0f + pad;

            mb.Dome(key, c, rig.right, up, rig.fwd, rx, ry, rz, Mathf.PI * 0.5f, 14, 7);

            const int m = 9;
            float brimLen = headHalfD * 0.55f;
            var pts = new Vector3[2 * m];
            for (int j = 0; j < m; j++)
            {
                float th = Mathf.PI * j / (m - 1);
                float cx = Mathf.Cos(th), sz = Mathf.Sin(th);
                pts[j] = c + rig.right * (cx * rx) + rig.fwd * (sz * rz) + up * (0.006f * H);
                pts[m + j] = c + rig.right * (cx * rx * 1.08f) + rig.fwd * (sz * (rz + brimLen)) - up * (0.012f * H);
            }
            mb.Grid(key, pts, 2, m, false);
        }

        private void Beret(int key)
        {
            var mb = For(rig.head);
            Quaternion tilt = Quaternion.AngleAxis(Rf(-14f, 14f), rig.fwd);
            Vector3 r = tilt * rig.right, u = tilt * up;

            Vector3 c = new Vector3(headCentre.x, headTop - 0.10f * headH, headCentre.z) + r * (headHalfW * 0.12f);
            float rx = headHalfW * 1.45f, rz = headHalfD * 1.40f, ry = headH * 0.30f;

            mb.Dome(key, c, r, u, rig.fwd, rx, ry, rz, Mathf.PI * 0.72f, 16, 8);

            Vector3 apex = c + u * ry;
            Vector3 stemEnd = apex + u * (0.025f * H);
            Tube(rig.head, key, apex - u * (0.005f * H), stemEnd, 0.008f * H, 0.007f * H, true, true, false);
        }

        private void Bucket(int key)
        {
            var mb = For(rig.head);
            float baseY = headTop - 0.48f * headH;
            Vector3 c = new Vector3(headCentre.x, baseY, headCentre.z);
            float rx = headHalfW * 1.05f + pad, rz = headHalfD * 1.05f + pad, ry = (headTop - baseY) * 0.95f + pad;

            mb.Dome(key, c, rig.right, up, rig.fwd, rx, ry, rz, Mathf.PI * 0.5f, 14, 7);

            var brim = new List<Ring>
            {
                new Ring(c, rx * 1.02f, rz * 1.02f),
                new Ring(c - up * (0.02f * H), rx * 1.6f, rz * 1.6f),
                new Ring(c - up * (0.026f * H), rx * 1.61f, rz * 1.61f)
            };
            mb.Loft(key, brim, rig.right, rig.fwd, 18, false, false);
        }

        private void Headphones()
        {
            var mb = For(rig.head);
            int key = PickColour(look.accentColors, Color.black);
            float earY = headTop - 0.55f * headH;
            float cupR = 0.042f * H, thick = 0.022f * H;
            Vector3 mid = new Vector3(headCentre.x, earY, headCentre.z);

            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                Vector3 c0 = mid + rig.right * (sgn * (headHalfW + 0.004f * H));
                var rings = new List<Ring>
                {
                    new Ring(c0, cupR, cupR),
                    new Ring(c0 + rig.right * (sgn * thick), cupR * 0.95f, cupR * 0.95f)
                };
                mb.Loft(key, rings, rig.fwd, up, 14, true, true);
            }

            const int m = 14;
            float rx = headHalfW + 0.01f * H + thick * 0.5f;
            float ry = (headTop - earY) + 0.02f * H;
            var pts = new Vector3[m];
            for (int i = 0; i < m; i++)
            {
                float a = Mathf.PI * i / (m - 1);
                pts[i] = mid + rig.right * (Mathf.Cos(a) * rx) + up * (Mathf.Sin(a) * ry);
            }
            mb.Path(key, pts, 0.008f * H, 6);
        }

        private void Sunglasses()
        {
            var mb = For(rig.head);
            int frame = PickColour(look.accentColors, Color.black);
            int lens = Key(new Color(0.05f, 0.06f, 0.09f));

            float eyeY = headTop - 0.50f * headH;
            Vector3 c = new Vector3(headCentre.x, eyeY, headCentre.z) + rig.fwd * (headHalfD * 0.96f);
            float lw = headHalfW * 0.34f, lh = headH * 0.11f;

            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                Vector3 lc = c + rig.right * (sgn * headHalfW * 0.42f);
                mb.Box(frame, lc, rig.right, up, rig.fwd, lw * 1.12f, lh * 1.15f, 0.008f * H);
                mb.Box(lens, lc + rig.fwd * (0.007f * H), rig.right, up, rig.fwd, lw, lh, 0.006f * H);

                Vector3 temple = c + rig.right * (sgn * headHalfW * 0.97f) - rig.fwd * (headHalfD * 0.5f);
                mb.Box(frame, temple, rig.right, up, rig.fwd, 0.005f * H, 0.006f * H, headHalfD * 0.5f);
            }
            mb.Box(frame, c, rig.right, up, rig.fwd, headHalfW * 0.12f, 0.006f * H, 0.008f * H);
        }

        // ---------- torso extras ----------

        private void BuildTorsoExtras()
        {
            Transform top = chain[chain.Count - 1];

            if (look.hood) Hood(top);
            if (look.collar) Collar(top);

            if (Roll(look.backpackChance)) Backpack(top);
            else if (Roll(look.sashBagChance)) SashBag();

            if (Roll(look.scarfChance)) Scarf(top);

            Splatters();
        }

        private void Hood(Transform bone)
        {
            float nr = body.neckR * look.topLoose;
            Vector3 c = rig.neck.position + up * (nr * 0.7f) - rig.fwd * (nr * 1.6f);
            For(bone).Dome(cTop, c, rig.right, up, rig.fwd, nr * 2.3f, nr * 1.4f, nr * 1.4f, Mathf.PI, 12, 8);
        }

        private void Collar(Transform bone)
        {
            Vector3 np = rig.neck.position;
            float r = body.neckR * 1.55f * look.topLoose;
            var rings = new List<Ring>
            {
                new Ring(np - up * (0.012f * H), r * 1.15f, r),
                new Ring(np + up * (0.030f * H), r * 0.95f, r * 0.90f)
            };
            For(bone).Loft(cTrim, rings, rig.right, rig.fwd, 14, false, false);
        }

        private void Backpack(Transform bone)
        {
            int key = PickColour(look.accentColors, Color.gray);
            float surf = look.topLoose * (1f + quiltAmp);

            float h = 0.66f;
            Vector3 packC = CenterAt(h) - rig.fwd * (Df(h) * surf + pad + 0.045f * H);
            Box(bone, key, packC, 0.075f * H, 0.105f * H, 0.045f * H);
            Box(bone, cTrim, packC - rig.fwd * (0.057f * H) - up * (0.025f * H), 0.055f * H, 0.05f * H, 0.014f * H);

            float hs2 = 0.74f;
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                Vector3 sc = CenterAt(hs2)
                           + rig.right * (sgn * Wf(hs2) * surf * 0.5f)
                           + rig.fwd * (Df(hs2) * surf * 0.87f + pad + 0.004f * H);
                Box(bone, key, sc, 0.013f * H, 0.075f * H, 0.005f * H);
            }
        }

        private void SashBag()
        {
            int key = PickColour(look.accentColors, Color.gray);
            float surf = look.topLoose * (1f + quiltAmp);
            Transform bone = rig.spine;

            const int n = 10;
            var pts = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float h = Mathf.Lerp(0.90f, 0.02f, t);
                float th = Mathf.Lerp(35f, 150f, t) * Mathf.Deg2Rad;
                pts[i] = SurfacePoint(h, th, surf, 0.008f * H);
            }
            For(bone).Path(key, pts, 0.011f * H, 6);

            float thEnd = 150f * Mathf.Deg2Rad;
            Vector3 bagC = pts[n - 1] + SurfaceNormal(0.02f, thEnd) * (0.03f * H);
            Box(bone, key, bagC, 0.055f * H, 0.04f * H, 0.025f * H);
            Box(bone, cTrim, bagC + SurfaceNormal(0.02f, thEnd) * (0.022f * H), 0.03f * H, 0.012f * H, 0.006f * H);
        }

        private void Scarf(Transform bone)
        {
            int key = PickColour(look.accentColors, Color.gray);
            float surf = look.topLoose * (1f + quiltAmp);
            Vector3 np = rig.neck.position;
            float nr = body.neckR * look.topLoose;

            var rings = new List<Ring>
            {
                new Ring(np - up * (0.012f * H), nr * 1.9f, nr * 1.7f),
                new Ring(np + up * (0.015f * H), nr * 1.95f, nr * 1.75f),
                new Ring(np + up * (0.045f * H), nr * 1.55f, nr * 1.45f)
            };
            For(bone).Loft(key, rings, rig.right, rig.fwd, 14, false, false);

            float h = 0.80f;
            Vector3 tailC = CenterAt(h) + rig.fwd * (Df(h) * surf + 0.012f * H) + rig.right * (0.02f * H);
            Box(bone, key, tailC, 0.022f * H, 0.075f * H, 0.008f * H);
        }

        private void Splatters()
        {
            if (look.splatterMax <= 0) return;

            int count = rng.Next(look.splatterMin, look.splatterMax + 1);
            float surf = look.topLoose * (1f + quiltAmp);

            for (int i = 0; i < count; i++)
            {
                float h = Rf(0.12f, 0.85f);
                float th = Rf(20f, 160f) * Mathf.Deg2Rad;
                int key = PickColour(look.accentColors, Color.magenta);

                Vector3 pos = SurfacePoint(h, th, surf, 0.002f * H);
                Vector3 n = SurfaceNormal(h, th);
                Vector3 r1 = Vector3.Cross(n, up).normalized;
                Vector3 f1 = Vector3.Cross(n, r1);
                float size = Rf(0.012f, 0.032f) * H;

                For(BoneAtH(h)).Dome(key, pos, r1, n, f1, size, 0.004f * H, size * Rf(0.7f, 1.2f), Mathf.PI * 0.5f, 8, 3);
            }
        }
    }
}