using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;
using StarterAssets;

[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 4)]
    public string text;

    [Tooltip("Index in cameraswap1's camera list to switch to when this line starts. Use -1 to leave the camera where it is.")]
    public int cameraIndexForThisLine = -1;
}


public class BiboGuide : MonoBehaviour
{
    private enum State { Roaming, CheckInApproach, Quipping }

    [Header("Identity")]
    public string npcName = "BIBO";

    [Header("Flight - roaming")]
    [Tooltip("Optional waypoints BIBO flies between while idle. Place them in the air. " +
             "If empty, he hovers where he starts.")]
    public Transform[] patrolPoints;

    public float patrolPauseDuration = 2f;

    [Tooltip("Top flying speed (units per second).")]
    public float flySpeed = 8f;

    [Tooltip("Higher = slower, softer acceleration and braking.")]
    public float arriveSmoothTime = 0.6f;

    [Tooltip("Degrees per second BIBO turns.")]
    public float turnSpeed = 360f;

    [Tooltip("Add 90 / 180 / -90 if the robot model faces the wrong way when flying.")]
    public float facingYawOffset = 0f;

    [Tooltip("How close to a waypoint counts as arrived.")]
    public float waypointArriveRadius = 1f;

    [Header("Flight - ground safety")]
    [Tooltip("Layers that count as ground/buildings. BIBO never flies below these. " +
             "Default layer is fine unless your ground is on another layer.")]
    public LayerMask groundLayers = 1;

    [Tooltip("Minimum height BIBO stays above the ground. Raise it if the model's pivot is at its feet, " +
             "or if he still sinks into the floor.")]
    public float minGroundClearance = 1.5f;

    [Tooltip("The ground check starts this far above BIBO / his destination.")]
    public float groundCheckStartHeight = 10f;

    [Header("Flight - hover bob")]
    public float bobHeight = 0.15f;
    public float bobSpeed = 2f;

    [Header("Check-ins (random funny lines)")]
    public bool enableCheckIns = true;

    [Tooltip("Seconds between check-ins. Counts only while BIBO is roaming and the player is active.")]
    public float checkInInterval = 30f;

    [Tooltip("Random +/- seconds added to each interval so it feels less robotic. 0 = exact.")]
    public float checkInIntervalJitter = 0f;

    [Tooltip("How far from the player BIBO hovers while talking.")]
    public float checkInDistance = 2.5f;

    [Tooltip("How high above the player BIBO hovers while talking.")]
    public float checkInHeight = 1.5f;

    [Tooltip("BIBO flies faster than normal when heading to the player.")]
    public float checkInSpeedMultiplier = 1.5f;

    [Tooltip("Gives up and goes back to roaming if he can't reach the player in this many seconds.")]
    public float checkInGiveUpTime = 25f;

    [Tooltip("Seconds the line stays on screen after it has finished typing.")]
    public float quipLingerSeconds = 3f;

    [Header("Interaction")]
    [Tooltip("How close the player needs to be to press E and start talking to BIBO")]
    public float interactRange = 3.5f;

    [Tooltip("Optional: a small UI element that says 'Press E to Interact'.")]
    public GameObject interactPromptUI;

    [Header("Player Control Lock - assign ONE of these two")]
    public ThirdPersonController thirdPersonController;
    public PlayerScript playerScript;

    [Header("Animation (optional - off by default)")]
    [Tooltip("Leave OFF to just fly with no animations. Turn on later if you set up an Animator Controller.")]
    public bool useAnimator = false;

    [Tooltip("The robot's Animator. If empty, one is found on a child automatically.")]
    public Animator animator;

    [Tooltip("Exact (case-sensitive) name of the idle STATE in the Animator Controller.")]
    public string idleState = "Idle";

    [Tooltip("Exact (case-sensitive) name of the flying STATE in the Animator Controller.")]
    public string flyState = "fly";

    public float animFadeTime = 0.15f;

    [Header("Camera")]
    [Tooltip("Drag the scene's cameraswap1 object here")]
    public cameraswap1 cameraSwitcher;

