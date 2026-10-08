using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;


public class HubPrefabSwitcher : MonoBehaviour
{
    public enum RemoveMode
    {
        Deactivate, 
        Destroy     
    }


    [Header("DEBUG - Simulate level completion (Play Mode only)")]
    [Tooltip("Tick to mark the level complete and apply its consequence. Untick to undo it. " +
             "These also show the real state when a level is genuinely completed.")]
    [SerializeField] private bool simulateArtistHitlerComplete;
    [SerializeField] private bool simulateAppleForestComplete;
    [SerializeField] private bool simulateWrongBeerComplete;
    [Tooltip("Tick to instantly collect every DeLorean part (the Apple Forest must be complete first). Unticks itself.")]
    [SerializeField] private bool debugCollectAllParts;
    [Tooltip("Tick to wipe DeLorean mission progress so you can test it again. Unticks itself.")]
    [SerializeField] private bool debugResetMission;

    
    // ARTIST HITLER
    
    [Header("Artist Hitler consequence - objects to remove")]
    [SerializeField] private List<GameObject> objectsToRemove = new List<GameObject>();
    [SerializeField] private RemoveMode removeMode = RemoveMode.Deactivate;

    
    
    // APPLE FOREST

    
    [Header("Apple Forest consequence - objects that float")]
    [Tooltip("Scene objects (drag from the Hierarchy). Gravity is switched off for any Rigidbody.")]
    [SerializeField] private List<GameObject> objectsToFloat = new List<GameObject>();

    [Header("Apple Forest - float volume (shown as a cyan box in the Scene view)")]
    [Tooltip("Optional. Centre of the volume. If empty, this GameObject's position is used.")]
    [SerializeField] private Transform volumeCenter;
    [Tooltip("Width (X), height (Y) and depth (Z) of the volume, in world units.")]
    [SerializeField] private Vector3 volumeSize = new Vector3(40f, 15f, 40f);

    [Header("Apple Forest - float motion")]
    [Tooltip("Each object gets a random top speed between these values (units per second).")]
    [SerializeField] private Vector2 driftSpeedRange = new Vector2(0.5f, 2f);
    [Tooltip("Higher = slower, softer acceleration and turning.")]
    [SerializeField] private float driftSmoothTime = 3f;
    [Tooltip("Each object gets a random spin speed between these values (degrees per second).")]
    [SerializeField] private Vector2 spinSpeedRange = new Vector2(5f, 40f);
    [Tooltip("An object picks a new random destination when it gets this close to its current one.")]
    [SerializeField] private float arriveDistance = 0.75f;

    [Header("Apple Forest - collisions while floating")]
    [Tooltip("Floating objects ALWAYS bounce off anything on the Default layer. " +
             "Tick any extra layers here that they should bounce off too " +
             "(e.g. a 'FloatBounce' layer on invisible walls).")]
    [SerializeField] private LayerMask extraBounceLayers;
    [Tooltip("After a bounce, the object heads this far in its new direction (kept inside the volume).")]
    [SerializeField] private float bounceTravelDistance = 10f;
    [Tooltip("0 = perfect mirror bounce. Higher = more random new direction.")]
    [Range(0f, 1f)]
    [SerializeField] private float bounceRandomness = 0.3f;
    [Tooltip("Collision box size relative to each object's own size. " +
             "Lower this if objects bounce too early, raise it if they clip into walls.")]
    [SerializeField] private float bounceSizeScale = 0.9f;
    [Tooltip("Objects bounce this far BEFORE touching a surface, so spinning corners don't clip in.")]
    [SerializeField] private float bounceLookAhead = 0.5f;

