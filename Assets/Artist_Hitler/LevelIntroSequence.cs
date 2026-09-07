using System.Collections;
using TMPro;
using UnityEngine;


public class LevelIntroSequence : MonoBehaviour
{
    [Header("References")]
    public HitlerNPC hitler;
    public MainCameraController mainCamera;
    [Tooltip("Optional - if assigned, player movement is locked for the duration of the intro.")]
    public PlayerScript player;
    [Tooltip("A TextMeshPro UI element used to show the subtitle lines.")]
    public TMP_Text introText;

    [Header("Shot 1 - Following Hitler")]
    [Tooltip("Leave empty to just follow Hitler's own transform.")]
    public Transform hitlerFollowTarget;
    public float hitlerFollowGap = 3f;
    [TextArea]
    public string hitlerLine = "This is the young artist Hitler, I should follow him to his paints.";
    [Tooltip("Total time this shot holds, including the typing time.")]
    public float hitlerShotDuration = 4.5f;

    [Header("Shot 2 - The Paintings")]
    [Tooltip("Drag a good framing point for the paintings here - e.g. one painting's paintingFocusPoint, or a dedicated wide shot Transform.")]
    public Transform paintingFocusPoint;
    public float paintingGap = 2f;
    [TextArea]
    public string paintingLine = "I should correct all of these and return to the future.";
    public float paintingShotDuration = 4.5f;

    [Header("Typing")]
    public float typeSpeed = 0.03f;

    [Header("Debug")]
    public bool debugLogging = true;

    private Transform previousCameraTarget;
    private float previousCameraGap;
    private Coroutine typingCoroutine;

    private void Start()
    {
        if (hitler == null || mainCamera == null)
        {
            Debug.LogError("[LevelIntroSequence] Missing Hitler or MainCameraController reference - skipping intro.");
            return;
        }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.GetComponent<PlayerScript>();
        }

        if (introText != null)
            introText.enabled = false;

        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        if (debugLogging) Debug.Log("[LevelIntroSequence] Intro started.");

        LockPlayer(false);
        TakeCameraControl();

        if (introText != null)
            introText.enabled = true;

       
        hitler.BeginWalkingToEasel();

        yield return PlayShot(
            hitlerFollowTarget != null ? hitlerFollowTarget : hitler.transform,
            hitlerFollowGap, hitlerLine, hitlerShotDuration);

        if (paintingFocusPoint != null)
        {
            yield return PlayShot(paintingFocusPoint, paintingGap, paintingLine, paintingShotDuration);
        }
        else if (debugLogging)
        {
            Debug.LogWarning("[LevelIntroSequence] No paintingFocusPoint assigned - skipping the painting shot.");
        }

        if (introText != null)
            introText.enabled = false;

        ReleaseCameraControl();
        LockPlayer(true);

        if (debugLogging) Debug.Log("[LevelIntroSequence] Intro finished - player back in control.");
    }

    private IEnumerator PlayShot(Transform focus, float gap, string line, float duration)
    {
        mainCamera.target = focus;
        mainCamera.gap = gap;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(line));

        yield return new WaitForSeconds(duration);
    }

    private IEnumerator TypeText(string line)
    {
        if (introText == null)
            yield break;

        introText.text = "";
        foreach (char c in line)
        {
            introText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    private void TakeCameraControl()
    {
        previousCameraTarget = mainCamera.target;
        previousCameraGap = mainCamera.gap;
        mainCamera.inputEnabled = false;
    }

    private void ReleaseCameraControl()
    {
        mainCamera.target = previousCameraTarget;
        mainCamera.gap = previousCameraGap;
        mainCamera.inputEnabled = true;
    }

    private void LockPlayer(bool hasControl)
    {
        if (player != null)
            player.SetControl(hasControl);
    }
}