    [Tooltip("Index of the normal player camera - restored once the dialogue ends")]
    public int normalCameraIndex = 0;

    [Header("Dialogue UI")]
    public GameObject dialogueBox;
    public Text dialogueText;
    public Text speakerNameText;

    [Tooltip("ONLY if your dialogue UI uses TextMeshPro instead of the normal UI Text. Assign these instead of the Text fields above.")]
    public TMP_Text dialogueTextTMP;
    public TMP_Text speakerNameTextTMP;

    [Tooltip("Seconds between each typed character")]
    public float typeSpeed = 0.02f;

    [Tooltip("Leave empty to use the built-in default line-up")]
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();

    [Header("Check-in UI (optional)")]
    [Tooltip("A separate small box for the random lines. If empty, the normal dialogue box is reused.")]
    public GameObject quipBox;
    public Text quipText;
    public TMP_Text quipTextTMP;

    [Header("Check-in lines (leave a list empty to use the built-in funny defaults)")]
    [Tooltip("Can be said at any time.")]
    public List<string> generalQuips = new List<string>();
    [Tooltip("Only used while the player has not completed any level yet.")]
    public List<string> earlyGameQuips = new List<string>();
    [Tooltip("Added once the Artist Hitler level is complete.")]
    public List<string> afterHitlerQuips = new List<string>();
    [Tooltip("Added once the Apple Forest level is complete.")]
    public List<string> afterAppleForestQuips = new List<string>();
    [Tooltip("Added once the Wrong Beer level is complete.")]
    public List<string> afterWrongBeerQuips = new List<string>();

    [Header("Debug")]
    public bool debugLogging = true;

    [Tooltip("How often (seconds) to print the range/status debug line.")]
    public float debugPrintInterval = 0.5f;

    private float debugTimer;

    private Rigidbody rb;
    private Transform player;
    private InteractIndicator indicator;

    // Flight
    private Vector3 _logicalPos;      
    private Vector3 _homePos;
    private Vector3 _flightVelocity;
    private bool _facePlayer;
    private int patrolIndex;
    private float patrolTimer;

    // Check-ins
    private State _state = State.Roaming;
    private float _checkInTimer;
    private float _currentInterval;
    private float _checkInElapsed;
    private Vector3 _checkInDir;
    private Coroutine _quipRoutine;
    private string _lastQuip;

    // Dialogue
    private bool inDialogue;
    private int currentLineIndex;
    private bool isTyping;
    private Coroutine typingCoroutine;

    // Animation
    private string _currentAnim;
    private bool _flyingAnim;
    private readonly HashSet<string> _warnedStates = new HashSet<string>();
    private float _nextPlayerSearch;

    private GameObject QuipPanel => quipBox != null ? quipBox : dialogueBox;

    private void Awake()
    {
        
        var leftoverAgent = GetComponent<NavMeshAgent>();
        if (leftoverAgent != null)
        {
            leftoverAgent.enabled = false;
            Debug.LogWarning("[BiboGuide] A NavMeshAgent is still on BIBO. It has been disabled - you can remove the component.");
        }

        
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        _logicalPos = transform.position;
        _homePos = transform.position;

        if (useAnimator && animator == null) animator = GetComponentInChildren<Animator>();

        
        foreach (var a in GetComponentsInChildren<Animator>(true))
            a.applyRootMotion = false;

        
        if (patrolPoints != null)
        {
            foreach (var wp in patrolPoints)
            {
                if (wp != null && wp != transform && wp.IsChildOf(transform))
                {
                    Debug.LogWarning($"[BiboGuide] Patrol point '{wp.name}' was a child of BIBO. It has been detached so it stays in place.");
                    wp.SetParent(null, true);
                }
            }
        }

        ResolvePlayer(true);

        // Create interaction indicator
        GameObject indicatorObj = new GameObject($"{npcName}_ExclamationMark");
        indicatorObj.transform.SetParent(transform);
        indicator = indicatorObj.AddComponent<InteractIndicator>();
        indicator.Init(transform);

        // Hide UI initially
        if (dialogueBox != null) dialogueBox.SetActive(false);
        if (quipBox != null) quipBox.SetActive(false);
        SetSpeaker(npcName);
        if (interactPromptUI != null) interactPromptUI.SetActive(false);

        AutoFindDialogueText();

        // Load defaults if nothing is assigned
        if (dialogueLines == null || dialogueLines.Count == 0) dialogueLines = GetDefaultDialogue();
        if (generalQuips == null || generalQuips.Count == 0) generalQuips = GetDefaultGeneralQuips();
        if (earlyGameQuips == null || earlyGameQuips.Count == 0) earlyGameQuips = GetDefaultEarlyGameQuips();
        if (afterHitlerQuips == null || afterHitlerQuips.Count == 0) afterHitlerQuips = GetDefaultHitlerQuips();
        if (afterAppleForestQuips == null || afterAppleForestQuips.Count == 0) afterAppleForestQuips = GetDefaultAppleQuips();
        if (afterWrongBeerQuips == null || afterWrongBeerQuips.Count == 0) afterWrongBeerQuips = GetDefaultBeerQuips();

        ResetCheckInTimer();
    }