    [Header("Apple Forest - performance")]
    [Tooltip("The bounce check (the expensive part) runs this many seconds apart per object, " +
             "instead of every frame. Higher = cheaper. 0.1 to 0.25 is usually fine.")]
    [SerializeField] private float bounceCheckInterval = 0.1f;
    [Tooltip("Objects further than this from the camera update less often (see Far Update Interval).")]
    [SerializeField] private float nearDistance = 80f;
    [Tooltip("How often far away objects move and check for bounces. Higher = cheaper.")]
    [SerializeField] private float farUpdateInterval = 0.1f;
    [Tooltip("Objects further than this from the camera are paused until you get closer. 0 = never pause.")]
    [SerializeField] private float cullDistance = 300f;
    [Tooltip("Switches off the floating objects' own colliders while they float. Much cheaper, and the " +
             "bounce check still stops them going through the Default layer and the player. " +
             "Side effect: floating objects pass through each other.")]
    [SerializeField] private bool disableCollidersWhileFloating = true;

    
    // DELOREAN REPAIR MISSION 
    
    [Header("DeLorean mission - parts (mission starts once Apple Forest is complete)")]
    [Tooltip("The part objects placed around the hub (drag from the Hierarchy). " +
             "They stay hidden until the mission starts. The counter total is the number of items here (use 3).")]
    [SerializeField] private List<GameObject> partObjects = new List<GameObject>();
    [Tooltip("The DeLorean. The player returns here to install the parts.")]
    [SerializeField] private Transform delorean;
    [Tooltip("The player. If empty, the object tagged 'Player' is used.")]
    [SerializeField] private Transform player;
    [Tooltip("How close the player must get to a part to collect it.")]
    [SerializeField] private float pickupRadius = 3f;
    [Tooltip("How close the player must get to the DeLorean to install the parts.")]
    [SerializeField] private float installRadius = 5f;
    [Tooltip("Off = parts install automatically on arrival. On = player must also press the install key.")]
    [SerializeField] private bool requireKeyToInstall = false;
    [SerializeField] private KeyCode installKey = KeyCode.E;
    [Tooltip("Degrees per second that uncollected parts spin.")]
    [SerializeField] private float partSpinSpeed = 60f;

    [Header("DeLorean mission - sky beams (created in code)")]
    [SerializeField] private Color partBeamColor = new Color(0.2f, 0.9f, 1f, 1f);
    [SerializeField] private Color deloreanBeamColor = new Color(1f, 0.6f, 0.1f, 1f);
    [SerializeField] private float beamHeight = 150f;
    [SerializeField] private float beamWidth = 1f;
    [SerializeField] private float beamPulseSpeed = 3f;
    [Tooltip("Optional. Leave empty to use a built-in unlit material.")]
    [SerializeField] private Material beamMaterial;

    [Header("DeLorean mission - UI (TextMeshPro)")]
    [Tooltip("Optional. A panel holding both texts; shown only while the mission is active.")]
    [SerializeField] private GameObject missionUIRoot;
    [Tooltip("Shows the current mission.")]
    [SerializeField] private TMP_Text missionText;
    [Tooltip("Shows how many parts the player has, e.g. 'Parts: 1 / 3'.")]
    [SerializeField] private TMP_Text partsCounterText;
    [SerializeField] private string findPartsMission = "Find parts to repair the DeLorean flight system";
    [SerializeField] private string returnMission = "Return to the DeLorean to install the parts";
    [Tooltip("Used when 'Require Key To Install' is on. {0} becomes the key name.")]
    [SerializeField] private string installPrompt = "Press {0} to install the parts";
    [SerializeField] private string repairedMessage = "DeLorean flight system repaired!";
    [Tooltip("{0} = parts collected, {1} = total parts.")]
    [SerializeField] private string counterFormat = "Parts: {0} / {1}";
    [Tooltip("How long the 'repaired' message stays on screen before the mission UI hides.")]
    [SerializeField] private float repairedMessageDuration = 6f;

    [Header("DeLorean mission - audio and events")]
    [SerializeField] private AudioClip partCollectedSound;
    [SerializeField] private AudioClip repairedSound;
    [Tooltip("Fires once when the parts are installed. Hook up the flight system here.")]
    [SerializeField] private UnityEvent onDeloreanRepaired;

   
    private class FloatItem
    {
        public Transform t;
        public Rigidbody rb;
        public Vector3 originalPos;
        public Quaternion originalRot;
        public bool wasKinematic;
        public bool usedGravity;
        public Vector3 boxCenterOffset;
        public Vector3 boxHalfExtents;
        public Quaternion invOriginalRot;
        public Collider[] colliders;
        public bool[] collidersWereEnabled;
        public float nextCastTime;
        public float accumDt;

