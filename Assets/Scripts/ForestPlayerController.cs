using System.Collections.Generic;
using BetterEventBus;
using Forestlevel;
using UnityEngine;
using UnityEngine.InputSystem;
using GameDevExtensionMethods;

[RequireComponent(typeof(CharacterController))]
public class ForestPlayerController : MonoBehaviour, IMovementOwner,
    IGamePlayEventListener<IMovementStrategy>,
    IGamePlayEventListener<ExplorationGameStateEvent>,
    IGamePlayEventListener<TutorialGameStateEvent>,
    IGamePlayEventListener<InGameGameStateEvent>,
    IGamePlayEventListener<LevelWonEvent>,
    IGamePlayEventListener<PlayerLocationEvent>
{
    [Header("References")]
    [SerializeField] Transform cameraTransform; // read by strategies for camera-relative direction
    public Transform CameraTransform => cameraTransform;

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
        set => SetControl(value);
    }

    bool playerControl = true;    // taken away by parkour actions via SetControl
    bool catchingEnabled;         // only during the in-game state
    bool movementLocked;          // LevelWonEvent backstop - see OnGamePlayEvent(LevelWonEvent)
    bool onSurface;
    float fallingSpeed;
    Vector3 currentHorizontalVelocity; // last-applied horizontal velocity; fed into MovementContext
                                        // so TraversalMovement's camera-lock speed threshold has
                                        // something to read, same role "momentum" played on the
                                        // Rigidbody version.
    Quaternion requiredRotation;

    readonly Collider[] catchHits = new Collider[8];
    readonly HashSet<Apple> caughtThisFrame = new();

    static readonly int MovementValueHash = Animator.StringToHash("movementValue");
    static readonly int OnSurfaceHash = Animator.StringToHash("onSurface");

    DefaultInputSystem controls;
    Vector2 moveInput;
    bool sprintHeld;

    IMovementStrategy currentStrategy;
    public IMovementStrategy CurrentStrategy => currentStrategy;

    void Awake()
    {
        if (!CC) CC = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!environmentChecker) environmentChecker = GetComponent<EnvironmentChecker>();
        requiredRotation = transform.rotation;

        controls = new DefaultInputSystem();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void OnEnable()
    {
        controls.Player.Enable();
        controls.Player.Move.performed += OnMove;
        controls.Player.Move.canceled += OnMove;
        controls.Player.Sprint.performed += OnSprint;
        controls.Player.Sprint.canceled += OnSprint;

        GameEventBus.Register<IMovementStrategy>(this);
        GameEventBus.Register<ExplorationGameStateEvent>(this);
        GameEventBus.Register<TutorialGameStateEvent>(this);
        GameEventBus.Register<InGameGameStateEvent>(this);
        GameEventBus.Register<LevelWonEvent>(this);
        GameEventBus.Register<PlayerLocationEvent>(this);
    }

    void OnDisable()
    {
        controls.Player.Move.performed -= OnMove;
        controls.Player.Move.canceled -= OnMove;
        controls.Player.Sprint.performed -= OnSprint;
        controls.Player.Sprint.canceled -= OnSprint;
        controls.Player.Disable();

        GameEventBus.Unregister<IMovementStrategy>(this);
        GameEventBus.Unregister<ExplorationGameStateEvent>(this);
        GameEventBus.Unregister<TutorialGameStateEvent>(this);
        GameEventBus.Unregister<InGameGameStateEvent>(this);
        GameEventBus.Unregister<LevelWonEvent>(this);
        GameEventBus.Unregister<PlayerLocationEvent>(this);
    }

    void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    void OnSprint(InputAction.CallbackContext ctx) => sprintHeld = ctx.ReadValueAsButton();

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
        // ASSUMPTION: MovementContext's constructor signature, inferred from the one
        // working call site I've seen (PlayerMovement). If your actual struct differs,
        // only this one line needs adjusting - the strategies just read ctx.moveInput /
        // ctx.currentHorizontalVelocity, nothing else here depends on its shape.
        var ctx = new MovementContext(moveInput, sprintHeld, onSurface, Vector3.up, currentHorizontalVelocity, Time.deltaTime);

        Vector3 wishDir = movementLocked
            ? Vector3.zero
            : (currentStrategy != null ? currentStrategy.GetHorizontalTarget(ctx) : Vector3.zero);

        Vector3 velocity = Vector3.zero;

        if (onSurface)
        {
            fallingSpeed = -0.5f;
            velocity = wishDir * movementSpeed;

            LedgeInfo ledge = default;
            playerOnLedge = environmentChecker != null && wishDir.sqrMagnitude > 0.0001f
                && environmentChecker.CheckLedge(wishDir, out ledge);

            if (playerOnLedge)
            {
                LedgeInfo = ledge;
                // stop at the edge; ParkourControllerScript decides whether to jump down
                if (Vector3.Angle(ledge.surfaceHit.normal, wishDir) < 90f)
                    velocity = Vector3.zero;
            }

            currentHorizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        }
        else
        {
            fallingSpeed += Physics.gravity.y * Time.deltaTime;
            // keep whatever horizontal momentum you had when you left the ground -
            // no air control, matches the earlier CharacterController version.
            velocity = currentHorizontalVelocity;
        }

        velocity.y = fallingSpeed;
        CC.Move(velocity * Time.deltaTime);

        if (wishDir.sqrMagnitude > 0.0001f)
            requiredRotation = Quaternion.LookRotation(wishDir);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, requiredRotation, rotSpeed * Time.deltaTime);

        float movementAmount = Mathf.Clamp01(wishDir.magnitude);
        animator.SetFloat(MovementValueHash, movementAmount, 0.2f, Time.deltaTime);
        animator.SetBool(OnSurfaceHash, onSurface);
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

    // ---------------- events ----------------

    public void OnGamePlayEvent(IMovementStrategy e)
    {
        if (e == null) return;
        currentStrategy?.OnExit();
        currentStrategy = e;
        currentStrategy.OnEnter(this);
    }

    public void OnGamePlayEvent(ExplorationGameStateEvent e) => catchingEnabled = false;
    public void OnGamePlayEvent(TutorialGameStateEvent e) => catchingEnabled = false;
    public void OnGamePlayEvent(InGameGameStateEvent e) => catchingEnabled = true;

    // ForestGameStateManager's LevelWonEvent handler doesn't raise an IdleMovement
    // strategy - it just logs. Without this, whatever strategy was active (usually
    // InGameMovement) would keep driving the player around after winning.
    public void OnGamePlayEvent(LevelWonEvent e)
    {
        movementLocked = true;
        catchingEnabled = false;
    }

    // teleports (exploration spot / play area). CharacterController must be off while
    // setting position. Also clears currentHorizontalVelocity so TraversalMovement's
    // camera-lock threshold doesn't mis-fire off stale pre-teleport speed.
    public void OnGamePlayEvent(PlayerLocationEvent e)
    {
        bool ccWasEnabled = CC.enabled;
        CC.enabled = false;
        transform.position = e.Destination;
        CC.enabled = ccWasEnabled;
        fallingSpeed = 0f;
        currentHorizontalVelocity = Vector3.zero;
        movementLocked = false; // re-entering play (e.g. after a reset) should unlock movement again
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.TransformPoint(surfaceCheckOffset), surfaceCheckRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.TransformPoint(catchOffset), catchRadius);
    }
}