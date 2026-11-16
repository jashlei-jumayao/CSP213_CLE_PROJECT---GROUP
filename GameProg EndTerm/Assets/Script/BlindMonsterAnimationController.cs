using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BlindMonsterAnimationController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to automatically find the Animator on this object or its children.")]
    public Animator anim;

    [Header("Animation Settings")]
    [Tooltip("Smooths out transitions between Walk and Run in the Blend Tree.")]
    public float speedDampTime = 0.1f;

    private NavMeshAgent agent;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (anim == null)
        {
            anim = GetComponent<Animator>();
            if (anim == null) anim = GetComponentInChildren<Animator>();
        }

        if (anim != null)
        {
            // Prevent root motion from overriding NavMeshAgent movement
            anim.applyRootMotion = false;
        }
        else
        {
            Debug.LogError($"[{name}] BlindMonsterAnimationController could not find an Animator on this object or its children!");
        }
    }

    private void Update()
    {
        if (anim == null || agent == null) return;

        // Measure horizontal speed so slope/step changes don't spike the Blend Tree
        Vector3 horizontalVelocity = new Vector3(agent.velocity.x, 0f, agent.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        anim.SetFloat(SpeedHash, currentSpeed, speedDampTime, Time.deltaTime);
    }
}