        public Vector3 target;
        public Vector3 velocity;
        public float maxSpeed;
        public Vector3 spinAxis;
        public float spinSpeed;
    }

    private readonly Dictionary<GameObject, bool> _hitlerOriginalActive = new Dictionary<GameObject, bool>();
    private readonly List<FloatItem> _floatItems = new List<FloatItem>();
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[16];
    private Transform _viewer;
    private float _nextViewerSearch;

    private bool _hitlerApplied;
    private bool _appleApplied;

    
    private enum MissionPhase { Inactive, FindParts, ReturnToDelorean, Complete }

    
    private static readonly HashSet<int> s_collectedParts = new HashSet<int>();
    private static bool s_deloreanRepaired;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetMissionStatics()
    {
        s_collectedParts.Clear();
        s_deloreanRepaired = false;
    }

    private class Beam
    {
        public GameObject go;
        public LineRenderer lr;
        public Transform target;
    }

    private readonly List<Beam> _partBeams = new List<Beam>();
    private Beam _deloreanBeam;
    private Material _builtMaterial;
    private MissionPhase _phase = MissionPhase.Inactive;
    private bool _justRepaired;
    private float _hideRepairedAt;
    private string _lastMissionString;
    private float _nextPlayerSearch;

    
    private bool _lastSimHitler, _lastSimApple, _lastSimBeer;

    private Vector3 VolumeCentre => volumeCenter != null ? volumeCenter.position : transform.position;

    private void Start()
    {
        CacheHitlerObjects();
        CacheFloatItems();
        BuildMissionBeams();
        SyncWithWorldState();
        RefreshMission();
    }

    private void OnDestroy()
    {
        foreach (var b in _partBeams)
            if (b != null && b.go != null) Destroy(b.go);
        if (_deloreanBeam != null && _deloreanBeam.go != null) Destroy(_deloreanBeam.go);
        if (_builtMaterial != null) Destroy(_builtMaterial);
    }

    private void Update()
    {
        HandleDebugToggles();
        SyncWithWorldState();

        if (_appleApplied) UpdateFloating();
        UpdateMission();
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 centre = volumeCenter != null ? volumeCenter.position : transform.position;

        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        Gizmos.DrawCube(centre, volumeSize);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(centre, volumeSize);
    }

  
    private void HandleDebugToggles()
    {
        SyncToggle(LevelId.ArtistHitler, ref simulateArtistHitlerComplete, ref _lastSimHitler);
        SyncToggle(LevelId.AppleForest, ref simulateAppleForestComplete, ref _lastSimApple);
        SyncToggle(LevelId.WrongBeer, ref simulateWrongBeerComplete, ref _lastSimBeer);

        if (debugCollectAllParts)
        {
            debugCollectAllParts = false;
            for (int i = 0; i < partObjects.Count; i++) s_collectedParts.Add(i);
            RefreshMission();
        }

        if (debugResetMission)
        {
            debugResetMission = false;
            s_collectedParts.Clear();
            s_deloreanRepaired = false;
            _justRepaired = false;
            RefreshMission();
        }
    }

    private void SyncToggle(LevelId level, ref bool toggle, ref bool last)
    {
        var wsm = WorldStateManager.Instance;

        
        if (toggle != last)
            wsm.SetLevelCompleted(level, toggle);

        
        toggle = wsm.IsLevelCompleted(level);
        last = toggle;
    }

