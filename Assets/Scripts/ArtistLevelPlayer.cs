using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UEA = UnityEditor.Animations;
#endif



public enum WeaponType { Fist, Rifle, Bazooka, Stone, Bat = Fist }

public static class AnimNames
{
    public static readonly string[] Locomotion = { "Basic Locomotion", "BasicLocomotion", "Locomotion" };

    public static readonly string[] RifleIdle = { "Rifle.RifleLocomotion", "RifleLocomotion" };
    public static readonly string[] RifleAim = { "Rifle.RifleAim", "RifleAim" };
    public static readonly string[] RifleShoot = { "Rifle.Shooting", "Shooting" };
    public static readonly string[] RifleReload = { "Rifle.ReloadAssaultRifle", "ReloadAssaultRifle" };

    public static readonly string[] BazookaIdle = { "Bazooka.BazookaLocomotion", "Bazooka.BazookaIdle", "Bazooka.Locomotion", "Bazooka.Idle", "BazookaLocomotion", "BazookaIdle" };
    public static readonly string[] BazookaAim = { "Bazooka.BazookaAim", "Bazooka.Aim", "BazookaAim" };
    public static readonly string[] BazookaShoot = { "Bazooka.BazookaShooting", "Bazooka.BazookaFire", "Bazooka.Shooting", "Bazooka.Fire", "BazookaShooting" };
    public static readonly string[] BazookaReload = { "Bazooka.ReloadBazooka", "Bazooka.BazookaReload", "Bazooka.Reload", "ReloadBazooka" };

    public static readonly string[] Throw = { "Throw" };

    public const int MeleeAttackCount = 5;
    public static string[] MeleeAttack(int i)
    {
        return new[] { "SingleHandMeleeAttack.SingleAttack" + i, "SingleAttack" + i };
    }
}


public static class WeaponModelFactory
{
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    static Material baseMaterial;

    public static GameObject Build(WeaponType type, string name)
    {
        GameObject root = new GameObject(name);
        Transform t = root.transform;

        switch (type)
        {
            case WeaponType.Rifle: BuildRifle(t); break;
            case WeaponType.Bazooka: BuildBazooka(t); break;
            case WeaponType.Stone: BuildStone(t); break;
            default: BuildBat(t); break;
        }
        return root;
    }

    // ---------------------------------------------------------------- models
    static void BuildRifle(Transform t)
    {
        Color metal = new Color(0.14f, 0.14f, 0.15f);
        Color dark = new Color(0.07f, 0.07f, 0.08f);
        Color wood = new Color(0.36f, 0.22f, 0.11f);

        Part(t, PrimitiveType.Cube, "Receiver", new Vector3(0f, 0.045f, 0.10f), new Vector3(0.055f, 0.085f, 0.42f), Vector3.zero, metal, 0.6f, 0.5f);
        Part(t, PrimitiveType.Cube, "Handguard", new Vector3(0f, 0.035f, 0.43f), new Vector3(0.065f, 0.065f, 0.24f), Vector3.zero, wood, 0f, 0.3f);
        Tube(t, "Barrel", new Vector3(0f, 0.05f, 0.72f), 0.30f, 0.022f, dark);
        Tube(t, "Muzzle", new Vector3(0f, 0.05f, 0.88f), 0.05f, 0.036f, dark);
        Part(t, PrimitiveType.Cube, "Stock", new Vector3(0f, 0f, -0.22f), new Vector3(0.05f, 0.10f, 0.26f), new Vector3(-6f, 0f, 0f), wood, 0f, 0.3f);
        Part(t, PrimitiveType.Cube, "Grip", new Vector3(0f, -0.065f, -0.01f), new Vector3(0.045f, 0.13f, 0.05f), new Vector3(14f, 0f, 0f), dark, 0.1f, 0.3f);
        Part(t, PrimitiveType.Cube, "Magazine", new Vector3(0f, -0.105f, 0.15f), new Vector3(0.04f, 0.17f, 0.07f), new Vector3(-10f, 0f, 0f), metal, 0.6f, 0.5f);
        Part(t, PrimitiveType.Cube, "ScopeMount", new Vector3(0f, 0.098f, 0.12f), new Vector3(0.03f, 0.025f, 0.10f), Vector3.zero, dark, 0.3f, 0.4f);
        Tube(t, "Scope", new Vector3(0f, 0.125f, 0.12f), 0.17f, 0.04f, dark);
        Part(t, PrimitiveType.Cube, "FrontSight", new Vector3(0f, 0.085f, 0.82f), new Vector3(0.01f, 0.04f, 0.01f), Vector3.zero, dark, 0.3f, 0.4f);
    }

    static void BuildBazooka(Transform t)
    {
        Color olive = new Color(0.26f, 0.33f, 0.20f);
        Color dark = new Color(0.08f, 0.08f, 0.09f);
        Color red = new Color(0.65f, 0.12f, 0.08f);

        Tube(t, "Tube", new Vector3(0f, 0.11f, 0.10f), 1.0f, 0.12f, olive);
        Tube(t, "FrontRim", new Vector3(0f, 0.11f, 0.60f), 0.07f, 0.15f, dark);
        Tube(t, "RearFlare", new Vector3(0f, 0.11f, -0.42f), 0.12f, 0.16f, dark);
        Tube(t, "BandA", new Vector3(0f, 0.11f, 0.32f), 0.03f, 0.135f, dark);
        Tube(t, "BandB", new Vector3(0f, 0.11f, -0.12f), 0.03f, 0.135f, dark);
        Part(t, PrimitiveType.Sphere, "Warhead", new Vector3(0f, 0.11f, 0.64f), new Vector3(0.10f, 0.10f, 0.15f), Vector3.zero, red, 0.2f, 0.4f);
        Part(t, PrimitiveType.Cube, "Grip", new Vector3(0f, -0.03f, 0f), new Vector3(0.04f, 0.13f, 0.05f), new Vector3(10f, 0f, 0f), dark, 0.1f, 0.3f);
        Part(t, PrimitiveType.Cube, "ForeGrip", new Vector3(0f, 0f, 0.30f), new Vector3(0.035f, 0.11f, 0.035f), Vector3.zero, dark, 0.1f, 0.3f);
        Part(t, PrimitiveType.Cube, "Sight", new Vector3(0.08f, 0.20f, 0.10f), new Vector3(0.02f, 0.07f, 0.10f), Vector3.zero, dark, 0.3f, 0.4f);
    }

    static void BuildBat(Transform t)
    {
        Color wood = new Color(0.72f, 0.52f, 0.30f);
        Color tape = new Color(0.10f, 0.10f, 0.11f);

        Tube(t, "Handle", new Vector3(0f, 0f, 0.03f), 0.30f, 0.03f, wood);
        Tube(t, "Tape", new Vector3(0f, 0f, 0.0f), 0.16f, 0.034f, tape);
        Tube(t, "Taper", new Vector3(0f, 0f, 0.28f), 0.22f, 0.05f, wood);
        Tube(t, "Head", new Vector3(0f, 0f, 0.60f), 0.42f, 0.07f, wood);
        Part(t, PrimitiveType.Sphere, "EndCap", new Vector3(0f, 0f, 0.80f), new Vector3(0.07f, 0.07f, 0.07f), Vector3.zero, wood, 0f, 0.3f);
        Part(t, PrimitiveType.Sphere, "Knob", new Vector3(0f, 0f, -0.125f), new Vector3(0.045f, 0.045f, 0.045f), Vector3.zero, wood, 0f, 0.3f);
    }

    static void BuildStone(Transform t)
    {
        Color rock = new Color(0.42f, 0.42f, 0.40f);
        Color rock2 = new Color(0.34f, 0.34f, 0.33f);

        Part(t, PrimitiveType.Sphere, "Rock", Vector3.zero, new Vector3(0.11f, 0.08f, 0.10f), new Vector3(10f, 25f, 5f), rock, 0f, 0.1f);
        Part(t, PrimitiveType.Sphere, "LumpA", new Vector3(0.03f, 0.02f, 0.02f), new Vector3(0.06f, 0.05f, 0.05f), new Vector3(0f, 40f, 0f), rock2, 0f, 0.1f);
        Part(t, PrimitiveType.Sphere, "LumpB", new Vector3(-0.03f, -0.015f, -0.02f), new Vector3(0.05f, 0.05f, 0.06f), new Vector3(20f, 0f, 10f), rock2, 0f, 0.1f);
    }

    // --------------------------------------------------------------- helpers
    static void Tube(Transform parent, string name, Vector3 center, float length, float diameter, Color color)
    {
        
        Part(parent, PrimitiveType.Cylinder, name, center,
             new Vector3(diameter, length * 0.5f, diameter), new Vector3(90f, 0f, 0f), color, 0.4f, 0.4f);
    }

    static GameObject Part(Transform parent, PrimitiveType prim, string name, Vector3 pos, Vector3 scale,
                           Vector3 euler, Color color, float metallic, float smooth)
    {
        GameObject go = GameObject.CreatePrimitive(prim);
        go.name = name;

        Collider col = go.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        Transform tr = go.transform;
        tr.SetParent(parent, false);
        tr.localPosition = pos;
        tr.localRotation = Quaternion.Euler(euler);
        tr.localScale = scale;

        go.GetComponent<Renderer>().sharedMaterial = GetMaterial(color, metallic, smooth);
        return go;
    }

    static Material GetMaterial(Color c, float metallic, float smooth)
    {
        string key = ColorUtility.ToHtmlStringRGB(c) + "_" + metallic.ToString("0.0") + "_" + smooth.ToString("0.0");
        Material m;
        if (materials.TryGetValue(key, out m) && m != null) return m;

        if (baseMaterial == null)
        {
            
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMaterial = tmp.GetComponent<Renderer>().sharedMaterial;
            Object.DestroyImmediate(tmp);
        }

        m = new Material(baseMaterial);
        m.name = "Proc_" + key;
        m.hideFlags = HideFlags.HideAndDontSave;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);