    private void Update()
    {
        // The player may only become active after the main menu's Play button
        ResolvePlayer(false);

        // Debug E press
        if (debugLogging && Input.GetKeyDown(KeyCode.E))
        {
            float distNow = player != null ? Vector3.Distance(player.position, transform.position) : -1f;
            Debug.Log(
                $"[BiboGuide][E PRESSED] inDialogue={inDialogue} playerFound={(player != null)} " +
                $"distance={distNow:F2} interactRange={interactRange} inRange={PlayerInRange()}");
        }

        // =========================================================
        // DIALOGUE
        // =========================================================
        if (inDialogue)
        {
            // Brake to a smooth hover and keep facing the player
            FlyTowards(_logicalPos, flySpeed);
            _facePlayer = true;

            HandleDialogueInput();
            ApplyMotion();
            return;
        }

        // =========================================================
        // FLIGHT / CHECK-INS
        // =========================================================
        switch (_state)
        {
            case State.Roaming: UpdateRoaming(); break;
            case State.CheckInApproach: UpdateCheckInApproach(); break;
            case State.Quipping: UpdateQuipping(); break;
        }

        bool playerNearby = PlayerInRange();
        UpdateInteractPrompt(playerNearby);

        // Debug status
        if (debugLogging && debugPrintInterval > 0f)
        {
            debugTimer += Time.deltaTime;
            if (debugTimer >= debugPrintInterval)
            {
                debugTimer = 0f;

                if (player == null)
                {
                    Debug.LogWarning("[BiboGuide][STATUS] player reference is NULL.");
                }
                else
                {
                    float dist = Vector3.Distance(player.position, transform.position);
                    Debug.Log(
                        $"[BiboGuide][STATUS] state={_state} distance={dist:F2} / interactRange={interactRange} " +
                        $"-> inRange={playerNearby} nextCheckIn={Mathf.Max(0f, _currentInterval - _checkInTimer):F0}s");
                }
            }
        }

        // =========================================================
        // START INTERACTION
        // =========================================================
        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (debugLogging) Debug.Log("[BiboGuide] Conditions met - calling StartDialogue().");
            StartDialogue();
        }