    private void SyncWithWorldState()
    {
        var wsm = WorldStateManager.Instance;

        bool hitler = wsm.IsLevelCompleted(LevelId.ArtistHitler);
        if (hitler != _hitlerApplied)
        {
            _hitlerApplied = hitler;
            if (hitler) RemoveHitlerObjects();
            else RestoreHitlerObjects();
        }

        bool apple = wsm.IsLevelCompleted(LevelId.AppleForest);
        if (apple != _appleApplied)
        {
            _appleApplied = apple;
            if (apple) StartFloating();
            else StopFloating();
            RefreshMission();
        }

        // Wrong Beer: Thandolwethu Your Task Bro !
    }

   
    private void CacheHitlerObjects()
    {
        _hitlerOriginalActive.Clear();
        foreach (var obj in objectsToRemove)
        {
            if (obj != null) _hitlerOriginalActive[obj] = obj.activeSelf;
        }
    }

    private void RemoveHitlerObjects()
    {
        foreach (var obj in objectsToRemove)
        {
            if (obj == null) continue;

            if (removeMode == RemoveMode.Destroy) Destroy(obj);
            else obj.SetActive(false);
        }
    }

    private void RestoreHitlerObjects()
    {
        if (removeMode == RemoveMode.Destroy) return;

        foreach (var kv in _hitlerOriginalActive)
        {
            if (kv.Key != null) kv.Key.SetActive(kv.Value);
        }
    }

    
    private MissionPhase ComputePhase()
    {
        if (partObjects.Count == 0) return MissionPhase.Inactive;
        if (!WorldStateManager.Instance.IsLevelCompleted(LevelId.AppleForest)) return MissionPhase.Inactive;
        if (s_deloreanRepaired) return MissionPhase.Complete;

        return s_collectedParts.Count >= partObjects.Count
            ? MissionPhase.ReturnToDelorean
            : MissionPhase.FindParts;
    }

    
    private void RefreshMission()
    {
        _phase = ComputePhase();

        int total = partObjects.Count;
        int have = Mathf.Min(s_collectedParts.Count, total);

        
        for (int i = 0; i < partObjects.Count; i++)
        {
            bool show = _phase == MissionPhase.FindParts && !s_collectedParts.Contains(i);

            if (partObjects[i] != null) partObjects[i].SetActive(show);
            if (i < _partBeams.Count && _partBeams[i] != null) _partBeams[i].go.SetActive(show);
        }

        if (_deloreanBeam != null)
            _deloreanBeam.go.SetActive(_phase == MissionPhase.ReturnToDelorean);

        
        bool showUI = _phase == MissionPhase.FindParts
                   || _phase == MissionPhase.ReturnToDelorean
                   || (_phase == MissionPhase.Complete && _justRepaired);

        SetMissionUIVisible(showUI);
        if (!showUI) return;

        if (partsCounterText != null)
            partsCounterText.text = string.Format(counterFormat, have, total);

        switch (_phase)
        {
            case MissionPhase.FindParts: SetMissionText(findPartsMission); break;
            case MissionPhase.ReturnToDelorean: SetMissionText(returnMission); break;
            case MissionPhase.Complete: SetMissionText(repairedMessage); break;
        }
    }

    private void UpdateMission()
    {
        
        if (_phase == MissionPhase.Complete && _justRepaired && Time.time >= _hideRepairedAt)
        {
            _justRepaired = false;
            RefreshMission();
        }

        if (_phase != MissionPhase.FindParts && _phase != MissionPhase.ReturnToDelorean) return;

        AnimateMissionVisuals();

        if (!TryGetPlayer(out Transform pl)) return;

        if (_phase == MissionPhase.FindParts)
        {
            for (int i = 0; i < partObjects.Count; i++)
            {
                if (partObjects[i] == null || s_collectedParts.Contains(i)) continue;

                if (Within(pl.position, partObjects[i].transform.position, pickupRadius))
                {
                    CollectPart(i, pl.position);
                    break; 
                }
            }
        }
        else 
        {
            if (delorean == null) return;

            bool inRange = Within(pl.position, delorean.position, installRadius);

            if (requireKeyToInstall)
            {
                SetMissionText(inRange ? string.Format(installPrompt, installKey) : returnMission);
                if (inRange && Input.GetKeyDown(installKey)) InstallParts(pl.position);
            }
            else if (inRange)
            {
                InstallParts(pl.position);
            }
        }
    }