        materials[key] = m;
        return m;
    }
}


[System.Serializable]
public class StanceRules
{
    public List<string> boolNames = new List<string>();     
    public List<bool> boolHold = new List<bool>();
    public List<string> floatForward = new List<string>();  
    public List<string> floatSide = new List<string>();     

    [System.NonSerialized] public List<int> boolHashes = new List<int>();
    [System.NonSerialized] public List<bool> boolValues = new List<bool>();
    [System.NonSerialized] public List<int> forwardHashes = new List<int>();
    [System.NonSerialized] public List<int> sideHashes = new List<int>();
}

[System.Serializable]
public class WeaponSlot
{
    public string displayName = "Weapon";
    public WeaponType type = WeaponType.Fist;

    [Tooltip("OPTIONAL. Leave empty - the model is generated from code. Only used if 'Generate Models From Code' is off.")]
    public GameObject prefab;
    [Tooltip("Optional icon for the weapon bar. If empty, only the name is shown.")]
    public Sprite icon;

    [Header("Hand placement (fine tuning, in the weapon's own space)")]
    [Tooltip("Offset of the weapon relative to the palm (metres).")]
    public Vector3 holdPosition = Vector3.zero;
    [Tooltip("Extra rotation on top of the automatic alignment.")]
    public Vector3 holdEuler = Vector3.zero;
    public Vector3 holdScale = Vector3.one;

    [Header("Combat")]
    public float damage = 10f;
    [Tooltip("Rifle/Bazooka: max shooting distance. Bat: how far in front of the player the swing hits.")]
    public float range = 100f;
    [Tooltip("Bat: swing hit radius. Bazooka: explosion radius.")]
    public float radius = 0f;
    public float cooldown = 0.5f;
    [Tooltip("Delay between click and the actual hit / throw release (sync with the animation).")]
    public float hitDelay = 0f;
    [Tooltip("How long the player can't move after attacking.")]
    public float moveLockTime = 0f;
    public bool automatic = false;
    [Tooltip("How far the sound of this weapon carries. Stone = impact noise radius. 0 = silent.")]
    public float noiseRadius = 0f;
    [Tooltip("Rifle: bullet impact effect. Bazooka: explosion effect.")]
    public GameObject effectPrefab;
    [Tooltip("Camera field of view while aiming. 0 = no zoom.")]
    public float aimFov = 0f;

    [Header("Ammo")]
    public bool infiniteAmmo = false;
    [Tooltip("0 = no clip (stone just uses Reserve Ammo as its count).")]
    public int clipSize = 0;
    public int reserveAmmo = 0;
    public float reloadTime = 1.5f;

    [HideInInspector] public int ammoInClip;
    [HideInInspector] public GameObject held;
    [HideInInspector] public bool calibratedIdle;
    [HideInInspector] public bool calibrated;

    public bool UsesClip { get { return clipSize > 0 && !infiniteAmmo; } }
}

