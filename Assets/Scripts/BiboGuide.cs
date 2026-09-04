using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using StarterAssets;

[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 4)]
    public string text;

    [Tooltip("Index in cameraswap1's camera list to switch to when this line starts. Use -1 to leave the camera where it is.")]
    public int cameraIndexForThisLine = -1;
}


[RequireComponent(typeof(NavMeshAgent))]
public class BiboGuide : MonoBehaviour
{
    [Header("Identity")]
    public string npcName = "BIBO";

    [Header("Patrol")]
    [Tooltip("Points BIBO drives between around the Future city while waiting to be talked to")]
    public Transform[] patrolPoints;

    public float patrolPauseDuration = 2f;

    [Header("Interaction")]
    [Tooltip("How close the player needs to be to press E and start talking to BIBO")]
    public float interactRange = 3.5f;

    [Tooltip("Optional: a small UI element (world-space canvas text, or screen overlay) that says 'Press E to Interact'. Shown automatically when the player is in range and not already talking.")]
    public GameObject interactPromptUI;

    [Header("Player Control Lock - assign ONE of these two, matching whichever your Future scene actually uses")]
    public ThirdPersonController thirdPersonController;
    public PlayerScript playerScript;

    [Header("Model Orientation Fix")]
    [Tooltip("Drag the actual 3D model child here. Leave the BIBO root upright.")]
    public Transform model;

    [Tooltip("Fixed rotation correction for the model only.")]
    public Vector3 modelRotationOffset = new Vector3(-104.13f, 0f, 0f);

    [Tooltip("Degrees per second BIBO turns to face a new direction while patrolling")]
    public float turnSpeed = 360f;

    [Header("Height")]
    [Tooltip("Raises BIBO above the NavMesh. Try values such as 0.5 or 1.")]
    public float heightOffset = 0f;

    [Header("Stability")]
    [Tooltip("If true, the root transform's X and Z rotation are forced to 0 every frame, no matter what physics/NavMesh does. Turn this on if BIBO ever tips onto its side.")]
    public bool forceUprightEveryFrame = true;

    [Header("Camera")]
    [Tooltip("Drag the scene's cameraswap1 object here")]
    public cameraswap1 cameraSwitcher;

    [Tooltip("Index of the normal player camera - restored once the dialogue ends")]
    public int normalCameraIndex = 0;

    [Header("Dialogue UI (build a simple Canvas > Panel > Text and assign both here)")]
    public GameObject dialogueBox;
    public Text dialogueText;
    public Text speakerNameText;

    [Tooltip("Seconds between each typed character")]
    public float typeSpeed = 0.02f;

    [Tooltip("Leave empty to use the built-in default line-up (Vienna / Apple Forest / Wrong Beer / DeLorean)")]
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();

    [Header("Debug")]
    public bool debugLogging = true;

    [Tooltip("How often (seconds) to print the range/status debug line while debugLogging is on. Set to 0 to disable the periodic print (E-press logs still happen).")]
    public float debugPrintInterval = 0.5f;

    private float debugTimer;

    private NavMeshAgent agent;
    private Rigidbody rb; 
    private Transform player;
    private InteractIndicator indicator;

    private int patrolIndex = -1;
    private float patrolTimer;

    private bool inDialogue;
    private int currentLineIndex;
    private bool isTyping;
    private Coroutine typingCoroutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        
        agent.updateRotation = false;

        
        agent.updateUpAxis = false;

        
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        
        transform.localRotation = Quaternion.Euler(
            0f,
            transform.localEulerAngles.y,
            0f
        );

        
        Vector3 position = transform.position;
        position.y += heightOffset;
        transform.position = position;

        
        if (model != null)
        {
            model.localRotation = Quaternion.Euler(modelRotationOffset);
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
        {
            player = p.transform;
        }
        else if (thirdPersonController != null)
        {
            
            player = thirdPersonController.transform;

            if (debugLogging)
                Debug.LogWarning("[BiboGuide] No GameObject tagged 'Player' found - " +
                                  "falling back to the assigned ThirdPersonController's transform instead. " +
                                  "Recommended fix: tag your player GameObject as 'Player' in the Inspector.");
        }
        else if (playerScript != null)
        {
            player = playerScript.transform;

            if (debugLogging)
                Debug.LogWarning("[BiboGuide] No GameObject tagged 'Player' found - " +
                                  "falling back to the assigned PlayerScript's transform instead. " +
                                  "Recommended fix: tag your player GameObject as 'Player' in the Inspector.");
        }
        else if (debugLogging)
        {
            Debug.LogWarning("[BiboGuide] No GameObject tagged 'Player' found, and neither " +
                              "thirdPersonController nor playerScript is assigned in the Inspector. " +
                              "BIBO has no way to find the player - tag your player GameObject as 'Player', " +
                              "or drag it into one of those two fields.");
        }

        
        GameObject indicatorObj = new GameObject($"{npcName}_ExclamationMark");
        indicatorObj.transform.SetParent(transform);

        indicator = indicatorObj.AddComponent<InteractIndicator>();
        indicator.Init(transform);

        if (dialogueBox != null)
            dialogueBox.SetActive(false);

        if (speakerNameText != null)
            speakerNameText.text = npcName;

        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);