        ApplyMotion();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);

        if (patrolPoints == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;
            Gizmos.DrawWireSphere(patrolPoints[i].position, waypointArriveRadius);

            var next = patrolPoints[(i + 1) % patrolPoints.Length];
            if (next != null) Gizmos.DrawLine(patrolPoints[i].position, next.position);
        }
    }

    // =============================================================
    // PLAYER LOOKUP
    // =============================================================

    private bool PlayerReady()
    {
        return player != null && player.gameObject.activeInHierarchy;
    }

    private void ResolvePlayer(bool logWarnings)
    {
        if (player != null) return;
        if (!logWarnings && Time.unscaledTime < _nextPlayerSearch) return;
        _nextPlayerSearch = Time.unscaledTime + 1f;

        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
        {
            player = p.transform;
        }
        else if (thirdPersonController != null)
        {
            player = thirdPersonController.transform;
            if (debugLogging && logWarnings)
                Debug.LogWarning("[BiboGuide] No GameObject tagged 'Player' found - falling back to the assigned ThirdPersonController.");
        }
        else if (playerScript != null)
        {
            player = playerScript.transform;
            if (debugLogging && logWarnings)
                Debug.LogWarning("[BiboGuide] No GameObject tagged 'Player' found - falling back to the assigned PlayerScript.");
        }
        else if (debugLogging && logWarnings)
        {
            Debug.LogWarning("[BiboGuide] No GameObject tagged 'Player' found, and neither thirdPersonController nor playerScript is assigned.");
        }
    }

    private bool PlayerInRange()
    {
        if (!PlayerReady()) return false;
        return Vector3.Distance(player.position, transform.position) <= interactRange;
    }

    // =============================================================
    // FLIGHT
    // =============================================================

    private readonly RaycastHit[] _groundHits = new RaycastHit[8];

    private void FlyTowards(Vector3 target, float maxSpeed)
    {
        // Keep the destination above the ground
        if (TryGetGroundY(target, out float targetGround))
            target.y = Mathf.Max(target.y, targetGround + minGroundClearance);

        _logicalPos = Vector3.SmoothDamp(_logicalPos, target, ref _flightVelocity, arriveSmoothTime, maxSpeed);

        // Safety net: never sink into the ground on the way
        if (TryGetGroundY(_logicalPos, out float currentGround))
            _logicalPos.y = Mathf.Max(_logicalPos.y, currentGround + minGroundClearance * 0.5f);
    }

    
    private bool TryGetGroundY(Vector3 pos, out float groundY)
    {
        groundY = 0f;
        if (groundLayers.value == 0) return false;

        Vector3 origin = pos + Vector3.up * groundCheckStartHeight;
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, _groundHits,
                                            groundCheckStartHeight + 100f, groundLayers,
                                            QueryTriggerInteraction.Ignore);

        bool found = false;
        float best = float.MinValue;

        for (int i = 0; i < count; i++)
        {
            Transform hitTransform = _groundHits[i].collider.transform;
            if (hitTransform.IsChildOf(transform)) continue;
            if (player != null && hitTransform.IsChildOf(player)) continue;

            if (_groundHits[i].point.y > best)
            {
                best = _groundHits[i].point.y;
                found = true;
            }
        }

        groundY = best;
        return found;
    }

    private Vector3 GetRoamTarget()
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Transform wp = patrolPoints[patrolIndex % patrolPoints.Length];
            if (wp != null) return wp.position;
        }
        return _homePos;
    }

    private void UpdateRoaming()
    {
        _facePlayer = false;

        Vector3 target = GetRoamTarget();
        FlyTowards(target, flySpeed);

        // Pause at each waypoint, then move on
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            if ((target - _logicalPos).sqrMagnitude <= waypointArriveRadius * waypointArriveRadius)
            {
                patrolTimer += Time.deltaTime;
                if (patrolTimer >= patrolPauseDuration)
                {
                    patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                    patrolTimer = 0f;
                }
            }
            else
            {
                patrolTimer = 0f;
            }
        }

        // Count down to the next check-in
        if (enableCheckIns && PlayerReady())
        {
            _checkInTimer += Time.deltaTime;
            if (_checkInTimer >= _currentInterval) StartCheckIn();
        }
    }

    private void ApplyMotion()
    {
        // Facing: look at the player when talking, otherwise face the direction of travel
        Vector3 face = Vector3.zero;

        if (_facePlayer && PlayerReady())
        {
            face = player.position - _logicalPos;
        }
        else
        {
            face = _flightVelocity;
        }

        face.y = 0f;
        float minFace = (_facePlayer && PlayerReady()) ? 0.04f : 0.25f;
        if (face.sqrMagnitude > minFace)
        {
            Quaternion target = Quaternion.LookRotation(face.normalized, Vector3.up) * Quaternion.Euler(0f, facingYawOffset, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        // Position with a gentle hover bob on top
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = _logicalPos + Vector3.up * bob;

        UpdateAnimation();
    }

    // =============================================================
    // ANIMATION
    // =============================================================

    private void UpdateAnimation()
    {
        if (!useAnimator) return;

        float speed = _flightVelocity.magnitude;

        // Two thresholds so it doesn't flicker between idle and fly
        if (!_flyingAnim && speed > 0.6f) _flyingAnim = true;
        else if (_flyingAnim && speed < 0.25f) _flyingAnim = false;

        PlayAnim(_flyingAnim ? flyState : idleState);
    }

    private void PlayAnim(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName) || stateName == _currentAnim) return;

        int hash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, hash))
        {
            if (_warnedStates.Add(stateName))
                Debug.LogWarning($"[BiboGuide] The Animator has no state called '{stateName}'. Check the spelling in the Inspector (case-sensitive).");
            return;
        }

        animator.CrossFadeInFixedTime(hash, animFadeTime);
        _currentAnim = stateName;
    }

    // =============================================================
    // CHECK-INS (random funny lines)
    // =============================================================

    private void ResetCheckInTimer()
    {
        _checkInTimer = 0f;
        _currentInterval = Mathf.Max(1f, checkInInterval + Random.Range(-checkInIntervalJitter, checkInIntervalJitter));
    }

    private Vector3 GetCheckInPoint()
    {
        return player.position + _checkInDir * checkInDistance + Vector3.up * checkInHeight;
    }

    private void StartCheckIn()
    {
        _state = State.CheckInApproach;
        _checkInElapsed = 0f;

        // Approach from the side BIBO is already on
        Vector3 away = _logicalPos - player.position;
        away.y = 0f;
        _checkInDir = away.sqrMagnitude > 0.01f ? away.normalized : player.forward;

        if (debugLogging) Debug.Log("[BiboGuide] Check-in started - flying to the player.");
    }

    private void UpdateCheckInApproach()
    {
        if (!PlayerReady())
        {
            AbortCheckIn();
            return;
        }

        _checkInElapsed += Time.deltaTime;

        Vector3 point = GetCheckInPoint();
        FlyTowards(point, flySpeed * checkInSpeedMultiplier);

        // Face where he is heading until he arrives
        _facePlayer = Vector3.Distance(_logicalPos, point) < checkInDistance * 2f;

        if (Vector3.Distance(_logicalPos, point) <= 1.5f)
        {
            BeginQuip();
        }
        else if (_checkInElapsed >= checkInGiveUpTime)
        {
            AbortCheckIn();
        }
    }

    private void UpdateQuipping()
    {
        if (!PlayerReady())
        {
            EndQuip();
            return;
        }

        // Keep hovering beside the player while talking
        FlyTowards(GetCheckInPoint(), flySpeed * checkInSpeedMultiplier);
        _facePlayer = true;
    }

    private void AbortCheckIn()
    {
        _state = State.Roaming;
        _facePlayer = false;
        _checkInTimer = _currentInterval * 0.5f; // try again a bit sooner
    }

    private void BeginQuip()
    {
        string line = PickQuip();
        if (string.IsNullOrEmpty(line))
        {
            AbortCheckIn();
            return;
        }

        _state = State.Quipping;
        _facePlayer = true;
        _quipRoutine = StartCoroutine(QuipRoutine(line));
    }

    private IEnumerator QuipRoutine(string text)
    {
        GameObject panel = QuipPanel;

        if (panel != null) panel.SetActive(true);
        SetSpeaker(npcName);
        SetQuipLabel("");

        for (int i = 1; i <= text.Length; i++)
        {
            SetQuipLabel(text.Substring(0, i));
            yield return new WaitForSecondsRealtime(typeSpeed);
        }

        yield return new WaitForSecondsRealtime(quipLingerSeconds);

        _quipRoutine = null;
        EndQuip();
    }

    private void EndQuip()
    {
        if (_quipRoutine != null)
        {
            StopCoroutine(_quipRoutine);
            _quipRoutine = null;
        }

        // Don't hide the shared box if a real conversation is using it
        GameObject panel = QuipPanel;
        if (panel != null && !inDialogue) panel.SetActive(false);

        _facePlayer = false;
        _state = State.Roaming;
        ResetCheckInTimer();
    }

    private string PickQuip()
    {
        var pool = new List<string>(generalQuips);
        var world = WorldStateManager.Instance;
        bool anyComplete = false;

        if (world.IsLevelCompleted(LevelId.ArtistHitler)) { pool.AddRange(afterHitlerQuips); anyComplete = true; }
        if (world.IsLevelCompleted(LevelId.AppleForest)) { pool.AddRange(afterAppleForestQuips); anyComplete = true; }
        if (world.IsLevelCompleted(LevelId.WrongBeer)) { pool.AddRange(afterWrongBeerQuips); anyComplete = true; }

        if (!anyComplete) pool.AddRange(earlyGameQuips);

        if (pool.Count == 0) return null;

        // Avoid saying the same line twice in a row
        string pick = pool[Random.Range(0, pool.Count)];
        for (int tries = 0; tries < 10 && pool.Count > 1 && pick == _lastQuip; tries++)
            pick = pool[Random.Range(0, pool.Count)];

        _lastQuip = pick;
        return pick;
    }

    // =============================================================
    // INTERACTION PROMPT
    // =============================================================

    private void UpdateInteractPrompt(bool playerNearby)
    {
        if (interactPromptUI == null) return;

        bool shouldShow = playerNearby && !inDialogue;
        if (interactPromptUI.activeSelf != shouldShow)
            interactPromptUI.SetActive(shouldShow);
    }

    // =============================================================
    // DIALOGUE
    // =============================================================

    private void StartDialogue()
    {
        // Cancel any random line that is currently showing
        EndQuip();

        inDialogue = true;
        currentLineIndex = -1;

        // Lock player
        SetPlayerControl(false);

        // Hide interaction indicators
        if (indicator != null) indicator.SetVisible(false);
        if (interactPromptUI != null) interactPromptUI.SetActive(false);

        // Show dialogue UI
        if (dialogueBox != null) dialogueBox.SetActive(true);
        SetSpeaker(npcName);

        if (dialogueText == null && dialogueTextTMP == null)
            Debug.LogError("[BiboGuide] Dialogue Text is not assigned in the Inspector, so no dialogue text can be shown. " +
                           "Press E to step through the lines and finish the conversation.");

        _facePlayer = true;

        if (debugLogging) Debug.Log("[BiboGuide] Dialogue started - BIBO is hovering in place.");

        AdvanceDialogue();
    }

    private void HandleDialogueInput()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        if (isTyping)
        {
            // Finish current line immediately
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);

            SetDialogue(dialogueLines[currentLineIndex].text);

            isTyping = false;
        }
        else
        {
            // Go to next dialogue line
            AdvanceDialogue();
        }
    }

    private void AdvanceDialogue()
    {
        currentLineIndex++;

        // Dialogue finished
        if (currentLineIndex >= dialogueLines.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = dialogueLines[currentLineIndex];

        if (debugLogging)
            Debug.Log($"[BiboGuide] Showing line {currentLineIndex + 1}/{dialogueLines.Count}");

        // Change camera if required (a bad camera index must never stop the dialogue)
        SwitchCamera(line.cameraIndexForThisLine);

        // Stop previous typing coroutine
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine(line.text));
    }

    private IEnumerator TypeLine(string text)
    {
        isTyping = true;

        SetDialogue("");

        for (int i = 1; i <= text.Length; i++)
        {
            SetDialogue(text.Substring(0, i));
            yield return new WaitForSecondsRealtime(typeSpeed);
        }

        isTyping = false;
    }

    private void EndDialogue()
    {
        inDialogue = false;

        // Hide dialogue
        if (dialogueBox != null) dialogueBox.SetActive(false);

        // Restore normal camera
        SwitchCamera(normalCameraIndex);

        // Restore player control
        SetPlayerControl(true);

        // Back to flying around; the next check-in countdown starts fresh
        _facePlayer = false;
        _state = State.Roaming;
        ResetCheckInTimer();

        // Restore interaction indicator
        if (indicator != null) indicator.SetVisible(true);

        if (debugLogging) Debug.Log("[BiboGuide] Dialogue ended - BIBO resumed flying.");
    }

    // =============================================================
    // UI / CAMERA HELPERS
    // =============================================================

    private void SetDialogue(string value)
    {
        if (dialogueText != null) dialogueText.text = value;
        if (dialogueTextTMP != null) dialogueTextTMP.text = value;
    }

    private void SetSpeaker(string value)
    {
        if (speakerNameText != null) speakerNameText.text = value;
        if (speakerNameTextTMP != null) speakerNameTextTMP.text = value;
    }

    private void SetQuipLabel(string value)
    {
        if (quipText != null || quipTextTMP != null)
        {
            if (quipText != null) quipText.text = value;
            if (quipTextTMP != null) quipTextTMP.text = value;
        }
        else
        {
            SetDialogue(value); // no separate quip box: reuse the dialogue text
        }
    }

    private void SwitchCamera(int index)
    {
        if (cameraSwitcher == null || index < 0) return;

        try
        {
            cameraSwitcher.SwitchTo(index);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[BiboGuide] Camera switch to index {index} failed ({e.Message}). " +
                             "Check that cameraswap1 has enough cameras in its list.");
        }
    }

    /// <summary>If no dialogue text was assigned, borrow one from inside the dialogue box.</summary>
    private void AutoFindDialogueText()
    {
        if (dialogueText != null || dialogueTextTMP != null || dialogueBox == null) return;

        foreach (var t in dialogueBox.GetComponentsInChildren<Text>(true))
        {
            if (t != speakerNameText) { dialogueText = t; break; }
        }

        if (dialogueText == null)
        {
            foreach (var t in dialogueBox.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t != speakerNameTextTMP) { dialogueTextTMP = t; break; }
            }
        }

        if (dialogueText != null || dialogueTextTMP != null)
            Debug.LogWarning("[BiboGuide] Dialogue Text was not assigned, so one inside the Dialogue Box was picked automatically. " +
                             "Assign it in the Inspector to be sure it is the right one.");
        else
            Debug.LogError("[BiboGuide] Dialogue Text is not assigned and none was found inside the Dialogue Box.");
    }

    // =============================================================
    // PLAYER CONTROL
    // =============================================================

    private bool _warnedNoController;

    /// <summary>If neither controller was assigned, look for the one the player is actually using.</summary>
    private void EnsurePlayerControllers()
    {
        if (thirdPersonController != null || playerScript != null) return;

        if (player != null)
        {
            var tpc = player.GetComponentInChildren<ThirdPersonController>(true);
            var ps = player.GetComponentInChildren<PlayerScript>(true);

            // Prefer whichever one is currently running
            if (tpc != null && tpc.enabled) thirdPersonController = tpc;
            else if (ps != null && ps.enabled) playerScript = ps;
            else if (tpc != null) thirdPersonController = tpc;
            else if (ps != null) playerScript = ps;
        }

        if (thirdPersonController == null && playerScript == null && !_warnedNoController)
        {
            _warnedNoController = true;
            Debug.LogWarning("[BiboGuide] No ThirdPersonController or PlayerScript found, so the player cannot be locked during dialogue.");
        }
    }

    private void SetPlayerControl(bool hasControl)
    {
        EnsurePlayerControllers();

        if (thirdPersonController != null)
            thirdPersonController.enabled = hasControl;

        if (playerScript != null)
            playerScript.SetControl(hasControl);
    }

    // =============================================================
    // DEFAULT LINES
    // =============================================================

    private List<string> GetDefaultGeneralQuips()
    {
        return new List<string>
        {
            "Just checking in! Still alive? Great. I'd hate to lose my only audience.",
            "Fun fact: I fly, you walk. Not rubbing it in. Okay, a little.",
            "Time travel tip: if you meet yourself, don't make eye contact. It's awkward for both of you.",
            "If you accidentally erase yourself from existence, please don't blame the tour guide.",
            "I check on you every thirty seconds. It's in my contract. I wrote the contract.",
            "Whatever you changed back there, I wasn't involved. I have an alibi. And wings.",
            "Paradox City: where yesterday is negotiable and tomorrow is, frankly, a mess.",
            "Cool car, by the way. Does eighty-eight miles an hour sound fast to anyone else? Asking for a friend.",
            "If you hear a loud thud, that's just a paradox. Totally normal. Probably.",
            "My battery is at one hundred percent and my patience is at forty. Keep exploring!",
            "Don't panic if the city looks different. Actually, panic a little. It's good cardio.",
            "History is like a Jenga tower. Pull carefully. Or don't. I'm just a flying robot.",
            "Three time machines, one rule: don't touch anything you don't have to. You'll break it. Everyone does.",
            "I could walk, but where's the drama in that?"
        };
    }

    private List<string> GetDefaultEarlyGameQuips()
    {
        return new List<string>
        {
            "Three time machines and zero trips so far. Procrastination, now available in every century.",
            "The time machines aren't going to use themselves. I checked. I tried. Very awkward.",
            "History is broken and you're window shopping. Bold strategy."
        };
    }

    private List<string> GetDefaultHitlerQuips()
    {
        return new List<string>
        {
            "Somewhere in Vienna, an art student just got a very different report card. I'm sure that's fine. Probably.",
            "Notice anything missing around here? Statues, posters, bad vibes... nope, not a thing. Keep walking."
        };
    }

    private List<string> GetDefaultAppleQuips()
    {
        return new List<string>
        {
            "Have you noticed the market stalls floating? Gravity filed a complaint. I told it to take a number.",
            "Floating furniture. Gravity quit, and honestly, I'd have quit too.",
            "That sleepy scientist is still napping, I hope. Nobody wants a grumpy genius."
        };
    }

    private List<string> GetDefaultBeerQuips()
    {
        return new List<string>
        {
            "Apparently one wrong drink can rewrite a whole city's billboards. Marketing departments hate this one trick.",
            "I'm not saying the tavern switch changed the shop signs, but I'm not not saying it.",
            "Somebody got served the wrong beer and now the whole skyline has opinions."
        };
    }

    private List<DialogueLine> GetDefaultDialogue()
    {
        return new List<DialogueLine>
        {
            new DialogueLine
            {
                text = "Oh good, you're up. Name's BIBO — self-appointed tour guide of Paradox City.",
                cameraIndexForThisLine = 8
            },
            new DialogueLine
            {
                text = "Quick version: history's a mess. Wars that shouldn't have happened, ideas that never got their shot, meetings that went sideways — and it's all still sitting out there, unfixed.",
                cameraIndexForThisLine = 7
            },
            new DialogueLine
            {
                text = "The world's pretty messed up right now. Think you can fix it?",
                cameraIndexForThisLine = 7
            },
            new DialogueLine
            {
                text = "See those three time machines? Each one drops you into a moment that's still waiting to be put right.",
                cameraIndexForThisLine = 7
            },
            new DialogueLine
            {
                text = "That one takes you to an art school entrance exam that's about to go very badly for someone. Worth a look.",
                cameraIndexForThisLine = 3
            },
            new DialogueLine
            {
                text = "That one drops you in an orchard, under a very sleepy, soon-to-be-famous scientist. Don't wake him. Also — catch the apples.",
                cameraIndexForThisLine = 4
            },
            new DialogueLine
            {
                text = "And that one leads to a tavern, right before a meeting that's supposed to change everything. Or doesn't. Depends what's in the glass.",
                cameraIndexForThisLine = 5
            },
            new DialogueLine
            {
                text = "Fix what you can back there. Every change ripples forward — keep an eye on this place when you get back, it won't look the same twice.",
                cameraIndexForThisLine = 7
            },
            new DialogueLine
            {
                text = "Oh, one more thing.",
                cameraIndexForThisLine = 7
            },
            new DialogueLine
            {
                text = "Cool car, by the way.",
                cameraIndexForThisLine = 6
            }
        };
    }
}