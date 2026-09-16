using UnityEngine;
using UnityEngine.InputSystem;

namespace Forestlevel
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform cameraTransform; // read by strategies for camera-relative direction
        public Transform CameraTransform => cameraTransform;

        [Header("Movement")]
        public float moveSpeed = 6f;
        public float acceleration = 10f;
        public float airControl = 2f;
        public float groundFriction = 8f;
        public float airFriction = 0.5f;

        [Header("Rotation")]
        public float turnSpeed = 720f;

        [Header("Slope")]
        public float slopeLimit = 45f;
        public float slideGravity = 12f; // steep-slope slide-down rate, not the removed dash-slide ability

        [Header("Ground Check")]
        public float groundCheckDistance = 0.3f;
        public LayerMask groundMask;

        Rigidbody _rb;
        public Rigidbody Rb => _rb;
        CapsuleCollider _col;

        Vector3 momentum;
        bool isGrounded;
        Vector3 groundNormal = Vector3.up;

        DefaultInputSystem controls;
        Vector2 moveInput;
        bool sprintHeld;

        IMovementStrategy currentStrategy;
        public IMovementStrategy CurrentStrategy => currentStrategy;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<CapsuleCollider>();
            _rb.freezeRotation = true;
            _rb.useGravity = false;

            controls = new DefaultInputSystem();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

        }

        void Start()
        {
            SetStrategy(new TraversalMovement());
            
        }

        void OnEnable()
        {
            controls.Player.Enable();
            controls.Player.Move.performed += OnMove;
            controls.Player.Move.canceled += OnMove;
            controls.Player.Sprint.performed += OnSprint;
            controls.Player.Sprint.canceled += OnSprint;
        }

        void OnDisable()
        {
            controls.Player.Move.performed -= OnMove;
            controls.Player.Move.canceled -= OnMove;
            controls.Player.Sprint.performed -= OnSprint;
            controls.Player.Sprint.canceled -= OnSprint;
            controls.Player.Disable();
        }

        void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
        void OnSprint(InputAction.CallbackContext ctx) => sprintHeld = ctx.ReadValueAsButton();

        public void SetStrategy(IMovementStrategy newStrategy)
        {
            if (newStrategy == null) return;
            currentStrategy?.OnExit();
            currentStrategy = newStrategy;
            currentStrategy.OnEnter(this);
        }

        void FixedUpdate()
        {
            CheckGround();
            HandleMomentum();
            HandleFacing();
            _rb.linearVelocity = momentum;
        }

        void CheckGround()
        {
            float radius = _col.radius * 0.9f;
            Vector3 origin = _col.bounds.center;
            float castDistance = _col.bounds.extents.y - radius + groundCheckDistance;

            isGrounded = Physics.SphereCast(
                origin, radius, Vector3.down, out RaycastHit hit,
                castDistance, groundMask, QueryTriggerInteraction.Ignore);

            groundNormal = isGrounded ? hit.normal : Vector3.up;
        }

        bool IsTooSteep() => isGrounded && Vector3.Angle(groundNormal, Vector3.up) > slopeLimit;

        void HandleFacing()
        {
            Vector3 flatVelocity = Vector3.ProjectOnPlane(momentum, Vector3.up);
            if (flatVelocity.sqrMagnitude < 0.01f) return;

            Quaternion targetRot = Quaternion.LookRotation(flatVelocity, Vector3.up);
            Quaternion newRot = Quaternion.RotateTowards(_rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
            _rb.MoveRotation(newRot);
        }

        void HandleMomentum()
        {
            Vector3 vertical = Vector3.Project(momentum, Vector3.up);
            Vector3 horizontal = momentum - vertical;

            vertical -= Vector3.up * (Physics.gravity.magnitude * Time.deltaTime);
            if (isGrounded && !IsTooSteep() && Vector3.Dot(vertical, Vector3.up) < 0f)
                vertical = Vector3.zero;

            if (IsTooSteep())
            {
                horizontal = Vector3.ProjectOnPlane(horizontal, groundNormal);
                Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                horizontal += slideDir * (slideGravity * Time.deltaTime);
            }
            else
            {
                var ctx = new MovementContext(
                    moveInput, sprintHeld, isGrounded, groundNormal, horizontal, Time.deltaTime);

                Vector3 wishDir = currentStrategy != null ? currentStrategy.GetHorizontalTarget(ctx) : Vector3.zero;
                if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

                float speed = moveSpeed * (sprintHeld ? 1.6f : 1f);
                Vector3 targetVel = wishDir * speed;
                float rate = isGrounded ? acceleration : airControl;
                horizontal = Vector3.MoveTowards(horizontal, targetVel, rate * Time.deltaTime);

                float friction = isGrounded ? groundFriction : airFriction;
                if (wishDir.sqrMagnitude < 0.01f)
                    horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, friction * Time.deltaTime);
            }

            momentum = horizontal + vertical;
        }
    }
}