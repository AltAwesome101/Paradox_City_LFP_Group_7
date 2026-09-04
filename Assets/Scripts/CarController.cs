using UnityEngine;
using StarterAssets;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    // =========================================================
    // DRIVING
    // =========================================================

    [Header("===== DRIVING =====")]

    public float maxSpeed = 20f;
    public float maxReverseSpeed = 6f;
    public float acceleration = 10f;
    public float turnSpeed = 100f;
    public float steerResponsiveness = 7f;
    public float tireGrip = 12f;


    // =========================================================
    // GROUND
    // =========================================================

    [Header("===== GROUND =====")]

    public LayerMask groundMask = ~0;

    [Tooltip("Distance between the bottom of the car and the ground.")]
    public float groundOffset = 0.30f;

    [Tooltip("How far down each ground probe searches.")]
    public float groundProbeDistance = 4f;

    [Tooltip("How quickly the car follows changes in ground height.")]
    public float groundSnapSpeed = 14f;

    [Tooltip("Maximum slope that can be driven over.")]
    [Range(0f, 89f)]
    public float maximumSlopeAngle = 55f;


    // =========================================================
    // PAVEMENT / STEP CLIMBING
    // =========================================================

    [Header("===== PAVEMENT CLIMBING =====")]

    [Tooltip("Distance ahead of the car used to detect raised pavement.")]
    public float pavementLookAhead = 0.8f;

    [Tooltip("Maximum vertical height the car can smoothly climb.")]
    public float maximumStepHeight = 0.55f;

    [Tooltip("How quickly the car climbs a pavement edge.")]
    public float stepClimbSpeed = 10f;

    [Tooltip("Radius of the pavement detection sphere.")]
    public float pavementProbeRadius = 0.20f;


    // =========================================================
    // FRONT / REAR PROBES
    // =========================================================

    [Header("===== GROUND PROBES =====")]

    [Tooltip("Forward distance of front probes.")]
    public float frontProbeDistance = 1.25f;

    [Tooltip("Backward distance of rear probes.")]
    public float rearProbeDistance = 1.10f;

    [Tooltip("Distance from the centre to left/right probes.")]
    public float sideProbeDistance = 0.65f;

    [Tooltip("Height above the car used to start the probes.")]
    public float probeStartHeight = 1.5f;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("===== DEBUG =====")]

    public bool enableDebugLogs = true;
    public bool enableDebugGUI = true;
    public bool drawGroundProbes = true;
    public bool drawPavementProbe = true;
    public bool drawForward = true;
    public bool drawGroundPoint = true;

    [Tooltip("How often Console debug messages appear.")]
    public float debugInterval = 1f;


    // =========================================================
    // DAMAGE
    // =========================================================

    [Header("===== COLLISION DAMAGE =====")]

    public float minImpactSpeedForDamage = 4f;
    public float wallDamageAmount = 15f;

    [Range(0f, 1f)]
    public float groundNormalThreshold = 0.6f;

    public float damageCooldown = 0.4f;


    // =========================================================
    // HYPER REVERSE TURN
    // =========================================================

    [Header("===== HYPER REVERSE TURN =====")]

    [Tooltip("How many seconds the player must hold S while actually rolling backward before the hyper turn triggers.")]
    public float reverseHoldTimeForFlip = 2f;

    [Tooltip("How long the 180-degree snap-turn takes to visually play out.")]
    public float flipSpinDuration = 0.35f;

    [Tooltip("Minimum reverse speed required to count as 'actively reversing' for the timer.")]
    public float minReverseSpeedForFlip = 0.5f;

    [Tooltip("Color of the code-generated burst particle effect played when the flip triggers.")]
    public Color hyperTurnParticleColor = new Color(0.3f, 0.75f, 1f, 1f);

    [Tooltip("How many particles spawn in the burst.")]
    public int hyperTurnParticleCount = 40;


    // =========================================================
    // ENTER / EXIT (INTERACT PROMPT)
    // =========================================================

    [Header("===== ENTER / EXIT CAR =====")]

    [Tooltip("Leave empty to auto-find the GameObject tagged 'Player' at Awake.")]
    public Transform player;

    [Tooltip("How close the player needs to be to see the prompt and press F to get in.")]
    public float driveInteractRange = 3.5f;

    [Tooltip("Assign a UI element (world-space canvas Text, or screen overlay) that says 'Press F to Drive'. Shown automatically when the player is nearby and not already driving.")]
    public GameObject interactPromptUI;

    public KeyCode enterExitKey = KeyCode.F;

    [Tooltip("Optional - drag the player's ThirdPersonController here to automatically lock/unlock their movement while they're driving.")]
    public ThirdPersonController thirdPersonController;


    // =========================================================
    // PUBLIC
    // =========================================================

    [HideInInspector]
    public bool isBeingDriven = false;

    [HideInInspector]
    public HealthController driverHealth;

    public bool IsGrounded => grounded;


    // =========================================================
    // PRIVATE
    // =========================================================

    private Rigidbody rb;

    private float currentSpeed;
    private float currentSteerInput;

    private float lastDamageTime = -999f;
    private float lastDebugTime = -999f;

    private bool grounded;
    private bool pavementDetected;
    private bool climbingPavement;

    private float groundHeight;
    private float groundDistance;
    private float groundAngle;

    private Vector3 groundNormal = Vector3.up;
    private Vector3 groundPoint;

    private Vector3[] probePoints;
    private bool[] probeHits;
    private float[] probeHeights;

    // Hyper reverse turn state
    private float reverseHoldTimer;
    private bool isFlipping;
    private float flipTimer;
    private Quaternion flipStartRotation;
    private Quaternion flipTargetRotation;
    private float flipCarrySpeed;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError(
                "[CAR DEBUG] Rigidbody missing!"
            );

            return;
        }

        /*
         * Arcade-style vehicle.
         *
         * The car is not allowed to physically rotate
         * around X or Z.
         */
        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        /*
         * Gravity is disabled because vertical movement
         * is controlled manually by the ground probes.
         */
        rb.useGravity = false;

        rb.angularDamping = 20f;

        rb.angularVelocity =
            Vector3.zero;

        /*
         * Lower centre of mass helps prevent physical
         * instability.
         */
        rb.centerOfMass =
            new Vector3(
                0f,
                -0.5f,
                0f
            );

        probePoints =
            new Vector3[5];

        probeHits =
            new bool[5];

        probeHeights =
            new float[5];

        Debug.Log(
            "[CAR DEBUG] ================================="
        );

        Debug.Log(
            "[CAR DEBUG] CAR CONTROLLER INITIALIZED"
        );

        Debug.Log(
            "[CAR DEBUG] Gravity disabled"
        );

        Debug.Log(
            "[CAR DEBUG] X/Z rotation frozen"
        );

        Debug.Log(
            "[CAR DEBUG] Vertical position controlled by probes"
        );

        Debug.Log(
            "[CAR DEBUG] ================================="
        );

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");

            if (p != null)
                player = p.transform;
            else if (enableDebugLogs)
                Debug.LogWarning("[CAR DEBUG] No GameObject tagged 'Player' found - " +
                                  "the 'Press F to Drive' prompt won't be able to show. " +
                                  "Tag your player, or drag it into the 'player' field.");
        }

        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);
    }


    // =========================================================
    // FIXED UPDATE
    // =========================================================

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        DetectGround();

        DetectPavement();

        ReadInput();

        HandleHyperReverseTurn();

        /*
         * While the snap-turn animation is playing, skip normal
         * driving entirely - the flip owns rotation and velocity
         * for its short duration.
         */
        if (isFlipping)
        {
            UpdateHyperFlip();

            MoveCarHeight();

            PrintDebugInfo();

            return;
        }

        HandleSpeed();

        HandleSteering();

        MoveCar();

        MoveCarHeight();

        KeepCarFlat();

        PreventPhysicsRotation();

        PrintDebugInfo();
    }


    // =========================================================
    // GROUND DETECTION
    // =========================================================

    private void DetectGround()
    {
        /*
         * Probe layout:
         *
         * 0 = Front Left
         * 1 = Front Centre
         * 2 = Front Right
         * 3 = Rear Left
         * 4 = Rear Right
         */

        Vector3[] localPositions =
        {
            new Vector3(
                -sideProbeDistance,
                probeStartHeight,
                frontProbeDistance
            ),

            new Vector3(
                0f,
                probeStartHeight,
                frontProbeDistance
            ),

            new Vector3(
                sideProbeDistance,
                probeStartHeight,
                frontProbeDistance
            ),

            new Vector3(
                -sideProbeDistance,
                probeStartHeight,
                -rearProbeDistance
            ),

            new Vector3(
                sideProbeDistance,
                probeStartHeight,
                -rearProbeDistance
            )
        };

        int hits = 0;

        float heightSum = 0f;

        Vector3 normalSum =
            Vector3.zero;

        Vector3 pointSum =
            Vector3.zero;

        float lowestHeight =
            float.MaxValue;

        float highestHeight =
            float.MinValue;

        for (int i = 0; i < 5; i++)
        {
            Vector3 origin =
                transform.TransformPoint(
                    localPositions[i]
                );

            probePoints[i] =
                origin;

            probeHits[i] =
                Physics.Raycast(
                    origin,
                    Vector3.down,
                    out RaycastHit hit,
                    groundProbeDistance,
                    groundMask,
                    QueryTriggerInteraction.Ignore
                );

            if (probeHits[i])
            {
                float height =
                    hit.point.y +
                    groundOffset;

                probeHeights[i] =
                    height;

                heightSum +=
                    height;

                normalSum +=
                    hit.normal;

                pointSum +=
                    hit.point;

                lowestHeight =
                    Mathf.Min(
                        lowestHeight,
                        height
                    );

                highestHeight =
                    Mathf.Max(
                        highestHeight,
                        height
                    );

                hits++;

                Debug.DrawLine(
                    origin,
                    hit.point,
                    Color.green
                );
            }
            else
            {
                probeHeights[i] =
                    float.NaN;

                Debug.DrawLine(
                    origin,
                    origin +
                    Vector3.down *
                    groundProbeDistance,
                    Color.red
                );
            }
        }

        if (hits > 0)
        {
            groundHeight =
                heightSum / hits;

            groundNormal =
                (
                    normalSum / hits
                ).normalized;

            groundPoint =
                pointSum / hits;

            groundAngle =
                Vector3.Angle(
                    groundNormal,
                    Vector3.up
                );

            grounded =
                groundAngle <=
                maximumSlopeAngle;

            groundDistance =
                transform.position.y -
                groundHeight;
        }
        else
        {
            grounded = false;

            groundHeight =
                transform.position.y;

            groundNormal =
                Vector3.up;

            groundPoint =
                transform.position;

            groundAngle = 0f;

            groundDistance = -1f;
        }

        /*
         * Detect whether the front of the vehicle is
         * sitting higher than the rear.
         */
        if (hits >= 3)
        {
            float frontAverage =
                GetFrontHeight();

            float rearAverage =
                GetRearHeight();

            float heightDifference =
                frontAverage -
                rearAverage;

            climbingPavement =
                heightDifference >
                0.05f &&
                heightDifference <=
                maximumStepHeight;
        }
        else
        {
            climbingPavement = false;
        }
    }


    // =========================================================
    // FRONT HEIGHT
    // =========================================================

    private float GetFrontHeight()
    {
        float total = 0f;
        int count = 0;

        for (int i = 0; i < 3; i++)
        {
            if (!float.IsNaN(
                probeHeights[i]))
            {
                total +=
                    probeHeights[i];

                count++;
            }
        }

        if (count == 0)
            return transform.position.y;

        return total / count;
    }


    // =========================================================
    // REAR HEIGHT
    // =========================================================

    private float GetRearHeight()
    {
        float total = 0f;
        int count = 0;

        for (int i = 3; i < 5; i++)
        {
            if (!float.IsNaN(
                probeHeights[i]))
            {
                total +=
                    probeHeights[i];

                count++;
            }
        }

        if (count == 0)
            return transform.position.y;

        return total / count;
    }


    // =========================================================
    // PAVEMENT DETECTION
    // =========================================================

    private void DetectPavement()
    {
        pavementDetected = false;

        Vector3 origin =
            transform.position +
            transform.forward *
            pavementLookAhead;

        origin.y +=
            probeStartHeight;

        Debug.DrawRay(
            origin,
            Vector3.down *
            groundProbeDistance,
            Color.cyan
        );

        /*
         * SphereCast gives the pavement detector
         * some tolerance around the edge.
         */
        if (Physics.SphereCast(
            origin,
            pavementProbeRadius,
            Vector3.down,
            out RaycastHit hit,
            groundProbeDistance,
            groundMask,
            QueryTriggerInteraction.Ignore))
        {
            float difference =
                (
                    hit.point.y +
                    groundOffset
                ) -
                transform.position.y;

            if (
                difference > 0.02f &&
                difference <=
                maximumStepHeight)
            {
                pavementDetected = true;

                Debug.DrawLine(
                    origin,
                    hit.point,
                    Color.magenta
                );
            }
        }
    }


    // =========================================================
    // INPUT
    // =========================================================

    private void ReadInput()
    {
        bool w =
            Input.GetKey(KeyCode.W);

        bool a =
            Input.GetKey(KeyCode.A);

        bool s =
            Input.GetKey(KeyCode.S);

        bool d =
            Input.GetKey(KeyCode.D);

        if (
            enableDebugLogs &&
            Time.time -
            lastDebugTime >
            debugInterval)
        {
            Debug.Log(
                "[CAR DEBUG] INPUT | " +
                "Driven=" +
                isBeingDriven +
                " | W=" +
                w +
                " | A=" +
                a +
                " | S=" +
                s +
                " | D=" +
                d
            );
        }
    }


    // =========================================================
    // SPEED
    // =========================================================

    private void HandleSpeed()
    {
        bool throttle =
            isBeingDriven &&
            Input.GetKey(KeyCode.W);

        bool brake =
            isBeingDriven &&
            Input.GetKey(KeyCode.S);

        if (!isBeingDriven)
        {
            currentSpeed =
                Mathf.MoveTowards(
                    currentSpeed,
                    0f,
                    acceleration *
                    Time.fixedDeltaTime
                );

            return;
        }

        if (throttle)
        {
            currentSpeed =
                Mathf.MoveTowards(
                    currentSpeed,
                    maxSpeed,
                    acceleration *
                    Time.fixedDeltaTime
                );
        }
        else if (brake)
        {
            if (currentSpeed > 0.05f)
            {
                currentSpeed =
                    Mathf.MoveTowards(
                        currentSpeed,
                        0f,
                        acceleration *
                        2f *
                        Time.fixedDeltaTime
                    );
            }
            else
            {
                currentSpeed =
                    Mathf.MoveTowards(
                        currentSpeed,
                        -maxReverseSpeed,
                        acceleration *
                        Time.fixedDeltaTime
                    );
            }
        }
        else
        {
            currentSpeed =
                Mathf.MoveTowards(
                    currentSpeed,
                    0f,
                    acceleration *
                    0.5f *
                    Time.fixedDeltaTime
                );
        }
    }


    // =========================================================
    // STEERING
    // =========================================================

    private void HandleSteering()
    {
        float target = 0f;

        if (isBeingDriven)
        {
            if (Input.GetKey(KeyCode.A))
                target = -1f;

            if (Input.GetKey(KeyCode.D))
                target = 1f;
        }

        currentSteerInput =
            Mathf.MoveTowards(
                currentSteerInput,
                target,
                steerResponsiveness *
                Time.fixedDeltaTime
            );

        if (
            Mathf.Abs(
                currentSteerInput
            ) < 0.001f)
        {
            return;
        }

        float speedFactor =
            Mathf.Clamp01(
                Mathf.Abs(currentSpeed) /
                maxSpeed
            );

        speedFactor =
            Mathf.Max(
                speedFactor,
                0.15f
            );

        float direction =
            currentSpeed >= 0f
                ? 1f
                : -1f;

        float rotationAmount =
            currentSteerInput *
            turnSpeed *
            speedFactor *
            direction *
            Time.fixedDeltaTime;

        Quaternion turn =
            Quaternion.Euler(
                0f,
                rotationAmount,
                0f
            );

        rb.MoveRotation(
            rb.rotation *
            turn
        );
    }


    // =========================================================
    // HYPER REVERSE TURN
    // =========================================================

    
    private void HandleHyperReverseTurn()
    {
        if (isFlipping)
            return;

        bool activelyReversing =
            isBeingDriven &&
            grounded &&
            Input.GetKey(KeyCode.S) &&
            currentSpeed < -minReverseSpeedForFlip;

        if (activelyReversing)
        {
            reverseHoldTimer += Time.fixedDeltaTime;

            if (reverseHoldTimer >= reverseHoldTimeForFlip)
            {
                StartHyperFlip();
                reverseHoldTimer = 0f;
            }
        }
        else
        {
            reverseHoldTimer = 0f;
        }
    }

    private void StartHyperFlip()
    {
        isFlipping = true;
        flipTimer = 0f;

        flipStartRotation = rb.rotation;
        flipTargetRotation = rb.rotation * Quaternion.Euler(0f, 180f, 0f);

        
        flipCarrySpeed = Mathf.Abs(currentSpeed);

        SpawnHyperTurnParticles();

        if (enableDebugLogs)
        {
            Debug.Log(
                "[CAR DEBUG] HYPER REVERSE TURN triggered | " +
                "carrySpeed=" +
                flipCarrySpeed.ToString("F2")
            );
        }
    }

    private void UpdateHyperFlip()
    {
        flipTimer += Time.fixedDeltaTime;

        float t = Mathf.Clamp01(flipTimer / Mathf.Max(0.01f, flipSpinDuration));

        
        float eased = 1f - Mathf.Pow(1f - t, 3f);

        Quaternion rotationNow = Quaternion.Slerp(flipStartRotation, flipTargetRotation, eased);
        rb.MoveRotation(rotationNow);

        
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (t >= 1f)
        {
            isFlipping = false;
            currentSpeed = flipCarrySpeed;
        }
    }

    
    private void SpawnHyperTurnParticles()
    {
        GameObject fx = new GameObject("HyperTurnFX");
        fx.transform.position = transform.position + Vector3.up * 0.5f;

        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = 6f;
        main.startSize = 2f;
        main.startColor = hyperTurnParticleColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, (short)hyperTurnParticleCount)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.5f;

        
        ParticleSystemRenderer psRenderer = fx.GetComponent<ParticleSystemRenderer>();
        Shader fxShader =
            Shader.Find("Particles/Standard Unlit") ??
            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
            Shader.Find("Sprites/Default");

        if (fxShader != null)
            psRenderer.material = new Material(fxShader);

        ps.Play();

        Destroy(fx, main.duration + main.startLifetime.constantMax + 0.5f);
    }


    // =========================================================
    // MOVEMENT
    // =========================================================

    private void MoveCar()
    {
        Vector3 forward =
            transform.forward;

        /*
         * Keep movement completely horizontal.
         *
         * The vehicle never physically drives its nose
         * upward when encountering a slope.
         */
        forward.y = 0f;

        if (
            forward.sqrMagnitude <
            0.001f)
        {
            forward =
                Vector3.forward;
        }

        forward.Normalize();

        Vector3 targetVelocity =
            forward *
            currentSpeed;

        /*
         * Absolutely no vertical movement through
         * the velocity system.
         */
        targetVelocity.y = 0f;

        Vector3 currentVelocity =
            rb.linearVelocity;

        float forwardVelocity =
            Vector3.Dot(
                currentVelocity,
                forward
            );

        Vector3 sidewaysVelocity =
            currentVelocity -
            forward *
            forwardVelocity;

        sidewaysVelocity.y = 0f;

        sidewaysVelocity =
            Vector3.MoveTowards(
                sidewaysVelocity,
                Vector3.zero,
                tireGrip *
                Time.fixedDeltaTime
            );

        rb.linearVelocity =
            targetVelocity +
            sidewaysVelocity;
    }


    // =========================================================
    // HEIGHT CONTROL
    // =========================================================

    private void MoveCarHeight()
    {
        /*
         * If the car has no ground underneath it,
         * do not allow upward/downward velocity.
         */
        if (!grounded)
        {
            Vector3 airVelocity =
                rb.linearVelocity;

            airVelocity.y = 0f;

            rb.linearVelocity =
                airVelocity;

            return;
        }

        float targetHeight =
            groundHeight;

        /*
         * If pavement is detected, allow the car to
         * begin moving toward the higher front section.
         */
        if (pavementDetected)
        {
            float frontHeight =
                GetFrontHeight();

            if (
                frontHeight >
                targetHeight)
            {
                targetHeight =
                    frontHeight;
            }
        }

        Vector3 position =
            rb.position;

        float difference =
            targetHeight -
            position.y;

        float heightSpeed =
            groundSnapSpeed;

        if (climbingPavement)
        {
            heightSpeed =
                stepClimbSpeed;
        }

        /*
         * Move vertically in a controlled way.
         *
         * This avoids suspension-style bouncing.
         */
        float newY =
            Mathf.MoveTowards(
                position.y,
                targetHeight,
                heightSpeed *
                Time.fixedDeltaTime
            );

        position.y =
            newY;

        rb.MovePosition(
            position
        );

        /*
         * Kill any remaining vertical velocity.
         */
        Vector3 velocity =
            rb.linearVelocity;

        velocity.y = 0f;

        rb.linearVelocity =
            velocity;
    }


    // =========================================================
    // KEEP FLAT
    // =========================================================

    private void KeepCarFlat()
    {
        /*
         * ALWAYS use world UP.
         *
         * The car therefore remains completely flat:
         *
         * Flat road  -> flat
         * Ramp       -> flat
         * Side slope -> flat
         */
        Vector3 forward =
            transform.forward;

        forward.y = 0f;

        if (
            forward.sqrMagnitude <
            0.001f)
        {
            forward =
                Vector3.forward;
        }

        forward.Normalize();

        Quaternion targetRotation =
            Quaternion.LookRotation(
                forward,
                Vector3.up
            );

        rb.MoveRotation(
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                25f *
                Time.fixedDeltaTime
            )
        );
    }


    // =========================================================
    // PHYSICS LOCK
    // =========================================================

    private void PreventPhysicsRotation()
    {
        /*
         * No vertical bouncing velocity.
         */
        Vector3 velocity =
            rb.linearVelocity;

        velocity.y = 0f;

        rb.linearVelocity =
            velocity;

        /*
         * No physical angular velocity.
         */
        rb.angularVelocity =
            Vector3.zero;

        /*
         * Force X and Z rotation back to zero.
         */
        Vector3 euler =
            rb.rotation.eulerAngles;

        Quaternion flat =
            Quaternion.Euler(
                0f,
                euler.y,
                0f
            );

        rb.MoveRotation(
            flat
        );
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void PrintDebugInfo()
    {
        if (!enableDebugLogs)
            return;

        if (
            Time.time -
            lastDebugTime <
            debugInterval)
        {
            return;
        }

        lastDebugTime =
            Time.time;

        Debug.Log(
            "========== CAR DEBUG ==========\n" +

            "Driven: " +
            isBeingDriven +

            "\nGrounded: " +
            grounded +

            "\nPavement Detected: " +
            pavementDetected +

            "\nClimbing Pavement: " +
            climbingPavement +

            "\nGround Height: " +
            groundHeight.ToString("F3") +

            "\nCar Height: " +
            transform.position.y.ToString("F3") +

            "\nHeight Difference: " +
            (
                groundHeight -
                transform.position.y
            ).ToString("F3") +

            "\nGround Distance: " +
            groundDistance.ToString("F3") +

            "\nGround Angle: " +
            groundAngle.ToString("F2") +

            "°\nGround Normal: " +
            groundNormal +

            "\nGround Point: " +
            groundPoint +

            "\nSpeed: " +
            currentSpeed.ToString("F2") +

            "\nSteering: " +
            currentSteerInput.ToString("F2") +

            "\nVelocity: " +
            rb.linearVelocity +

            "\nAngular Velocity: " +
            rb.angularVelocity +

            "\nRotation: " +
            transform.eulerAngles +

            "\n================================"
        );
    }


    // =========================================================
    // INTERACT PROMPT / ENTER-EXIT
    // =========================================================

    /*
     * Runs in Update (not FixedUpdate) so GetKeyDown can't miss a
     * press between physics steps.
     */
    private void Update()
    {
        UpdateInteractPrompt();
        HandleEnterExitInput();
    }

    private void UpdateInteractPrompt()
    {
        if (interactPromptUI == null || player == null)
            return;

        bool nearby = Vector3.Distance(player.position, transform.position) <= driveInteractRange;
        bool shouldShow = nearby && !isBeingDriven;

        if (interactPromptUI.activeSelf != shouldShow)
            interactPromptUI.SetActive(shouldShow);
    }

    private void HandleEnterExitInput()
    {
        if (player == null)
            return;

        if (!Input.GetKeyDown(enterExitKey))
            return;

        if (!isBeingDriven)
        {
            float dist = Vector3.Distance(player.position, transform.position);

            if (dist <= driveInteractRange)
                EnterCar();
        }
        else
        {
            ExitCar();
        }
    }

    private void EnterCar()
    {
        isBeingDriven = true;

        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);

        if (thirdPersonController != null)
            thirdPersonController.enabled = false;

        if (enableDebugLogs)
            Debug.Log("[CAR DEBUG] Player entered the car.");
    }

    private void ExitCar()
    {
        isBeingDriven = false;

        if (thirdPersonController != null)
            thirdPersonController.enabled = true;

        if (enableDebugLogs)
            Debug.Log("[CAR DEBUG] Player exited the car.");
    }


    // =========================================================
    // COLLISION DAMAGE
    // =========================================================

    private void OnCollisionEnter(
        Collision collision)
    {
        if (!isBeingDriven)
            return;

        if (driverHealth == null)
            return;

        if (
            Time.time -
            lastDamageTime <
            damageCooldown)
        {
            return;
        }

        foreach (
            ContactPoint contact
            in collision.contacts)
        {
            float groundDot =
                Mathf.Abs(
                    Vector3.Dot(
                        contact.normal,
                        Vector3.up
                    )
                );

            /*
             * Ignore ground and pavement contacts.
             */
            if (
                groundDot >
                groundNormalThreshold)
            {
                continue;
            }

            float impactSpeed =
                Mathf.Abs(
                    Vector3.Dot(
                        collision.relativeVelocity,
                        contact.normal
                    )
                );

            if (
                impactSpeed <
                minImpactSpeedForDamage)
            {
                continue;
            }

            if (enableDebugLogs)
            {
                Debug.Log(
                    "[CAR DEBUG] DAMAGE | " +
                    "Impact Speed = " +
                    impactSpeed.ToString("F2") +
                    " | Object = " +
                    collision.gameObject.name
                );
            }

            driverHealth.DamagePlayer(
                wallDamageAmount
            );

            lastDamageTime =
                Time.time;

            break;
        }
    }


    // =========================================================
    // DEBUG GUI
    // =========================================================

    private void OnGUI()
    {
        if (!enableDebugGUI)
            return;

        GUI.Box(
            new Rect(
                10,
                10,
                400,
                350
            ),
            ""
        );

        GUI.Label(
            new Rect(
                20,
                20,
                380,
                25
            ),
            "CAR DEBUG"
        );

        GUI.Label(
            new Rect(
                20,
                50,
                380,
                20
            ),
            "Grounded: " +
            grounded
        );

        GUI.Label(
            new Rect(
                20,
                70,
                380,
                20
            ),
            "Pavement: " +
            pavementDetected
        );

        GUI.Label(
            new Rect(
                20,
                90,
                380,
                20
            ),
            "Climbing: " +
            climbingPavement
        );

        GUI.Label(
            new Rect(
                20,
                110,
                380,
                20
            ),
            "Ground Height: " +
            groundHeight.ToString("F2")
        );

        GUI.Label(
            new Rect(
                20,
                130,
                380,
                20
            ),
            "Car Height: " +
            transform.position.y.ToString("F2")
        );

        GUI.Label(
            new Rect(
                20,
                150,
                380,
                20
            ),
            "Ground Distance: " +
            groundDistance.ToString("F2")
        );

        GUI.Label(
            new Rect(
                20,
                170,
                380,
                20
            ),
            "Ground Angle: " +
            groundAngle.ToString("F1") +
            "°"
        );

        GUI.Label(
            new Rect(
                20,
                190,
                380,
                20
            ),
            "Speed: " +
            currentSpeed.ToString("F2")
        );

        GUI.Label(
            new Rect(
                20,
                210,
                380,
                20
            ),
            "Steering: " +
            currentSteerInput.ToString("F2")
        );

        GUI.Label(
            new Rect(
                20,
                230,
                380,
                20
            ),
            "Velocity: " +
            rb.linearVelocity.ToString("F2")
        );

        GUI.Label(
            new Rect(
                20,
                250,
                380,
                20
            ),
            "Angular: " +
            rb.angularVelocity.ToString("F2")
        );

        GUI.Label(
            new Rect(
                20,
                270,
                380,
                20
            ),
            "Rotation: " +
            transform.eulerAngles.ToString("F1")
        );

        GUI.Label(
            new Rect(
                20,
                290,
                380,
                20
            ),
            "Normal: " +
            groundNormal.ToString()
        );

        GUI.Label(
            new Rect(
                20,
                315,
                380,
                20
            ),
            "A/D: " +
            (
                Input.GetKey(KeyCode.A)
                    ? "LEFT"
                    :
                Input.GetKey(KeyCode.D)
                    ? "RIGHT"
                    : "NONE"
            )
        );

        GUI.Label(
            new Rect(
                20,
                335,
                380,
                20
            ),
            "W/S: " +
            (
                Input.GetKey(KeyCode.W)
                    ? "FORWARD"
                    :
                Input.GetKey(KeyCode.S)
                    ? "BRAKE/REVERSE"
                    : "NONE"
            )
        );
    }


    // =========================================================
    // GIZMOS
    // =========================================================

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        /*
         * Ground probes.
         */
        if (drawGroundProbes)
        {
            Gizmos.color =
                Color.yellow;

            Vector3[] positions =
            {
                new Vector3(
                    -sideProbeDistance,
                    probeStartHeight,
                    frontProbeDistance
                ),

                new Vector3(
                    0f,
                    probeStartHeight,
                    frontProbeDistance
                ),

                new Vector3(
                    sideProbeDistance,
                    probeStartHeight,
                    frontProbeDistance
                ),

                new Vector3(
                    -sideProbeDistance,
                    probeStartHeight,
                    -rearProbeDistance
                ),

                new Vector3(
                    sideProbeDistance,
                    probeStartHeight,
                    -rearProbeDistance
                )
            };

            foreach (
                Vector3 localPosition
                in positions)
            {
                Vector3 world =
                    transform.TransformPoint(
                        localPosition
                    );

                Gizmos.DrawLine(
                    world,
                    world +
                    Vector3.down *
                    groundProbeDistance
                );

                Gizmos.DrawSphere(
                    world,
                    0.06f
                );
            }
        }

        /*
         * Pavement probe.
         */
        if (drawPavementProbe)
        {
            Gizmos.color =
                Color.magenta;

            Vector3 origin =
                transform.position +
                transform.forward *
                pavementLookAhead;

            origin.y +=
                probeStartHeight;

            Gizmos.DrawWireSphere(
                origin,
                pavementProbeRadius
            );

            Gizmos.DrawLine(
                origin,
                origin +
                Vector3.down *
                groundProbeDistance
            );
        }

        /*
         * Car forward.
         */
        if (drawForward)
        {
            Gizmos.color =
                Color.blue;

            Gizmos.DrawLine(
                transform.position,
                transform.position +
                transform.forward *
                2f
            );
        }

        /*
         * Ground point.
         */
        if (
            drawGroundPoint &&
            grounded)
        {
            Gizmos.color =
                Color.green;

            Gizmos.DrawSphere(
                groundPoint,
                0.12f
            );
        }
    }

#endif
}