    private void CollectPart(int index, Vector3 playerPos)
    {
        s_collectedParts.Add(index);

        if (partCollectedSound != null)
            AudioSource.PlayClipAtPoint(partCollectedSound, playerPos);

        RefreshMission();
    }

    private void InstallParts(Vector3 playerPos)
    {
        s_deloreanRepaired = true;
        _justRepaired = true;
        _hideRepairedAt = Time.time + repairedMessageDuration;

        if (repairedSound != null)
            AudioSource.PlayClipAtPoint(repairedSound, playerPos);

        RefreshMission();
        onDeloreanRepaired?.Invoke();
    }

    private bool TryGetPlayer(out Transform p)
    {
        
        if (player == null && Time.unscaledTime >= _nextPlayerSearch)
        {
            _nextPlayerSearch = Time.unscaledTime + 0.5f;
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }

        p = player;
        return p != null && p.gameObject.activeInHierarchy;
    }

    private static bool Within(Vector3 a, Vector3 b, float radius)
    {
        return (a - b).sqrMagnitude <= radius * radius;
    }

    
    private void SetMissionText(string text)
    {
        if (missionText == null || text == _lastMissionString) return;
        _lastMissionString = text;
        missionText.text = text;
    }

    private void SetMissionUIVisible(bool visible)
    {
        if (missionUIRoot != null) missionUIRoot.SetActive(visible);
        if (missionText != null) missionText.gameObject.SetActive(visible);
        if (partsCounterText != null) partsCounterText.gameObject.SetActive(visible);
    }

    
    private void BuildMissionBeams()
    {
        _partBeams.Clear();

        if (partObjects.Count == 0)
            Debug.LogWarning("[HubPrefabSwitcher] DeLorean mission: no parts assigned, so the mission will not start.");
        if (delorean == null)
            Debug.LogWarning("[HubPrefabSwitcher] DeLorean mission: no DeLorean assigned, so parts cannot be installed.");

        foreach (var part in partObjects)
            _partBeams.Add(part != null ? BuildBeam(part.transform, partBeamColor) : null);

        if (delorean != null)
            _deloreanBeam = BuildBeam(delorean, deloreanBeamColor);
    }

    private Beam BuildBeam(Transform target, Color color)
    {
        var go = new GameObject("MissionBeam_" + target.name);
        var lr = go.AddComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View; 
        lr.numCapVertices = 0;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.sharedMaterial = GetBeamMaterial();

       
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        lr.colorGradient = gradient;

        lr.startWidth = beamWidth;
        lr.endWidth = beamWidth;
        lr.SetPosition(0, target.position);
        lr.SetPosition(1, target.position + Vector3.up * beamHeight);

        go.SetActive(false);
        return new Beam { go = go, lr = lr, target = target };
    }