[DefaultExecutionOrder(200)]   
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class ArtistLevelPlayer : MonoBehaviour
{
    
    [Header("Player Health & Energy")]
    private float playerHealth = 200f;
    public float presentHealth;
    public HealthBar healthbar;
    private float playerEnergy = 100f;
    public float presentEnergy;
    public EnergyBar energybar;
    public float staminaDrainPerSecond = 15f;
    public float staminaRegenPerSecond = 10f;
    public GameObject DamageIndicator;

    // ------------------------------------------------------------------ Movement
    [Header("Player Movement")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float aimWalkSpeed = 1.5f;
    public float movementSpeed = 3f;
    public float rotSpeed = 450f;
    public MainCameraController MCC;
    public EnvironmentChecker environmentChecker;

    [Header("Player Animator")]
    public Animator animator;
    [Tooltip("Animator layer that holds the states (0 = Base Layer).")]
    public int animationLayer = 0;
    [Tooltip("Print a warning in the Console when an animator state can't be found.")]
    public bool logAnimatorWarnings = true;

    [Header("Player Collision & Gravity")]
    public CharacterController CC;
    public float surfaceCheckRadious = 0.1f;
    public Vector3 surfaceCheckOffset;
    public LayerMask surfaceLayer;

    // ------------------------------------------------------------------ Weapons
    [Header("Weapons (scroll wheel / 1-4 to switch)")]
    public WeaponSlot[] weapons;
    [Tooltip("Right hand bone. If empty, it is found automatically.")]
    public Transform handSocket;
    [Tooltip("What bullets / punches / explosions can hit.")]
    public LayerMask hitMask = ~0;
    [Tooltip("ON = weapon models are built from code (recommended). OFF = use the prefab of each slot.")]
    public bool generateModelsFromCode = true;
    [Tooltip("OFF (recommended): the script finds the real hand bone itself and creates its own 'WeaponSocket (auto)' in the palm - you can delete ShootingArea. ON: use the Hand Socket above exactly as assigned.")]
    public bool useManualHandSocket = false;
    [Tooltip("Draws a cyan marker in the Scene view where the weapon is held. Watch it follow the hand while you play.")]
    public bool drawSocketGizmo = true;

#if UNITY_EDITOR
    [Header("Editor Preview")]
    [Tooltip("Check this box to see the weapon on the hand inside the Scene View.")]
    public bool previewInEditor = false;
    [Tooltip("Which weapon slot index to preview (0 = Bat, 1 = Rifle, 2 = Bazooka, 3 = Stone).")]
    public int previewWeaponIndex = 0;
    private GameObject editorPreviewObj;
#endif

    [Header("Stone throwing")]
    public float throwSpeed = 14f;
    [Tooltip("Adds upward arc to the throw (0 = straight where camera looks).")]
    public float throwArc = 0.25f;
    public float stoneMass = 0.3f;
    public float stoneMinImpactSpeed = 1.5f;
    public AudioClip stoneImpactSound;
    public int trajectoryPoints = 40;
    public float trajectoryTimeStep = 0.05f;

    [Header("Aiming / UI")]
    public float aimLerpSpeed = 10f;
    public Font uiFont;

    [Header("Over-the-shoulder aim camera")]
    [Tooltip("Move the camera to the player's shoulder while the right mouse button is held.")]
    public bool shoulderCamera = true;
    [Tooltip("Camera-space offset at full aim: X = right (use a negative value for the left shoulder), Y = up, Z = forward (closer).")]
    public Vector3 shoulderOffset = new Vector3(0.65f, 0.15f, 0.9f);
    public float shoulderLerpSpeed = 9f;

    
    [SerializeField, HideInInspector] string[] discoveredStates;
    [SerializeField, HideInInspector] StanceRules rifleRules = new StanceRules();
    [SerializeField, HideInInspector] StanceRules bazookaRules = new StanceRules();

    // ------------------------------------------------------------------ Private state
    Camera cam;
    float defaultFov = 60f;

    bool playerControl = true;
    bool isDead;
    bool onSurface;
    float fallingSpeed;
    float moveAmount;
    Quaternion requiredRotation;
    public bool playerOnLedge { get; set; }
    public LedgeInfo LedgeInfo { get; set; }

    int currentIndex = -1;
    bool isAiming;
    bool wasAiming;
    float aimStartTime;
    float aimBlend;
    Vector3 camLastFinalPos;
    bool camShiftValid;
    bool isReloading;
    float reloadEndTime;
    float nextFireTime;
    float actionLockUntil;
    float moveLockUntil;
    float equipTime;
    bool warnedNoHand;
    Transform autoSocket;
    const string AutoSocketName = "WeaponSocket (auto)";
    bool handMonitorDone, handFirstSet, handMoved;
    Vector3 handFirstLocal;
    float movingTime;

    readonly RaycastHit[] rayBuffer = new RaycastHit[32];

    // ------------------------------------------------------------------ Animation state
    class AnimSet
    {
        public int idle;
        public int aim;
        public int reload;
        public readonly List<int> attacks = new List<int>();
        public int comboIndex;
    }

    struct AnimParam
    {
        public int hash;
        public string lower;
        public AnimatorControllerParameterType type;
    }

    AnimSet[] animSets;
    int locoHash;
    int currentAnimHash;
    bool hasCurrentAnim;
    float attackAnimUntil;
    float lastAttackTime;
    float lastAnimPlayTime;
    float driftSince = -1f;
    readonly Dictionary<int, int> shortHashOf = new Dictionary<int, int>();

    readonly List<AnimParam> animParams = new List<AnimParam>();
    readonly Dictionary<int, AnimatorControllerParameterType> paramTypes = new Dictionary<int, AnimatorControllerParameterType>();
    readonly List<int> moveAliasParams = new List<int>();
    bool hasMovementValue, hasOnSurface;
    static readonly int MovementValueHash = Animator.StringToHash("movementValue");
    static readonly int OnSurfaceHash = Animator.StringToHash("onSurface");

    static readonly string[] RifleFlags = { "rifle", "hasrifle", "isrifle", "rifleequipped", "equiprifle", "riflemode" };
    static readonly string[] BazookaFlags = { "bazooka", "hasbazooka", "isbazooka", "bazookaequipped", "equipbazooka", "bazookamode" };
    static readonly string[] MeleeFlags = { "melee", "singlehandmelee", "singlehandmeleeattack", "punch", "bat", "swing" };
    static readonly string[] AimFlags = { "aim", "aiming", "isaiming", "rifleaim", "bazookaaim" };
    static readonly string[] ShootFlags = { "shoot", "shooting", "isshooting", "fire", "firing" };
    static readonly string[] ReloadFlags = { "reload", "reloading", "isreloading" };
    static readonly string[] MoveAliases = { "speed", "movespeed", "moveamount", "movement", "movementspeed", "locomotion", "blend", "walkspeed" };

    // UI
    class SlotUI
    {
        public RectTransform root;
        public Image bg, border, icon;
        public Text nameText, ammoText;
    }
    Canvas canvas;
    readonly List<SlotUI> slotUIs = new List<SlotUI>();
    RectTransform reticleRoot;
    readonly List<Image> reticleParts = new List<Image>();
    float reticleGap = 14f;
    float reticleKick;
    bool reticleIsRed;
    Text messageText;
    float messageUntil;
    bool uiDirty = true;
    LineRenderer trajectoryLine;

    public WeaponSlot CurrentWeapon { get { return weapons[Mathf.Max(0, currentIndex)]; } }

    // ====================================================================== SETUP
    static WeaponSlot[] CreateDefaultWeapons()
    {
        return new WeaponSlot[]
        {
            new WeaponSlot
            {
                displayName = "Bat", type = WeaponType.Fist,
                damage = 25f, range = 1.5f, radius = 1.0f,
                cooldown = 0.6f, hitDelay = 0.2f, moveLockTime = 0.5f,
                infiniteAmmo = true
            },
            new WeaponSlot
            {
                displayName = "Rifle", type = WeaponType.Rifle,
                damage = 25f, range = 120f, cooldown = 0.45f,
                clipSize = 8, reserveAmmo = 40, reloadTime = 1.8f,
                noiseRadius = 45f, aimFov = 40f
            },
            new WeaponSlot
            {
                displayName = "Bazooka", type = WeaponType.Bazooka,
                damage = 120f, range = 100f, radius = 8f, cooldown = 1.2f,
                clipSize = 1, reserveAmmo = 5, reloadTime = 2.5f,
                noiseRadius = 70f, aimFov = 50f
            },
            new WeaponSlot
            {
                displayName = "Stone", type = WeaponType.Stone,
                cooldown = 0.9f, hitDelay = 0.35f, moveLockTime = 0.6f,
                reserveAmmo = 10, noiseRadius = 25f, aimFov = 55f
            }
        };
    }

    void Reset()
    {
        weapons = CreateDefaultWeapons();
    }

    private void Awake()
    {
#if UNITY_EDITOR
        ClearEditorPreview();
#endif

        if (CC == null) CC = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (weapons == null || weapons.Length == 0) weapons = CreateDefaultWeapons();
        if (rifleRules == null) rifleRules = new StanceRules();
        if (bazookaRules == null) bazookaRules = new StanceRules();

#if UNITY_EDITOR
        AutoSetupFromController(false);
#endif

        presentHealth = playerHealth;
        presentEnergy = playerEnergy;
        if (healthbar != null) healthbar.GiveFUllHealth(presentHealth);
        if (energybar != null) energybar.GiveFUllEnergy(presentEnergy);

        foreach (WeaponSlot w in weapons)
        {
            w.ammoInClip = w.clipSize;
            w.calibrated = false;
            w.calibratedIdle = false;
        }

        requiredRotation = transform.rotation;
        movementSpeed = walkSpeed;
    }

    private void Start()
    {
        cam = Camera.main;
        if (cam != null) defaultFov = cam.fieldOfView;

        CacheAnimatorParameters();
        PrepareRules(rifleRules);
        PrepareRules(bazookaRules);
        BuildAnimSets();
        BuildHeldModels();
        BuildUI();
        BuildTrajectoryLine();
        SelectWeapon(0, true);
    }

    private void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
        if (trajectoryLine != null) Destroy(trajectoryLine.gameObject);
    }

    // ====================================================================== EDITOR: PREVIEW + CONTROLLER SCAN
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;

        UnityEditor.EditorApplication.delayCall -= EditorDelayed;
        UnityEditor.EditorApplication.delayCall += EditorDelayed;
    }

    void EditorDelayed()
    {
        if (this == null) return;
        AutoSetupFromController(false);
        RefreshEditorPreview();
    }

    public void RefreshEditorPreview()
    {
        if (this == null) return;
        ClearEditorPreview();

        if (!previewInEditor || weapons == null || weapons.Length == 0) return;

        previewWeaponIndex = Mathf.Clamp(previewWeaponIndex, 0, weapons.Length - 1);
        WeaponSlot slot = weapons[previewWeaponIndex];
        if (slot == null) return;

        Transform socket = GetHandSocket();
        if (socket == null) return;

        editorPreviewObj = CreateHeld(slot, socket);
        editorPreviewObj.name = "[PREVIEW] " + slot.displayName;
        SetHideFlagsRecursive(editorPreviewObj, HideFlags.DontSave);
    }

    static void SetHideFlagsRecursive(GameObject go, HideFlags flags)
    {
        go.hideFlags = flags;
        foreach (Transform child in go.transform)
            SetHideFlagsRecursive(child.gameObject, flags);
    }

    public void ClearEditorPreview()
    {
        if (editorPreviewObj != null)
        {
            DestroyImmediate(editorPreviewObj);
            editorPreviewObj = null;
        }

        // remove leftover previews anywhere under the player (e.g. under an old ShootingArea)
        List<GameObject> leftovers = new List<GameObject>();
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t != null && t != transform && t.name.StartsWith("[PREVIEW]")) leftovers.Add(t.gameObject);
        foreach (GameObject go in leftovers)
            if (go != null) DestroyImmediate(go);
    }

    // ---------------------------------------------------------------- controller scan
    UEA.AnimatorController GetEditorController()
    {
        Animator a = animator != null ? animator : GetComponentInChildren<Animator>();
        if (a == null || a.runtimeAnimatorController == null) return null;

        RuntimeAnimatorController rc = a.runtimeAnimatorController;
        UEA.AnimatorController ctrl = rc as UEA.AnimatorController;
        if (ctrl == null)
        {
            AnimatorOverrideController ov = rc as AnimatorOverrideController;
            if (ov != null) ctrl = ov.runtimeAnimatorController as UEA.AnimatorController;
        }
        return ctrl;
    }

    [ContextMenu("Re-scan Animator Controller")]
    void RescanAnimatorController()
    {
        AutoSetupFromController(true);
    }

    /// <summary>
    /// Reads your Animator Controller (Editor only) and stores:
    ///  - the real state names inside the Rifle / Bazooka sub-state machines
    ///  - which bool parameters stop the controller from leaving the weapon stance
    ///  - which blend-tree floats drive the walk cycle of those stances
    /// The results are serialized, so builds use them too.
    /// </summary>
    void AutoSetupFromController(bool force)
    {
        if (!force && discoveredStates != null && discoveredStates.Length == 8
            && (!string.IsNullOrEmpty(discoveredStates[0]) || !string.IsNullOrEmpty(discoveredStates[4])))
            return;

        UEA.AnimatorController ctrl = GetEditorController();
        if (ctrl == null || ctrl.layers.Length == 0) return;

        if (rifleRules == null) rifleRules = new StanceRules();
        if (bazookaRules == null) bazookaRules = new StanceRules();

        int layer = Mathf.Clamp(animationLayer, 0, ctrl.layers.Length - 1);
        UEA.AnimatorStateMachine root = ctrl.layers[layer].stateMachine;

        HashSet<string> bools = new HashSet<string>();
        foreach (UnityEngine.AnimatorControllerParameter p in ctrl.parameters)
            if (p.type == AnimatorControllerParameterType.Bool) bools.Add(p.name);

        string[] found = new string[8];
        System.Text.StringBuilder log = new System.Text.StringBuilder("[ArtistLevelPlayer] Animator scan\n");

        ScanWeaponMachine(root, "rifle", 0, found, rifleRules, bools, log);
        ScanWeaponMachine(root, "bazooka", 4, found, bazookaRules, bools, log);

        discoveredStates = found;
        if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log(log.ToString(), this);
    }

    void ScanWeaponMachine(UEA.AnimatorStateMachine root, string key, int offset, string[] found,
                           StanceRules rules, HashSet<string> bools, System.Text.StringBuilder log)
    {
        rules.boolNames.Clear();
        rules.boolHold.Clear();
        rules.floatForward.Clear();
        rules.floatSide.Clear();

        string path;
        UEA.AnimatorStateMachine m = FindMachine(root, key, "", out path);
        if (m == null)
        {
            log.AppendLine("  - no '" + key + "' sub-state machine found");
            return;
        }

        HashSet<UEA.AnimatorState> inside = new HashSet<UEA.AnimatorState>();
        foreach (UEA.ChildAnimatorState cs in m.states) inside.Add(cs.state);

        foreach (UEA.ChildAnimatorState cs in m.states)
        {
            UEA.AnimatorState st = cs.state;
            string n = st.name.ToLowerInvariant();
            string statePath = path + "." + st.name;

            if (n.Contains("reload")) found[offset + 3] = statePath;
            else if (n.Contains("aim")) found[offset + 1] = statePath;
            else if (n.Contains("shoot") || n.Contains("fire") || n.Contains("attack")) found[offset + 2] = statePath;

            CollectBlendParams(st.motion, rules);

            foreach (UEA.AnimatorStateTransition t in st.transitions)
            {
                bool leaves = t.isExit
                    || (t.destinationState != null && !inside.Contains(t.destinationState))
                    || (t.destinationState == null && t.destinationStateMachine != null && t.destinationStateMachine != m);
                if (!leaves) continue;

                bool handled = false;
                foreach (UEA.AnimatorCondition c in t.conditions)
                {
                    bool isBool = c.mode == UEA.AnimatorConditionMode.If || c.mode == UEA.AnimatorConditionMode.IfNot;
                    if (isBool && bools.Contains(c.parameter))
                    {
                        // to NOT take this transition keep the opposite of what it waits for
                        bool hold = c.mode == UEA.AnimatorConditionMode.IfNot;
                        if (!rules.boolNames.Contains(c.parameter))
                        {
                            rules.boolNames.Add(c.parameter);
                            rules.boolHold.Add(hold);
                        }
                        handled = true;
                        break;
                    }
                }

                if (!handled)
                    log.AppendLine("  ! '" + st.name + "' can leave the " + key + " stance by exit-time / a numeric condition - can't be held automatically");
            }
        }

        if (m.defaultState != null) found[offset] = path + "." + m.defaultState.name;

        log.AppendLine("  " + key + ": idle=" + found[offset] + " aim=" + found[offset + 1]
                       + " shoot=" + found[offset + 2] + " reload=" + found[offset + 3]);
        log.AppendLine("    hold bools: " + (rules.boolNames.Count == 0 ? "(none)" : BoolRuleString(rules))
                       + " | walk floats: " + string.Join(",", rules.floatForward.ToArray()));
    }

    static string BoolRuleString(StanceRules r)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < r.boolNames.Count; i++)
            sb.Append(r.boolNames[i]).Append("=").Append(r.boolHold[i]).Append(" ");
        return sb.ToString();
    }

    static UEA.AnimatorStateMachine FindMachine(UEA.AnimatorStateMachine parent, string key, string parentPath, out string path)
    {
        foreach (UEA.ChildAnimatorStateMachine sub in parent.stateMachines)
        {
            string p = string.IsNullOrEmpty(parentPath) ? sub.stateMachine.name : parentPath + "." + sub.stateMachine.name;
            if (sub.stateMachine.name.ToLowerInvariant().Contains(key))
            {
                path = p;
                return sub.stateMachine;
            }

            string inner;
            UEA.AnimatorStateMachine r = FindMachine(sub.stateMachine, key, p, out inner);
            if (r != null)
            {
                path = inner;
                return r;
            }
        }
        path = null;
        return null;
    }

    static void CollectBlendParams(Motion mo, StanceRules r)
    {
        UEA.BlendTree bt = mo as UEA.BlendTree;
        if (bt == null) return;

        if (bt.blendType == UEA.BlendTreeType.Simple1D)
        {
            AddUnique(r.floatForward, bt.blendParameter);
        }
        else if (bt.blendType != UEA.BlendTreeType.Direct)
        {
            AddUnique(r.floatSide, bt.blendParameter);
            AddUnique(r.floatForward, bt.blendParameterY);
        }

        foreach (UEA.ChildMotion cm in bt.children)
            CollectBlendParams(cm.motion, r);
    }

    static void AddUnique(List<string> list, string s)
    {
        if (!string.IsNullOrEmpty(s) && !list.Contains(s)) list.Add(s);
    }

    [ContextMenu("Log All Animator States")]
    void LogAllAnimatorStates()
    {
        UEA.AnimatorController ctrl = GetEditorController();
        if (ctrl == null)
        {
            Debug.LogWarning("No Animator / controller found.");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder("=== Animator states ===\n");
        foreach (UEA.AnimatorControllerLayer layer in ctrl.layers)
            DumpMachine(layer.stateMachine, layer.name, sb);

        sb.AppendLine("=== Parameters ===");
        foreach (UnityEngine.AnimatorControllerParameter p in ctrl.parameters)
            sb.AppendLine(p.name + "  (" + p.type + ")");

        Debug.Log(sb.ToString(), this);
    }

    static void DumpMachine(UEA.AnimatorStateMachine m, string path, System.Text.StringBuilder sb)
    {
        foreach (UEA.ChildAnimatorState cs in m.states)
            sb.AppendLine(path + "." + cs.state.name);
        foreach (UEA.ChildAnimatorStateMachine sub in m.stateMachines)
            DumpMachine(sub.stateMachine, path + "." + sub.stateMachine.name, sb);
    }
#endif

    // ====================================================================== UPDATE
    private void Update()
    {
        if (isDead) return;
        if (cam == null) cam = Camera.main;

        UpdateSprintAndStamina();

        if (!playerControl)
        {
            SetReticleVisible(false);
            if (trajectoryLine != null) trajectoryLine.enabled = false;
            aimBlend = 0f;
            return;
        }

        HandleWeaponSwitching();
        HandleAiming();
        HandleCombat();
        HandleMovement();
        UpdateTimers();
        UpdateAnimation();
        UpdateHeldCalibration();
        MonitorHandMotion();
        UpdateCameraZoom();
        UpdateTrajectory();
        UpdateReticle();
        UpdateMessage();

        if (uiDirty) RefreshWeaponUI();
    }

    // ====================================================================== SHOULDER CAMERA
    private void LateUpdate()
    {
        camShiftValid = false;
        if (cam == null || isDead || !shoulderCamera || aimBlend <= 0.0005f) return;

        Vector3 shift = cam.transform.TransformDirection(shoulderOffset * aimBlend);
        cam.transform.position += shift;

        camLastFinalPos = cam.transform.position;
        camShiftValid = true;
    }

   
    Vector3 AimOrigin()
    {
        Vector3 p = cam.transform.position;
        if (!shoulderCamera || aimBlend <= 0.0005f) return p;

        bool alreadyShifted = camShiftValid && (p - camLastFinalPos).sqrMagnitude < 1e-8f;
        if (!alreadyShifted)
            p += cam.transform.TransformDirection(shoulderOffset * aimBlend);
        return p;
    }

    // ====================================================================== STAMINA
    void UpdateSprintAndStamina()
    {
        bool isMoving = Input.GetButton("Horizontal") || Input.GetButton("Vertical");
        bool sprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool isSprinting = isMoving && sprintHeld && presentEnergy > 0f && !isAiming && Time.time >= moveLockUntil;

        if (isAiming) movementSpeed = aimWalkSpeed;
        else movementSpeed = isSprinting ? sprintSpeed : walkSpeed;

        if (isSprinting)
        {
            playerEnergyDecrease(staminaDrainPerSecond * Time.deltaTime);
        }
        else if (presentEnergy < playerEnergy)
        {
            presentEnergy = Mathf.Min(playerEnergy, presentEnergy + staminaRegenPerSecond * Time.deltaTime);
            if (energybar != null) energybar.SetEnergy(presentEnergy);
        }
    }

    // ====================================================================== MOVEMENT
    Quaternion CameraFlatRotation()
    {
        if (MCC != null) return MCC.flatRotation;
        if (cam != null) return Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
        return Quaternion.identity;
    }

    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        moveAmount = Mathf.Clamp01(Mathf.Abs(h) + Mathf.Abs(v));

        Vector3 input = new Vector3(h, 0f, v).normalized;
        Vector3 desiredDir = CameraFlatRotation() * input;

        if (Time.time < moveLockUntil)
        {
            desiredDir = Vector3.zero;
            moveAmount = 0f;
        }

        Vector3 velocity;
        if (onSurface)
        {
            fallingSpeed = -0.5f;

            if (environmentChecker != null && desiredDir != Vector3.zero)
            {
                playerOnLedge = environmentChecker.CheckLedge(desiredDir, out LedgeInfo ledgeInfo);
                if (playerOnLedge)
                {
                    LedgeInfo = ledgeInfo;
                    if (Vector3.Angle(ledgeInfo.surfaceHit.normal, desiredDir) < 90f)
                    {
                        desiredDir = Vector3.zero;
                        moveAmount = 0f;
                    }
                }
            }

            velocity = desiredDir * movementSpeed;
        }
        else
        {
            fallingSpeed += Physics.gravity.y * Time.deltaTime;
            velocity = transform.forward * movementSpeed * 0.5f * (moveAmount > 0f ? 1f : 0f);
        }

        velocity.y = fallingSpeed;
        if (CC != null && CC.enabled) CC.Move(velocity * Time.deltaTime);

        if (isAiming && cam != null)
            requiredRotation = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
        else if (moveAmount > 0.01f && desiredDir != Vector3.zero)
            requiredRotation = Quaternion.LookRotation(desiredDir);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, requiredRotation, rotSpeed * Time.deltaTime);

        if (animator != null)
        {
            if (hasMovementValue)
                animator.SetFloat(MovementValueHash, moveAmount, 0.2f, Time.deltaTime);

            for (int i = 0; i < moveAliasParams.Count; i++)
                animator.SetFloat(moveAliasParams[i], moveAmount, 0.2f, Time.deltaTime);

            FeedStanceFloats();
        }

        SurfaceCheck();
        if (animator != null && hasOnSurface) animator.SetBool(OnSurfaceHash, onSurface);
    }

    void SurfaceCheck()
    {
        onSurface = Physics.CheckSphere(transform.TransformPoint(surfaceCheckOffset), surfaceCheckRadious, surfaceLayer);
    }

    void SnapToCameraYaw()
    {
        if (cam == null) return;
        transform.rotation = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
        requiredRotation = transform.rotation;
    }

    // ====================================================================== ANIMATION
    void CacheAnimatorParameters()
    {
        animParams.Clear();
        paramTypes.Clear();
        moveAliasParams.Clear();
        hasMovementValue = false;
        hasOnSurface = false;
        if (animator == null || animator.runtimeAnimatorController == null) return;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            AnimParam ap = new AnimParam { hash = p.nameHash, lower = p.name.ToLowerInvariant(), type = p.type };
            animParams.Add(ap);
            paramTypes[p.nameHash] = p.type;

            if (p.name == "movementValue" && p.type == AnimatorControllerParameterType.Float) hasMovementValue = true;
            if (p.name == "onSurface" && p.type == AnimatorControllerParameterType.Bool) hasOnSurface = true;

            if (p.type == AnimatorControllerParameterType.Float && p.name != "movementValue"
                && System.Array.IndexOf(MoveAliases, ap.lower) >= 0)
                moveAliasParams.Add(p.nameHash);
        }
    }

    
    void PrepareRules(StanceRules r)
    {
        if (r == null) return;
        r.boolHashes.Clear();
        r.boolValues.Clear();
        r.forwardHashes.Clear();
        r.sideHashes.Clear();

        for (int i = 0; i < r.boolNames.Count && i < r.boolHold.Count; i++)
        {
            string lower = r.boolNames[i].ToLowerInvariant();
            
            if (System.Array.IndexOf(AimFlags, lower) >= 0 || System.Array.IndexOf(ShootFlags, lower) >= 0
                || System.Array.IndexOf(ReloadFlags, lower) >= 0) continue;

            int h = Animator.StringToHash(r.boolNames[i]);
            AnimatorControllerParameterType t;
            if (paramTypes.TryGetValue(h, out t) && t == AnimatorControllerParameterType.Bool)
            {
                r.boolHashes.Add(h);
                r.boolValues.Add(r.boolHold[i]);
            }
        }

        for (int i = 0; i < r.floatForward.Count; i++)
        {
            int h = Animator.StringToHash(r.floatForward[i]);
            AnimatorControllerParameterType t;
            if (paramTypes.TryGetValue(h, out t) && t == AnimatorControllerParameterType.Float) r.forwardHashes.Add(h);
        }
        for (int i = 0; i < r.floatSide.Count; i++)
        {
            int h = Animator.StringToHash(r.floatSide[i]);
            AnimatorControllerParameterType t;
            if (paramTypes.TryGetValue(h, out t) && t == AnimatorControllerParameterType.Float) r.sideHashes.Add(h);
        }
    }

    StanceRules CurrentRules()
    {
        if (weapons == null || weapons.Length == 0) return null;
        switch (CurrentWeapon.type)
        {
            case WeaponType.Rifle: return rifleRules;
            case WeaponType.Bazooka: return bazookaRules;
        }
        return null;
    }

    
    void FeedStanceFloats()
    {
        StanceRules r = CurrentRules();
        if (r == null) return;

        for (int i = 0; i < r.forwardHashes.Count; i++)
            animator.SetFloat(r.forwardHashes[i], moveAmount, 0.2f, Time.deltaTime);
        for (int i = 0; i < r.sideHashes.Count; i++)
            animator.SetFloat(r.sideHashes[i], 0f, 0.2f, Time.deltaTime);
    }

    void ApplyBoolRules(StanceRules r, bool active)
    {
        if (r == null) return;
        for (int i = 0; i < r.boolHashes.Count; i++)
            animator.SetBool(r.boolHashes[i], active ? r.boolValues[i] : !r.boolValues[i]);
    }

    string[] WithDiscovered(int index, string[] defaults)
    {
        if (discoveredStates != null && index < discoveredStates.Length && !string.IsNullOrEmpty(discoveredStates[index]))
        {
            List<string> list = new List<string> { discoveredStates[index] };
            list.AddRange(defaults);
            return list.ToArray();
        }
        return defaults;
    }

    int ResolveState(string label, string[] candidates)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 0;

        string layerName = animator.GetLayerName(animationLayer);
        foreach (string c in candidates)
        {
            int full = Animator.StringToHash(layerName + "." + c);
            int bare = Animator.StringToHash(c);
            int found = 0;

            if (animator.HasState(animationLayer, full)) found = full;
            else if (animator.HasState(animationLayer, bare)) found = bare;

            if (found != 0)
            {
                int dot = c.LastIndexOf('.');
                string shortName = dot >= 0 ? c.Substring(dot + 1) : c;
                shortHashOf[found] = Animator.StringToHash(shortName);
                return found;
            }
        }

        if (logAnimatorWarnings && !string.IsNullOrEmpty(label))
            Debug.LogWarning("[ArtistLevelPlayer] Animator state not found for '" + label + "'. Tried: "
                             + string.Join(", ", candidates), this);
        return 0;
    }

    void BuildAnimSets()
    {
        animSets = new AnimSet[System.Enum.GetValues(typeof(WeaponType)).Length];
        for (int i = 0; i < animSets.Length; i++) animSets[i] = new AnimSet();

        locoHash = ResolveState("Basic Locomotion", AnimNames.Locomotion);

        AnimSet bat = animSets[(int)WeaponType.Fist];
        bat.idle = locoHash;
        for (int i = 1; i <= AnimNames.MeleeAttackCount; i++)
        {
            int h = ResolveState("SingleAttack" + i, AnimNames.MeleeAttack(i));
            if (h != 0) bat.attacks.Add(h);
        }

        AnimSet rifle = animSets[(int)WeaponType.Rifle];
        rifle.idle = ResolveState("Rifle stance", WithDiscovered(0, AnimNames.RifleIdle));
        rifle.aim = ResolveState("Rifle aim", WithDiscovered(1, AnimNames.RifleAim));
        int rifleShoot = ResolveState("Rifle shoot", WithDiscovered(2, AnimNames.RifleShoot));
        if (rifleShoot != 0) rifle.attacks.Add(rifleShoot);
        rifle.reload = ResolveState("Rifle reload", WithDiscovered(3, AnimNames.RifleReload));

        AnimSet baz = animSets[(int)WeaponType.Bazooka];
        baz.idle = ResolveState(null, WithDiscovered(4, AnimNames.BazookaIdle));
        baz.aim = ResolveState(null, WithDiscovered(5, AnimNames.BazookaAim));
        int bazShoot = ResolveState(null, WithDiscovered(6, AnimNames.BazookaShoot));
        if (bazShoot != 0) baz.attacks.Add(bazShoot);
        baz.reload = ResolveState(null, WithDiscovered(7, AnimNames.BazookaReload));

        if (baz.idle == 0 || baz.aim == 0 || baz.attacks.Count == 0 || baz.reload == 0)
        {
            if (logAnimatorWarnings)
                Debug.LogWarning("[ArtistLevelPlayer] Some Bazooka states weren't found - using Rifle animations for those. "
                                 + "Right-click this component > 'Re-scan Animator Controller' to fix.", this);
            if (baz.idle == 0) baz.idle = rifle.idle;
            if (baz.aim == 0) baz.aim = rifle.aim;
            if (baz.attacks.Count == 0) baz.attacks.AddRange(rifle.attacks);
            if (baz.reload == 0) baz.reload = rifle.reload;
        }

        AnimSet stone = animSets[(int)WeaponType.Stone];
        stone.idle = locoHash;
        int th = ResolveState("Throw", AnimNames.Throw);
        if (th != 0) stone.attacks.Add(th);

        foreach (AnimSet s in animSets)
            if (s.idle == 0) s.idle = locoHash;
    }

    AnimSet CurrentSet { get { return animSets[(int)CurrentWeapon.type]; } }

    void PlayHash(int hash, float fade)
    {
        if (animator == null || hash == 0) return;
        animator.CrossFadeInFixedTime(hash, fade, animationLayer);
        currentAnimHash = hash;
        hasCurrentAnim = true;
        lastAnimPlayTime = Time.time;
    }

    void PlayAttackAnimation(WeaponSlot w)
    {
        AnimSet s = CurrentSet;
        float duration = w.moveLockTime > 0f ? w.moveLockTime : Mathf.Min(w.cooldown, 0.4f);
        attackAnimUntil = Time.time + duration;

        if (s.attacks.Count == 0) return;

        if (Time.time - lastAttackTime > 1.2f) s.comboIndex = 0;
        int hash = s.attacks[s.comboIndex % s.attacks.Count];
        s.comboIndex = (s.comboIndex + 1) % s.attacks.Count;
        lastAttackTime = Time.time;

        PlayHash(hash, 0.05f);
    }

    void UpdateAnimation()
    {
        if (animator == null || animSets == null) return;

        WeaponSlot w = CurrentWeapon;
        AnimSet s = animSets[(int)w.type];
        bool attacking = Time.time < attackAnimUntil;

        if (!attacking)
        {
            int target = s.idle;
            if (isReloading && s.reload != 0) target = s.reload;
            else if (isAiming && s.aim != 0) target = s.aim;

            if (target != 0)
            {
                if (!hasCurrentAnim || target != currentAnimHash)
                {
                    PlayHash(target, isReloading ? 0.1f : 0.15f);
                    driftSince = -1f;
                }
                else
                {
                    CorrectDrift(s, target);
                }
            }
        }
        else
        {
            driftSince = -1f;
        }

        SyncAnimatorFlags(w, attacking);
    }

    bool StateMatches(AnimatorStateInfo info, int hash)
    {
        if (hash == 0) return false;
        if (info.fullPathHash == hash || info.shortNameHash == hash) return true;

        int shortHash;
        return shortHashOf.TryGetValue(hash, out shortHash) && info.shortNameHash == shortHash;
    }

    bool InWeaponFamily(AnimatorStateInfo info, AnimSet s)
    {
        if (StateMatches(info, s.idle) || StateMatches(info, s.aim) || StateMatches(info, s.reload)) return true;
        for (int i = 0; i < s.attacks.Count; i++)
            if (StateMatches(info, s.attacks[i])) return true;
        return false;
    }

    
    void CorrectDrift(AnimSet s, int target)
    {
        if (Time.time < lastAnimPlayTime + 0.3f || animator.IsInTransition(animationLayer))
        {
            driftSince = -1f;
            return;
        }

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(animationLayer);
        if (StateMatches(info, target))
        {
            driftSince = -1f;
            return;
        }

        float grace = InWeaponFamily(info, s) ? 0.6f : 0.15f;
        if (driftSince < 0f) driftSince = Time.time;

        if (Time.time - driftSince >= grace)
        {
            driftSince = -1f;
            PlayHash(target, 0.15f);
        }
    }

    void SetFlags(string[] names, bool value)
    {
        for (int i = 0; i < animParams.Count; i++)
        {
            AnimParam p = animParams[i];
            if (p.type == AnimatorControllerParameterType.Bool && System.Array.IndexOf(names, p.lower) >= 0)
                animator.SetBool(p.hash, value);
        }
    }

    void SyncAnimatorFlags(WeaponSlot w, bool attacking)
    {
        if (animParams.Count == 0) return;

        bool gun = w.type == WeaponType.Rifle || w.type == WeaponType.Bazooka;

        SetFlags(RifleFlags, w.type == WeaponType.Rifle);
        SetFlags(BazookaFlags, w.type == WeaponType.Bazooka);
        SetFlags(MeleeFlags, w.type == WeaponType.Fist && attacking);
        SetFlags(AimFlags, isAiming);
        SetFlags(ShootFlags, gun && attacking);
        SetFlags(ReloadFlags, isReloading);

        
        ApplyBoolRules(rifleRules, w.type == WeaponType.Rifle);
        ApplyBoolRules(bazookaRules, w.type == WeaponType.Bazooka);
    }

    // ====================================================================== WEAPON SWITCHING
    void HandleWeaponSwitching()
    {
        if (weapons.Length < 2 || Time.time < actionLockUntil) return;

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0.01f) SelectWeapon((currentIndex - 1 + weapons.Length) % weapons.Length);
        else if (scroll < -0.01f) SelectWeapon((currentIndex + 1) % weapons.Length);

        for (int i = 0; i < weapons.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                SelectWeapon(i);
        }
    }

    void SelectWeapon(int index, bool force = false)
    {
        if (index < 0 || index >= weapons.Length) return;
        if (index == currentIndex && !force) return;

        if (isReloading)
        {
            isReloading = false;
            moveLockUntil = Time.time;
        }

        isAiming = false;
        attackAnimUntil = 0f;
        hasCurrentAnim = false;
        driftSince = -1f;
        equipTime = Time.time;
        currentIndex = index;

        RefreshHeld();
        UpdateAnimation();
        uiDirty = true;
    }

    // ====================================================================== AIMING
    void HandleAiming()
    {
        WeaponSlot w = CurrentWeapon;
        bool canAim = w.type != WeaponType.Fist && !isReloading;
        isAiming = canAim && Input.GetMouseButton(1);

        if (isAiming && !wasAiming) aimStartTime = Time.time;
        wasAiming = isAiming;

        // blend the camera towards the shoulder
        float target = (shoulderCamera && isAiming) ? 1f : 0f;
        aimBlend = Mathf.Lerp(aimBlend, target, 1f - Mathf.Exp(-shoulderLerpSpeed * Time.deltaTime));
        if (target == 0f && aimBlend < 0.001f) aimBlend = 0f;
    }

    void UpdateCameraZoom()
    {
        if (cam == null) return;
        WeaponSlot w = CurrentWeapon;
        float target = (isAiming && w.aimFov > 0f) ? w.aimFov : defaultFov;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, target, 1f - Mathf.Exp(-aimLerpSpeed * Time.deltaTime));
    }

    
    bool AimRaycast(float range, out RaycastHit best)
    {
        best = default(RaycastHit);
        if (cam == null) return false;

        Vector3 origin = AimOrigin();
        Ray ray = new Ray(origin, cam.transform.forward);
        float extra = Vector3.Distance(origin, transform.position);
        int n = Physics.RaycastNonAlloc(ray, rayBuffer, range + extra, hitMask, QueryTriggerInteraction.Ignore);

        float bestDist = float.MaxValue;
        bool found = false;
        for (int i = 0; i < n; i++)
        {
            RaycastHit h = rayBuffer[i];
            if (h.transform.IsChildOf(transform)) continue;
            if (h.distance < bestDist)
            {
                bestDist = h.distance;
                best = h;
                found = true;
            }
        }
        return found;
    }

    // ====================================================================== COMBAT
    void HandleCombat()
    {
        if (isReloading) return;

        WeaponSlot w = CurrentWeapon;

        if (Input.GetKeyDown(KeyCode.R)) StartReload(w);

        bool wantsFire = w.automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        if (!wantsFire || Time.time < nextFireTime) return;

        switch (w.type)
        {
            case WeaponType.Fist: BatSwing(w); break;
            case WeaponType.Rifle: FireRifle(w); break;
            case WeaponType.Bazooka: FireBazooka(w); break;
            case WeaponType.Stone: ThrowStone(w); break;
        }
    }

    void BeginAction(WeaponSlot w)
    {
        nextFireTime = Time.time + w.cooldown;
        if (w.moveLockTime > 0f) moveLockUntil = Time.time + w.moveLockTime;
        if (w.type == WeaponType.Fist || w.type == WeaponType.Stone) actionLockUntil = nextFireTime;

        if (w.type != WeaponType.Fist) SnapToCameraYaw();

        PlayAttackAnimation(w);

        reticleKick = 1f;
    }

    void BatSwing(WeaponSlot w)
    {
        BeginAction(w);
        StartCoroutine(DelayedCall(w.hitDelay, () => MeleeHit(w)));
    }

    void MeleeHit(WeaponSlot w)
    {
        Vector3 center = transform.position + Vector3.up * 1f + transform.forward * w.range;
        Collider[] cols = Physics.OverlapSphere(center, w.radius, hitMask, QueryTriggerInteraction.Ignore);
        HashSet<Component> done = new HashSet<Component>();

        foreach (Collider c in cols)
        {
            if (c.transform.IsChildOf(transform)) continue;

            Component key;
            System.Action<float> apply;
            if (TryGetDamageTarget(c, out key, out apply) && done.Add(key))
                apply(w.damage);
        }

        NoiseSystem.Emit(transform.position, w.noiseRadius);
    }

    void FireRifle(WeaponSlot w)
    {
        if (!ConsumeAmmo(w)) return;
        BeginAction(w);

        RaycastHit hit;
        if (AimRaycast(w.range, out hit))
        {
            Component key;
            System.Action<float> apply;
            if (TryGetDamageTarget(hit.collider, out key, out apply))
                apply(w.damage);

            if (w.effectPrefab != null)
                Destroy(Instantiate(w.effectPrefab, hit.point, Quaternion.LookRotation(hit.normal)), 3f);
        }

        NoiseSystem.Emit(transform.position, w.noiseRadius);
    }

    void FireBazooka(WeaponSlot w)
    {
        if (!ConsumeAmmo(w)) return;
        BeginAction(w);

        RaycastHit hit;
        if (AimRaycast(w.range, out hit))
            Explode(hit.point, w);
    }

    void Explode(Vector3 point, WeaponSlot w)
    {
        if (w.effectPrefab != null)
            Destroy(Instantiate(w.effectPrefab, point, Quaternion.identity), 5f);

        Collider[] cols = Physics.OverlapSphere(point, w.radius, hitMask, QueryTriggerInteraction.Ignore);
        HashSet<Component> done = new HashSet<Component>();

        foreach (Collider c in cols)
        {
            if (c.transform.IsChildOf(transform)) continue;

            Component key;
            System.Action<float> apply;
            if (TryGetDamageTarget(c, out key, out apply) && done.Add(key))
                apply(w.damage);
        }

        NoiseSystem.Emit(point, w.noiseRadius);
    }

    void ThrowStone(WeaponSlot w)
    {
        if (!ConsumeAmmo(w)) return;
        BeginAction(w);
        StartCoroutine(DelayedCall(w.hitDelay, () => SpawnStone(w)));
    }

    Vector3 ThrowOrigin
    {
        get
        {
            Transform socket = GetHandSocket();
            if (socket != null && socket != transform) return socket.position;
            return transform.position + Vector3.up * 1.5f + transform.forward * 0.4f;
        }
    }

    Vector3 GetThrowVelocity()
    {
        Vector3 forward = cam != null ? cam.transform.forward : transform.forward;
        Vector3 dir = (forward + Vector3.up * throwArc).normalized;
        return dir * throwSpeed;
    }

    void SpawnStone(WeaponSlot w)
    {
        GameObject go;
        Vector3 origin = ThrowOrigin;
        bool generated = generateModelsFromCode || w.prefab == null;

        if (!generated)
        {
            go = Instantiate(w.prefab, origin, Quaternion.identity);

            foreach (Grenade g in go.GetComponentsInChildren<Grenade>(true))
                Destroy(g);
        }
        else
        {
            go = WeaponModelFactory.Build(WeaponType.Stone, "ThrownStone");
            go.transform.position = origin;
            go.transform.rotation = Random.rotation;
        }

        go.name = "ThrownStone";

        if (go.GetComponentInChildren<Collider>() == null)
        {
            SphereCollider sc = go.AddComponent<SphereCollider>();
            sc.radius = 0.08f;
        }

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.mass = stoneMass;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (CC != null)
        {
            foreach (Collider c in go.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(c, CC);
        }

        StoneProjectile sp = go.GetComponent<StoneProjectile>();
        if (sp == null) sp = go.AddComponent<StoneProjectile>();
        sp.noiseRadius = w.noiseRadius;
        sp.minImpactSpeed = stoneMinImpactSpeed;
        sp.impactClip = stoneImpactSound;

        rb.AddForce(GetThrowVelocity(), ForceMode.VelocityChange);
    }

    static bool TryGetDamageTarget(Collider c, out Component key, out System.Action<float> apply)
    {
        IDamageable d = c.GetComponentInParent<IDamageable>();
        if (d != null)
        {
            key = (d as Component) ?? c;
            apply = amount => d.TakeDamage(amount);
            return true;
        }

        KnightAI k = c.GetComponentInParent<KnightAI>();
        if (k != null)
        {
            key = k;
            apply = amount => k.TakeDamage(amount);
            return true;
        }

        key = null;
        apply = null;
        return false;
    }

    IEnumerator DelayedCall(float delay, System.Action action)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (action != null) action();
    }

    // ====================================================================== AMMO
    bool ConsumeAmmo(WeaponSlot w)
    {
        if (w.infiniteAmmo) return true;

        if (w.UsesClip)
        {
            if (w.ammoInClip <= 0)
            {
                if (w.reserveAmmo > 0) StartReload(w);
                else ShowMessage("Out of ammo", 1f);
                return false;
            }

            w.ammoInClip--;
            uiDirty = true;

            if (w.ammoInClip == 0 && w.reserveAmmo > 0)
                StartReload(w);

            return true;
        }

        if (w.reserveAmmo <= 0)
        {
            ShowMessage("No " + w.displayName.ToLower() + "s left", 1f);
            return false;
        }

        w.reserveAmmo--;
        uiDirty = true;
        return true;
    }

    void StartReload(WeaponSlot w)
    {
        if (isReloading || !w.UsesClip) return;
        if (w.ammoInClip >= w.clipSize || w.reserveAmmo <= 0) return;

        isReloading = true;
        reloadEndTime = Time.time + w.reloadTime;
        moveLockUntil = reloadEndTime;
        isAiming = false;
        attackAnimUntil = 0f;
        ShowMessage("Reloading...", w.reloadTime);
    }

    void UpdateTimers()
    {
        if (isReloading && Time.time >= reloadEndTime)
        {
            WeaponSlot w = CurrentWeapon;
            int take = Mathf.Min(w.clipSize - w.ammoInClip, w.reserveAmmo);
            w.ammoInClip += take;
            w.reserveAmmo -= take;
            isReloading = false;
            uiDirty = true;
        }

        RefreshHeld();
    }

    // ====================================================================== HAND TRANSFORM RESOLUTION
    Transform GetHandSocket()
    {
        if (useManualHandSocket && handSocket != null) return handSocket;
        if (autoSocket != null) return autoSocket;

        Transform bone = FindRealHandBone();
        if (bone == null)
        {
            if (!warnedNoHand)
            {
                warnedNoHand = true;
                Debug.LogWarning("[ArtistLevelPlayer] Couldn't find a right-hand bone. Turn on 'Use Manual Hand Socket' and assign it.", this);
            }
            return transform;
        }

        
        Transform socket = bone.Find(AutoSocketName);
        if (socket == null)
        {
            GameObject go = new GameObject(AutoSocketName);
            socket = go.transform;
            socket.SetParent(bone, false);
        }

        socket.localRotation = Quaternion.identity;
        socket.localScale = Vector3.one;
        socket.localPosition = PalmPositionInBone(bone);

        autoSocket = socket;
        return autoSocket;
    }

    Vector3 PalmPositionInBone(Transform bone)
    {
        if (animator != null && animator.isHuman)
        {
            Transform knuckle = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            if (knuckle != null && knuckle.IsChildOf(bone))
                return bone.InverseTransformPoint(Vector3.Lerp(bone.position, knuckle.position, 0.55f));
        }
        return Vector3.zero;
    }

    
    Transform FindRealHandBone()
    {
        List<Transform> candidates = new List<Transform>();

        if (animator != null)
        {
            if (animator.isHuman)
            {
                Transform humanHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (humanHand != null) candidates.Add(humanHand);
            }
            Transform byName = FindHandBoneRecursive(animator.transform);
            if (byName != null) candidates.Add(byName);
        }
        Transform inPlayer = FindHandBoneRecursive(transform);
        if (inPlayer != null) candidates.Add(inPlayer);

        SkinnedMeshRenderer[] skins = GetComponentsInChildren<SkinnedMeshRenderer>(true);

        
        foreach (Transform c in candidates)
            foreach (SkinnedMeshRenderer smr in skins)
                if (System.Array.IndexOf(smr.bones, c) >= 0) return c;

       
        foreach (SkinnedMeshRenderer smr in skins)
            foreach (Transform b in smr.bones)
                if (b != null && IsRightHandName(b.name)) return b;

        return candidates.Count > 0 ? candidates[0] : null;
    }

    static bool IsRightHandName(string name)
    {
        string n = name.ToLowerInvariant();

        bool isHand = n.Contains("righthand") || n.Contains("hand_r") || n.Contains("hand.r") || n.Contains("handr") ||
                      n.Contains("r_hand") || n.Contains("right_hand") || n.Contains("r hand") || n == "hand";

        bool isOther = n.Contains("finger") || n.Contains("thumb") || n.Contains("index") || n.Contains("middle") ||
                       n.Contains("ring") || n.Contains("pinky") || n.Contains("holder") || n.Contains("weapon") ||
                       n.Contains("[preview]") || n.Contains("_held") || n.Contains("socket") || n.Contains("shooting");

        return isHand && !isOther;
    }

    Transform FindHandBoneRecursive(Transform current)
    {
        if (IsRightHandName(current.name)) return current;

        foreach (Transform child in current)
        {
            Transform result = FindHandBoneRecursive(child);
            if (result != null) return result;
        }
        return null;
    }

    
    void MonitorHandMotion()
    {
        if (handMonitorDone || autoSocket == null) return;

        Vector3 local = transform.InverseTransformPoint(autoSocket.position);
        if (!handFirstSet)
        {
            handFirstSet = true;
            handFirstLocal = local;
            return;
        }

        if ((local - handFirstLocal).sqrMagnitude > 0.0025f)   
        {
            handMoved = true;
            handMonitorDone = true;
            return;
        }

        if (moveAmount > 0.1f) movingTime += Time.deltaTime;
        if (movingTime > 3f && !handMoved)
        {
            handMonitorDone = true;
            Debug.LogWarning("[ArtistLevelPlayer] The hand bone '" + autoSocket.parent.name + "' hasn't moved while walking. "
                             + "The Animator is probably not driving this skeleton: check that the Animator sits on the visible character "
                             + "and that its Avatar belongs to this model.", autoSocket);
        }
    }

    // ====================================================================== HELD MODELS (generated from code)
    GameObject CreateModel(WeaponSlot w)
    {
        if (!generateModelsFromCode && w.prefab != null)
            return Instantiate(w.prefab);

        return WeaponModelFactory.Build(w.type, w.displayName + "_Model");
    }

    
    Vector3 PalmLocalOffset(Transform socket)
    {
        if (animator != null && animator.isHuman)
        {
            Transform knuckle = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            if (knuckle != null && knuckle.IsChildOf(socket))
                return socket.InverseTransformPoint(Vector3.Lerp(socket.position, knuckle.position, 0.55f));
        }
        return Vector3.zero;
    }


    Quaternion GripRotation(WeaponSlot w, Transform socket)
    {
        Vector3 fwd = transform.forward;
        Vector3 up = transform.up;
        Vector3 dir = fwd;

        if (w.type == WeaponType.Fist)   
            dir = (fwd * 0.35f + up * 0.9f).normalized;

        Quaternion world = Quaternion.LookRotation(dir, up) * Quaternion.Euler(w.holdEuler);
        return Quaternion.Inverse(socket.rotation) * world;
    }

    GameObject CreateHeld(WeaponSlot w, Transform socket)
    {
        GameObject root = new GameObject(w.displayName + "_Held");
        Transform rt = root.transform;
        rt.SetParent(socket, false);
        rt.localPosition = PalmLocalOffset(socket);
        rt.localRotation = GripRotation(w, socket);

        
        Vector3 ls = socket.lossyScale;
        rt.localScale = new Vector3(SafeInv(ls.x), SafeInv(ls.y), SafeInv(ls.z));

        GameObject model = CreateModel(w);
        Transform mt = model.transform;
        mt.SetParent(rt, false);
        mt.localPosition = w.holdPosition;
        mt.localRotation = Quaternion.identity;
        mt.localScale = Vector3.Scale(mt.localScale, w.holdScale);

        MakeInert(root);
        return root;
    }

    static float SafeInv(float v)
    {
        return Mathf.Abs(v) < 1e-5f ? 1f : 1f / v;
    }

    void BuildHeldModels()
    {
        Transform socket = GetHandSocket();

        foreach (WeaponSlot w in weapons)
        {
            GameObject go = CreateHeld(w, socket);
            go.SetActive(false);
            w.held = go;
        }
    }


    void UpdateHeldCalibration()
    {
        WeaponSlot w = CurrentWeapon;
        if (w.held == null || w.calibrated || animator == null || animSets == null) return;
        if (Time.time < equipTime + 0.35f || animator.IsInTransition(animationLayer)) return;
        if (Time.time < attackAnimUntil || isReloading) return;

        bool gun = w.type == WeaponType.Rifle || w.type == WeaponType.Bazooka;
        bool hasAimPose = animSets[(int)w.type].aim != 0;
        Transform socket = w.held.transform.parent;

        if (gun && hasAimPose)
        {
            if (isAiming)
            {
                if (Time.time < aimStartTime + 0.4f) return;
                w.held.transform.localRotation = GripRotation(w, socket);
                w.calibrated = true;
            }
            else if (!w.calibratedIdle)
            {
                
                w.held.transform.localRotation = GripRotation(w, socket);
                w.calibratedIdle = true;
            }
        }
        else if (!isAiming)
        {
            w.held.transform.localRotation = GripRotation(w, socket);
            w.calibratedIdle = true;
            w.calibrated = true;
        }
    }

    static void MakeInert(GameObject go)
    {
        foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        foreach (MonoBehaviour mb in go.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;
    }

    void RefreshHeld()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            WeaponSlot w = weapons[i];
            if (w.held == null) continue;

            bool show = i == currentIndex;

            if (show && w.type == WeaponType.Stone && (w.reserveAmmo <= 0 || Time.time < nextFireTime))
                show = false;

            if (w.held.activeSelf != show) w.held.SetActive(show);
        }
    }

    // ====================================================================== TRAJECTORY PREVIEW
    void BuildTrajectoryLine()
    {
        GameObject go = new GameObject("StoneTrajectory");
        trajectoryLine = go.AddComponent<LineRenderer>();
        trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
        trajectoryLine.startWidth = 0.05f;
        trajectoryLine.endWidth = 0.02f;
        trajectoryLine.startColor = new Color(1f, 1f, 1f, 0.9f);
        trajectoryLine.endColor = new Color(1f, 1f, 1f, 0.1f);
        trajectoryLine.numCapVertices = 2;
        trajectoryLine.enabled = false;
    }

    void UpdateTrajectory()
    {
        if (trajectoryLine == null) return;

        WeaponSlot w = CurrentWeapon;
        bool show = isAiming && w.type == WeaponType.Stone && w.reserveAmmo > 0;
        trajectoryLine.enabled = show;
        if (!show) return;

        Vector3 pos = ThrowOrigin;
        Vector3 vel = GetThrowVelocity();
        int steps = Mathf.Max(2, trajectoryPoints);

        trajectoryLine.positionCount = steps;
        trajectoryLine.SetPosition(0, pos);
        int count = 1;

        for (int i = 1; i < steps; i++)
        {
            vel += Physics.gravity * trajectoryTimeStep;
            Vector3 next = pos + vel * trajectoryTimeStep;

            RaycastHit hit;
            if (Physics.Linecast(pos, next, out hit, hitMask, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(transform))
            {
                trajectoryLine.SetPosition(i, hit.point);
                count = i + 1;
                break;
            }

            trajectoryLine.SetPosition(i, next);
            count = i + 1;
            pos = next;
        }

        trajectoryLine.positionCount = count;
    }

    // ====================================================================== UI CREATION
    Font UIFont
    {
        get
        {
            if (uiFont == null)
                uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Helvetica", "Liberation Sans" }, 16);
            return uiFont;
        }
    }

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
        t.font = UIFont;
        t.text = content;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    void BuildUI()
    {
        GameObject canvasGO = new GameObject("ArtistLevel_UI");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        BuildWeaponBar();
        BuildReticle();

        Vector2 bottomCenter = new Vector2(0.5f, 0f);
        messageText = NewText("Message", canvas.transform, bottomCenter, new Vector2(0f, 190f),
                              new Vector2(600f, 40f), "", 26, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.3f));
        messageText.gameObject.SetActive(false);
    }

    void BuildWeaponBar()
    {
        const float slotW = 120f, slotH = 110f, spacing = 14f;
        Vector2 bottomCenter = new Vector2(0.5f, 0f);

        RectTransform bar = NewRect("WeaponBar", canvas.transform, bottomCenter,
                                    new Vector2(0f, 20f + slotH * 0.5f), new Vector2(800f, slotH));

        float total = weapons.Length * slotW + (weapons.Length - 1) * spacing;

        for (int i = 0; i < weapons.Length; i++)
        {
            WeaponSlot w = weapons[i];
            float x = -total * 0.5f + slotW * 0.5f + i * (slotW + spacing);
            Vector2 mid = new Vector2(0.5f, 0.5f);

            SlotUI s = new SlotUI();
            s.root = NewRect("Slot_" + w.displayName, bar, mid, new Vector2(x, 0f), new Vector2(slotW, slotH));
            s.border = NewImage("Border", s.root, mid, Vector2.zero, new Vector2(slotW + 8f, slotH + 8f), new Color(1f, 0.85f, 0.2f, 1f));
            s.bg = NewImage("Background", s.root, mid, Vector2.zero, new Vector2(slotW, slotH), new Color(0f, 0f, 0f, 0.55f));
            s.icon = NewImage("Icon", s.root, mid, new Vector2(0f, 22f), new Vector2(52f, 52f), Color.white);
            s.icon.preserveAspect = true;
            s.icon.sprite = w.icon;
            s.icon.enabled = w.icon != null;

            NewText("Number", s.root, mid, new Vector2(-46f, 42f), new Vector2(24f, 24f),
                    (i + 1).ToString(), 18, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.8f));
            s.nameText = NewText("Name", s.root, mid, new Vector2(0f, -18f), new Vector2(slotW - 8f, 24f),
                                 w.displayName, 17, TextAnchor.MiddleCenter, Color.white);
            s.ammoText = NewText("Ammo", s.root, mid, new Vector2(0f, -40f), new Vector2(slotW - 8f, 22f),
                                 "", 15, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.4f));

            slotUIs.Add(s);
        }
    }

    void BuildReticle()
    {
        Vector2 center = new Vector2(0.5f, 0.5f);
        reticleRoot = NewRect("Reticle", canvas.transform, center, Vector2.zero, new Vector2(10f, 10f));

        reticleParts.Add(NewImage("Up", reticleRoot, center, Vector2.zero, new Vector2(3f, 14f), Color.white));
        reticleParts.Add(NewImage("Down", reticleRoot, center, Vector2.zero, new Vector2(3f, 14f), Color.white));
        reticleParts.Add(NewImage("Left", reticleRoot, center, Vector2.zero, new Vector2(14f, 3f), Color.white));
        reticleParts.Add(NewImage("Right", reticleRoot, center, Vector2.zero, new Vector2(14f, 3f), Color.white));
        reticleParts.Add(NewImage("Dot", reticleRoot, center, Vector2.zero, new Vector2(4f, 4f), Color.white));

        reticleRoot.gameObject.SetActive(false);
    }

    // ====================================================================== UI UPDATE
    void RefreshWeaponUI()
    {
        uiDirty = false;

        for (int i = 0; i < slotUIs.Count && i < weapons.Length; i++)
        {
            SlotUI s = slotUIs[i];
            WeaponSlot w = weapons[i];
            bool selected = i == currentIndex;

            s.border.enabled = selected;
            s.bg.color = selected ? new Color(0.12f, 0.12f, 0.12f, 0.85f) : new Color(0f, 0f, 0f, 0.55f);
            s.root.localScale = Vector3.one * (selected ? 1.12f : 1f);

            Color tint = selected ? Color.white : new Color(1f, 1f, 1f, 0.6f);
            s.nameText.color = tint;
            s.icon.color = tint;

            s.ammoText.text = AmmoString(w);
        }
    }

    static string AmmoString(WeaponSlot w)
    {
        if (w.infiniteAmmo) return "";
        if (w.UsesClip) return w.ammoInClip + " / " + w.reserveAmmo;
        return "x" + w.reserveAmmo;
    }

    void SetReticleVisible(bool visible)
    {
        if (reticleRoot != null && reticleRoot.gameObject.activeSelf != visible)
            reticleRoot.gameObject.SetActive(visible);
    }

    void UpdateReticle()
    {
        WeaponSlot w = CurrentWeapon;
        bool visible = w.type == WeaponType.Rifle || w.type == WeaponType.Bazooka;
        SetReticleVisible(visible);
        if (!visible) return;

        float target = 14f;
        if (moveAmount > 0.1f) target += 8f;
        target += reticleKick * 10f;
        if (isAiming) target *= 0.5f;

        reticleGap = Mathf.Lerp(reticleGap, target, Time.deltaTime * 12f);
        reticleKick = Mathf.MoveTowards(reticleKick, 0f, Time.deltaTime * 4f);

        float d = reticleGap + 7f;
        reticleParts[0].rectTransform.anchoredPosition = new Vector2(0f, d);
        reticleParts[1].rectTransform.anchoredPosition = new Vector2(0f, -d);
        reticleParts[2].rectTransform.anchoredPosition = new Vector2(-d, 0f);
        reticleParts[3].rectTransform.anchoredPosition = new Vector2(d, 0f);

        bool overEnemy = false;
        RaycastHit hit;
        if (AimRaycast(w.range, out hit))
        {
            Component key;
            System.Action<float> apply;
            overEnemy = TryGetDamageTarget(hit.collider, out key, out apply);
        }

        if (overEnemy != reticleIsRed)
        {
            reticleIsRed = overEnemy;
            Color c = overEnemy ? new Color(1f, 0.2f, 0.2f) : Color.white;
            foreach (Image img in reticleParts) img.color = c;
        }
    }

    void ShowMessage(string text, float duration)
    {
        if (messageText == null) return;
        messageText.text = text;
        messageText.gameObject.SetActive(true);
        messageUntil = Time.time + duration;
    }

    void UpdateMessage()
    {
        if (messageText != null && messageText.gameObject.activeSelf && Time.time >= messageUntil)
            messageText.gameObject.SetActive(false);
    }

    // ====================================================================== PLAYER API
    public void SetControl(bool hasControl)
    {
        playerControl = hasControl;
        CC.enabled = hasControl;

        if (!hasControl)
        {
            if (animator != null && hasMovementValue) animator.SetFloat(MovementValueHash, 0f);
            requiredRotation = transform.rotation;
        }
    }

    public bool HasPlayerControl
    {
        get { return playerControl; }
        set { playerControl = value; }
    }

    public void playerHitDamage(float takeDamage)
    {
        if (isDead) return;

        presentHealth -= takeDamage;
        if (healthbar != null) healthbar.SetHealth(presentHealth);
        if (DamageIndicator != null) StartCoroutine(showDamage());

        if (presentHealth <= 0f)
            PlayerDie();
    }

    private void PlayerDie()
    {
        isDead = true;
        Cursor.lockState = CursorLockMode.None;
        if (canvas != null) canvas.gameObject.SetActive(false);
        if (trajectoryLine != null) trajectoryLine.enabled = false;
        Destroy(gameObject, 1.0f);
    }

    public void playerEnergyDecrease(float energyDecrease)
    {
        presentEnergy = Mathf.Max(0f, presentEnergy - energyDecrease);
        if (energybar != null) energybar.SetEnergy(presentEnergy);
    }

    IEnumerator showDamage()
    {
        DamageIndicator.SetActive(true);
        yield return new WaitForSeconds(0.2f);
        if (DamageIndicator != null) DamageIndicator.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        if (!drawSocketGizmo) return;

        Transform s = useManualHandSocket && handSocket != null ? handSocket : autoSocket;
        if (s == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(s.position, 0.04f);
        Gizmos.color = Color.blue; Gizmos.DrawLine(s.position, s.position + s.forward * 0.15f);
        Gizmos.color = Color.red; Gizmos.DrawLine(s.position, s.position + s.right * 0.08f);
        Gizmos.color = Color.green; Gizmos.DrawLine(s.position, s.position + s.up * 0.08f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.TransformPoint(surfaceCheckOffset), surfaceCheckRadious);

        if (weapons != null && weapons.Length > 0)
        {
            WeaponSlot bat = weapons[0];
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up + transform.forward * bat.range, bat.radius);
        }
    }
}