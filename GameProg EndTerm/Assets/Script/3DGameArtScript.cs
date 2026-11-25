using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MonsterChaser : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Leave empty to automatically find the object tagged 'Player' at runtime.")]
    public Transform playerTarget;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float stoppingDistance = 1.2f;
    [Tooltip("How often (in seconds) the path updates toward the player.")]
    public float pathUpdateInterval = 0.1f;

    [Header("Rotation Lock & Orientation")]
    [Tooltip("If checked, the monster's root stays locked at Locked World Rotation instead of turning toward the player.")]
    public bool lockWorldRotation = true;
    [Tooltip("The exact world Euler rotation (X, Y, Z) to hold when Lock World Rotation is enabled.")]
    public Vector3 lockedWorldRotation = Vector3.zero;
    public float turnSpeed = 12f;

    [Header("Child Model Override (Optional)")]
    [Tooltip("Assign the child mesh/armature if an animation is resetting its local rotation back to 0.")]
    public Transform modelTransform;
    public Vector3 lockedModelLocalRotation = Vector3.zero;

    private NavMeshAgent agent;
    private float updateTimer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = false;

        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.freezeRotation = true;
        }
    }

    private void Start()
    {
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
            }
        }

        if (lockWorldRotation)
        {
            transform.rotation = Quaternion.Euler(lockedWorldRotation);
        }
    }

    private void Update()
    {
        if (playerTarget == null || !agent.isOnNavMesh) return;

        agent.speed = moveSpeed;
        updateTimer += Time.deltaTime;

        if (updateTimer >= pathUpdateInterval)
        {
            agent.SetDestination(playerTarget.position);
            updateTimer = 0f;
        }
    }

    // Runs AFTER the Animator evaluates so animation keyframes cannot overwrite the rotation back to 0
    private void LateUpdate()
    {
        if (lockWorldRotation)
        {
            transform.rotation = Quaternion.Euler(lockedWorldRotation);
        }
        else
        {
            Vector3 rawDir = agent.desiredVelocity.sqrMagnitude > 0.05f ? agent.desiredVelocity : agent.velocity;
            Vector3 moveDirection = new Vector3(rawDir.x, 0f, rawDir.z);

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }

        if (modelTransform != null)
        {
            modelTransform.localRotation = Quaternion.Euler(lockedModelLocalRotation);
        }
    }
}