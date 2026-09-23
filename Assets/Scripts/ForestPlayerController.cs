using System.Collections.Generic;
using BetterEventBus;
using Forestlevel;
using UnityEngine;


[RequireComponent(typeof(CharacterController))]
public class ForestPlayerController : MonoBehaviour,
    IGamePlayEventListener<ExplorationGameStateEvent>,
    IGamePlayEventListener<TutorialGameStateEvent>,
    IGamePlayEventListener<InGameGameStateEvent>,
    IGamePlayEventListener<LevelWonEvent>,
    IGamePlayEventListener<PlayerLocationEvent>
{
    [Header("Movement")]
    public float movementSpeed = 5f;
    public float rotSpeed = 450f;
    public EnvironmentChecker environmentChecker;

    [Header("Animator")]
    public Animator animator;

    [Header("Collision & Gravity")]
    public CharacterController CC;
    public float surfaceCheckRadius = 0.1f;
    public Vector3 surfaceCheckOffset;
    public LayerMask surfaceLayer;

    [Header("Apple Catching")]
    [Tooltip("Turn off if Apple.cs already raises AppleCollectedEvent itself (avoids double counting).")]
    [SerializeField] bool collectApples = true;
    [Tooltip("Catch sphere position relative to the player (put it around the raised hands / above the head).")]
    [SerializeField] Vector3 catchOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] float catchRadius = 0.6f;
    [SerializeField] LayerMask appleLayer = ~0;

    // used by ParkourControllerScript
    public bool playerOnLedge { get; set; }
    public LedgeInfo LedgeInfo { get; set; }
    public bool HasPlayerControl
    {
        get => playerControl;
        set => playerControl = value;
    }

    bool playerControl = true;   // taken away by parkour actions via SetControl
    bool movementEnabled = true; // driven by the game-state events
    bool lateralOnly;            // in-game: left/right only
    bool catchingEnabled;        // only during the in-game state
    bool onSurface;
    float fallingSpeed;
    Vector3 velocity;
    Vector3 lastGroundedHorizontalVelocity;
    Quaternion requiredRotation;

    readonly Collider[] catchHits = new Collider[8];
    readonly HashSet<Apple> caughtThisFrame = new();

    static readonly int MovementValueHash = Animator.StringToHash("movementValue");
    static readonly int OnSurfaceHash = Animator.StringToHash("onSurface");

    void Awake()
    {
        if (!CC) CC = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!environmentChecker) environmentChecker = GetComponent<EnvironmentChecker>();
        requiredRotation = transform.rotation;
    }

    void OnEnable()
    {
        GameEventBus.Register<ExplorationGameStateEvent>(this);
        GameEventBus.Register<TutorialGameStateEvent>(this);
        GameEventBus.Register<InGameGameStateEvent>(this);
        GameEventBus.Register<LevelWonEvent>(this);
        GameEventBus.Register<PlayerLocationEvent>(this);
    }

    void OnDisable()
    {
        GameEventBus.Unregister<ExplorationGameStateEvent>(this);
        GameEventBus.Unregister<TutorialGameStateEvent>(this);
        GameEventBus.Unregister<InGameGameStateEvent>(this);
        GameEventBus.Unregister<LevelWonEvent>(this);
        GameEventBus.Unregister<PlayerLocationEvent>(this);
    }

    void Update()
    {
        // catching keeps working even while a parkour action has taken over movement
        if (collectApples && catchingEnabled) TryCatchApples();

        if (!playerControl) return;

        SurfaceCheck();
        HandleMovement();
    }

    // ---------------- movement ----------------

    void HandleMovement()
    {
        float h = movementEnabled ? Input.GetAxis("Horizontal") : 0f;
        float v = (movementEnabled && !lateralOnly) ? Input.GetAxis("Vertical") : 0f;
        float movementAmount = Mathf.Clamp01(Mathf.Abs(h) + Mathf.Abs(v));

        Vector3 input = new Vector3(h, 0f, v).normalized;
        Vector3 desiredDir = CameraFlatRotation() * input;

        velocity = Vector3.zero;

        if (onSurface)
        {
            fallingSpeed = -0.5f;
            velocity = desiredDir * movementSpeed;

            LedgeInfo ledge = default;
            playerOnLedge = environmentChecker != null && environmentChecker.CheckLedge(desiredDir, out ledge);

            if (playerOnLedge)
            {
                LedgeInfo = ledge;
                // stop at the edge; ParkourControllerScript decides whether to jump down
                if (Vector3.Angle(ledge.surfaceHit.normal, desiredDir) < 90f)
                    velocity = Vector3.zero;
            }

            lastGroundedHorizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        }
        else
        {
            fallingSpeed += Physics.gravity.y * Time.deltaTime;
            // keep whatever horizontal momentum you had when you left the ground —
            // don't force a constant forward push while airborne.
            velocity = lastGroundedHorizontalVelocity;
        }

        velocity.y = fallingSpeed;
        CC.Move(velocity * Time.deltaTime);

        // rotate toward the input direction (even when stopped at a ledge, so the parkour check sees the right angle)
        if (movementAmount > 0.01f)
            requiredRotation = Quaternion.LookRotation(desiredDir);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, requiredRotation, rotSpeed * Time.deltaTime);

        animator.SetFloat(MovementValueHash, movementAmount, 0.2f, Time.deltaTime);
        animator.SetBool(OnSurfaceHash, onSurface);
    }

    Quaternion CameraFlatRotation()
    {
        var cam = Camera.main;
        return cam ? Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f) : Quaternion.identity;
    }

    void SurfaceCheck()
    {
        onSurface = Physics.CheckSphere(transform.TransformPoint(surfaceCheckOffset), surfaceCheckRadius, surfaceLayer);
    }

    public void SetControl(bool hasControl)
    {
        playerControl = hasControl;
        CC.enabled = hasControl;

        if (!hasControl)
        {
            animator.SetFloat(MovementValueHash, 0f);
            requiredRotation = transform.rotation;
        }
    }

    // ---------------- apple catching ----------------

    void TryCatchApples()
    {
        Vector3 point = transform.TransformPoint(catchOffset);
        int count = Physics.OverlapSphereNonAlloc(point, catchRadius, catchHits, appleLayer, QueryTriggerInteraction.Collide);
        if (count == 0) return;

        caughtThisFrame.Clear();
        for (int i = 0; i < count; i++)
        {
            var apple = catchHits[i].GetComponentInParent<Apple>();
            if (apple == null || !caughtThisFrame.Add(apple)) continue;
            CatchApple(apple);
        }
    }

    void CatchApple(Apple apple)
    {
        GameEventBus.Raise<AppleCollectedEvent>(new AppleCollectedEvent(apple));
        AppleDeployManager.Instance.ReturnToPool(apple);
    }

    // ---------------- game state events ----------------

    public void OnGamePlayEvent(ExplorationGameStateEvent e)
    {
        movementEnabled = true;
        lateralOnly = false;
        catchingEnabled = false;
    }

    public void OnGamePlayEvent(TutorialGameStateEvent e)
    {
        movementEnabled = false;
        catchingEnabled = false;
    }

    public void OnGamePlayEvent(InGameGameStateEvent e)
    {
        movementEnabled = true;
        lateralOnly = true;
        catchingEnabled = true;
    }

    public void OnGamePlayEvent(LevelWonEvent e)
    {
        movementEnabled = false;
        catchingEnabled = false;
    }

    // teleports (exploration spot / play area). CharacterController must be off while setting position.
    public void OnGamePlayEvent(PlayerLocationEvent e)
    {
        bool ccWasEnabled = CC.enabled;
        CC.enabled = false;
        transform.position = e.Destination;
        CC.enabled = ccWasEnabled;
        fallingSpeed = 0f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.TransformPoint(surfaceCheckOffset), surfaceCheckRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.TransformPoint(catchOffset), catchRadius);
    }
}