    private Material GetBeamMaterial()
    {
        if (beamMaterial != null) return beamMaterial;

        if (_builtMaterial == null)
        {
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh == null)
            {
                Debug.LogError("[HubPrefabSwitcher] No shader found for the mission beams. Assign a Beam Material.");
                return null;
            }
            _builtMaterial = new Material(sh);
        }
        return _builtMaterial;
    }

    private void AnimateMissionVisuals()
    {
        float width = beamWidth * (1f + Mathf.Sin(Time.time * beamPulseSpeed) * 0.15f);

        foreach (var b in _partBeams) UpdateBeam(b, width);
        UpdateBeam(_deloreanBeam, width);

        
        if (_phase == MissionPhase.FindParts)
        {
            float spin = partSpinSpeed * Time.deltaTime;
            for (int i = 0; i < partObjects.Count; i++)
            {
                if (partObjects[i] != null && partObjects[i].activeSelf && !s_collectedParts.Contains(i))
                    partObjects[i].transform.Rotate(0f, spin, 0f, Space.World);
            }
        }
    }

    private void UpdateBeam(Beam b, float width)
    {
        if (b == null || b.target == null || !b.go.activeSelf) return;

        Vector3 basePos = b.target.position;
        b.lr.SetPosition(0, basePos);
        b.lr.SetPosition(1, basePos + Vector3.up * beamHeight);
        b.lr.startWidth = width;
        b.lr.endWidth = width;
    }

    private void CacheFloatItems()
    {
        _floatItems.Clear();

        foreach (var obj in objectsToFloat)
        {
            if (obj == null) continue;

            var rb = obj.GetComponent<Rigidbody>();
            Bounds b = GetWorldBounds(obj);
            var cols = obj.GetComponentsInChildren<Collider>(true);
            var enabledStates = new bool[cols.Length];
            for (int i = 0; i < cols.Length; i++) enabledStates[i] = cols[i].enabled;

            _floatItems.Add(new FloatItem
            {
                t = obj.transform,
                rb = rb,
                originalPos = obj.transform.position,
                originalRot = obj.transform.rotation,
                wasKinematic = rb != null && rb.isKinematic,
                usedGravity = rb != null && rb.useGravity,
                boxCenterOffset = b.center - obj.transform.position,
                boxHalfExtents = b.extents,
                invOriginalRot = Quaternion.Inverse(obj.transform.rotation),
                colliders = cols,
                collidersWereEnabled = enabledStates,
                nextCastTime = Random.value * bounceCheckInterval 
            });
        }
    }

    private void StartFloating()
    {
        foreach (var item in _floatItems)
        {
            if (item.t == null) continue;

            
            if (item.rb != null)
            {
                item.rb.isKinematic = true;
                item.rb.useGravity = false;
            }

            if (disableCollidersWhileFloating)
            {
                foreach (var col in item.colliders)
                    if (col != null) col.enabled = false;
            }

            item.accumDt = 0f;
            item.target = RandomPointInVolume();
            item.velocity = Vector3.zero;
            item.maxSpeed = Random.Range(driftSpeedRange.x, driftSpeedRange.y);
            item.spinAxis = Random.onUnitSphere;
            item.spinSpeed = Random.Range(spinSpeedRange.x, spinSpeedRange.y);
        }
    }

    private void StopFloating()
    {
        foreach (var item in _floatItems)
        {
            if (item.t == null) continue;

            item.t.SetPositionAndRotation(item.originalPos, item.originalRot);

            for (int i = 0; i < item.colliders.Length; i++)
                if (item.colliders[i] != null) item.colliders[i].enabled = item.collidersWereEnabled[i];

            if (item.rb != null)
            {
                item.rb.isKinematic = item.wasKinematic;
                item.rb.useGravity = item.usedGravity;
            }
        }
    }

    private void UpdateFloating()
    {
        float dt = Time.deltaTime;
        float now = Time.time;

        
        int bounceMask = extraBounceLayers.value | 1;

        bool haveViewer = TryGetViewerPosition(out Vector3 viewerPos);
        float nearSqr = nearDistance * nearDistance;
        float cullSqr = cullDistance * cullDistance;

        for (int i = 0; i < _floatItems.Count; i++)
        {
            FloatItem item = _floatItems[i];
            if (item.t == null) continue;

            float stepDt = dt;
            float castInterval = bounceCheckInterval;

            if (haveViewer)
            {
                float distSqr = (item.t.position - viewerPos).sqrMagnitude;

                
                if (cullDistance > 0f && distSqr > cullSqr) continue;

                
                if (distSqr > nearSqr)
                {
                    item.accumDt += dt;
                    if (item.accumDt < farUpdateInterval) continue;

                    stepDt = item.accumDt;
                    item.accumDt = 0f;
                    castInterval = farUpdateInterval;
                    item.nextCastTime = 0f; 
                }
            }

            
            if (now >= item.nextCastTime)
            {
                item.nextCastTime = now + castInterval;
                CheckBounce(item, item.t.position, bounceMask, castInterval);
            }

            StepItem(item, stepDt);
        }
    }

    private void StepItem(FloatItem item, float dt)
    {
        Transform t = item.t;
        Vector3 pos = t.position;

        
        if ((pos - item.target).sqrMagnitude < arriveDistance * arriveDistance)
            item.target = RandomPointInVolume();

        Vector3 newPos = Vector3.SmoothDamp(
            pos, item.target, ref item.velocity, driftSmoothTime, item.maxSpeed, dt);

        Quaternion newRot = Quaternion.AngleAxis(item.spinSpeed * dt, item.spinAxis) * t.rotation;

        
        t.SetPositionAndRotation(newPos, newRot);
    }


    private void CheckBounce(FloatItem item, Vector3 pos, int mask, float interval)
    {
        Vector3 vel = item.velocity;
        float speed = vel.magnitude;
        Vector3 dir = speed > 0.05f ? vel / speed : (item.target - pos).normalized;
        if (dir.sqrMagnitude < 0.001f) return;

        float distance = speed * interval + bounceLookAhead;

        if (TryFindSurface(item, pos, dir, distance, mask, out RaycastHit hit))
            Bounce(item, pos, dir, hit.normal);
    }

    private bool TryGetViewerPosition(out Vector3 pos)
    {
        if ((_viewer == null || !_viewer.gameObject.activeInHierarchy) && Time.unscaledTime >= _nextViewerSearch)
        {
            _nextViewerSearch = Time.unscaledTime + 1f;
            var cam = Camera.main;
            _viewer = cam != null ? cam.transform : null;
        }

        pos = _viewer != null ? _viewer.position : Vector3.zero;
        return _viewer != null;
    }

    private void Bounce(FloatItem item, Vector3 pos, Vector3 dir, Vector3 normal)
    {
        Vector3 reflected = Vector3.Reflect(dir, normal);
        reflected = (reflected + Random.insideUnitSphere * bounceRandomness).normalized;

        
        if (Vector3.Dot(reflected, normal) < 0.1f)
            reflected = (reflected + normal).normalized;

        item.target = ClampToVolume(pos + reflected * bounceTravelDistance);
        item.velocity = reflected * Mathf.Max(item.velocity.magnitude, 0.5f);
    }

    private Vector3 ClampToVolume(Vector3 p)
    {
        Vector3 c = VolumeCentre;
        Vector3 half = volumeSize * 0.5f;
        return new Vector3(
            Mathf.Clamp(p.x, c.x - half.x, c.x + half.x),
            Mathf.Clamp(p.y, c.y - half.y, c.y + half.y),
            Mathf.Clamp(p.z, c.z - half.z, c.z + half.z));
    }

    private static Bounds GetWorldBounds(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.one);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

   
    private bool TryFindSurface(FloatItem item, Vector3 pos, Vector3 dir, float distance,
                                int mask, out RaycastHit best)
    {
        Quaternion spin = item.t.rotation * item.invOriginalRot;
        Vector3 centre = pos + spin * item.boxCenterOffset;
        Vector3 half = item.boxHalfExtents * bounceSizeScale;

        int count = Physics.BoxCastNonAlloc(centre, half, dir, _hitBuffer, spin,
                                            distance, mask, QueryTriggerInteraction.Ignore);

        best = default;
        bool found = false;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            RaycastHit h = _hitBuffer[i];
            if (h.collider == null || h.collider.transform.IsChildOf(item.t)) continue; 
            if (h.distance >= bestDistance) continue;

            bestDistance = h.distance;
            best = h;
            found = true;
        }

        
        if (found && best.normal.sqrMagnitude < 0.001f) best.normal = -dir;
        return found;
    }

    private Vector3 RandomPointInVolume()
    {
        Vector3 half = volumeSize * 0.5f;
        return VolumeCentre + new Vector3(
            Random.Range(-half.x, half.x),
            Random.Range(-half.y, half.y),
            Random.Range(-half.z, half.z));
    }
}