        if (dialogueLines == null || dialogueLines.Count == 0)
        {
            dialogueLines = GetDefaultDialogue();
        }
    }

    private void Start()
    {
        GoToNextPatrolPoint();
    }

    private void Update()
    {
        
        if (debugLogging && Input.GetKeyDown(KeyCode.E))
        {
            float distNow = player != null ? Vector3.Distance(player.position, transform.position) : -1f;
            Debug.Log($"[BiboGuide][E PRESSED] inDialogue={inDialogue} playerFound={(player != null)} " +
                      $"distance={distNow:F2} interactRange={interactRange} inRange={PlayerInRange()}");
        }

        if (inDialogue)
        {
            HandleDialogueInput();
            return;
        }

        Patrol();

        bool playerNearby = PlayerInRange();

        UpdateInteractPrompt(playerNearby);

       
        if (debugLogging && debugPrintInterval > 0f)
        {
            debugTimer += Time.deltaTime;
            if (debugTimer >= debugPrintInterval)
            {
                debugTimer = 0f;

                if (player == null)
                {
                    Debug.LogWarning("[BiboGuide][STATUS] player reference is NULL - " +
                                      "no GameObject tagged 'Player' was found at Awake(). " +
                                      "PlayerInRange() will always return false until this is fixed.");
                }
                else
                {
                    float dist = Vector3.Distance(player.position, transform.position);
                    Debug.Log($"[BiboGuide][STATUS] distance={dist:F2} / interactRange={interactRange} " +
                              $"-> inRange={playerNearby}");
                }
            }
        }

        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (debugLogging)
                Debug.Log("[BiboGuide] Conditions met - calling StartDialogue().");

            StartDialogue();
        }
    }

    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }

    private void LateUpdate()
    {
        if (!forceUprightEveryFrame)
            return;

        
        Vector3 euler = transform.eulerAngles;
        if (Mathf.Abs(euler.x) > 0.01f || Mathf.Abs(euler.z) > 0.01f)
        {
            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
        }

        
        if (model != null)
        {
            model.localRotation = Quaternion.Euler(modelRotationOffset);
        }
    }

    // ---------------- Interact Prompt ----------------

    private void UpdateInteractPrompt(bool playerNearby)
    {
        if (interactPromptUI == null)
            return;

        bool shouldShow = playerNearby && !inDialogue;

        if (interactPromptUI.activeSelf != shouldShow)
            interactPromptUI.SetActive(shouldShow);
    }

    // ---------------- Patrol ----------------

    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        
        if (agent.velocity.sqrMagnitude > 0.05f)
        {
            FaceDirection(agent.velocity, turnSpeed);
        }

        if (agent.pathPending)
            return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            patrolTimer += Time.deltaTime;

            if (patrolTimer >= patrolPauseDuration)
            {
                GoToNextPatrolPoint();
            }
        }
    }

    
    private void FaceDirection(Vector3 direction, float turnSpeedDegPerSec)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction.normalized, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnSpeedDegPerSec * Time.deltaTime
        );

        
        if (model != null)
        {
            model.localRotation = Quaternion.Euler(modelRotationOffset);
        }
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;

        agent.SetDestination(patrolPoints[patrolIndex].position);

        patrolTimer = 0f;
    }

    private bool PlayerInRange()
    {
        if (player == null)
            return false;

        return Vector3.Distance(
            player.position,
            transform.position
        ) <= interactRange;
    }

    // ---------------- Dialogue ----------------

    private void StartDialogue()
    {
        inDialogue = true;
        currentLineIndex = -1;

        agent.isStopped = true;

        SetPlayerControl(false);

        indicator.SetVisible(false);

        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);

        if (dialogueBox != null)
            dialogueBox.SetActive(true);

        if (player != null)
        {
            Vector3 toPlayer = player.position - transform.position;

            
            FaceDirection(toPlayer, 999f);
        }

        if (debugLogging)
            Debug.Log("[BiboGuide] Dialogue started.");

        AdvanceDialogue();
    }

    private void HandleDialogueInput()
    {
        if (!Input.GetKeyDown(KeyCode.E))
            return;

        if (isTyping)
        {
            
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            if (dialogueText != null)
                dialogueText.text = dialogueLines[currentLineIndex].text;

            isTyping = false;
        }
        else
        {
            
            AdvanceDialogue();
        }
    }

    private void AdvanceDialogue()
    {
        currentLineIndex++;

        if (currentLineIndex >= dialogueLines.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = dialogueLines[currentLineIndex];

        if (line.cameraIndexForThisLine >= 0 && cameraSwitcher != null)
        {
            cameraSwitcher.SwitchTo(line.cameraIndexForThisLine);
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine(line.text));
    }

    private IEnumerator TypeLine(string text)
    {
        isTyping = true;

        if (dialogueText != null)
            dialogueText.text = "";

        foreach (char c in text)
        {
            if (dialogueText != null)
                dialogueText.text += c;

            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
    }

    private void EndDialogue()
    {
        inDialogue = false;

        if (dialogueBox != null)
            dialogueBox.SetActive(false);

        if (cameraSwitcher != null)
            cameraSwitcher.SwitchTo(normalCameraIndex);

        SetPlayerControl(true);

        agent.isStopped = false;

        indicator.SetVisible(true);

        if (debugLogging)
            Debug.Log("[BiboGuide] Dialogue ended.");
    }

    private void SetPlayerControl(bool hasControl)
    {
        if (thirdPersonController != null)
            thirdPersonController.enabled = hasControl;

        if (playerScript != null)
            playerScript.SetControl(hasControl);
    }

    // ---------------- Default dialogue ----------------

    private List<DialogueLine> GetDefaultDialogue()
    {
        return new List<DialogueLine>
        {
            new DialogueLine
            {
                text = "Oh good, you're up. Name's BIBO — self-appointed tour guide of Paradox City.",
                cameraIndexForThisLine = -1
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