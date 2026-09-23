using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCLocomotionAnimator : MonoBehaviour
{
    [Tooltip("Animator driving this NPC. Defaults to one found on this object or its children.")]
    [SerializeField] Animator animator;

    [Tooltip("Animator float parameter that blends/switches between idle and walk. " +
             "Change this to match whatever your Animator Controller actually uses.")]
    [SerializeField] string speedParameter = "Speed";

    [Tooltip("Animation playback speed - 1 = normal, 0.5 = half speed, 2 = double speed. " +
             "This slows the animation clips themselves, separate from NavMeshAgent's actual movement speed.")]
    [SerializeField] float animationPlaybackSpeed = 1f;

    NavMeshAgent agent;
    int speedHash;
    bool hasSpeedParameter;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        speedHash = Animator.StringToHash(speedParameter);

        if (animator) animator.speed = animationPlaybackSpeed;

        
        hasSpeedParameter = false;
        if (animator)
        {
            foreach (var param in animator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Float && param.nameHash == speedHash)
                {
                    hasSpeedParameter = true;
                    break;
                }
            }
        }
    }

    void Update()
    {
        if (!animator || !hasSpeedParameter) return;

        
        animator.SetFloat(speedHash, agent.velocity.magnitude);
    }
}