using UnityEngine;
using UnityEngine.AI;

public class MonsterVisionAI : MonoBehaviour
{
    public enum MonsterState { Wandering, Investigating, Sweeping }

    [Header("Movement Speeds")]
    public float wanderSpeed = 2.0f;     // Slow, idle pacing
    public float suspicionSpeed = 3.5f;  // Brisk walk to investigate last seen spot
    public float huntSpeed = 7.0f;       // Full sprint to chase the player on sight

    [Header("Wander Settings")]
    public float wanderRadius = 15f;
    public float wanderWaitTime = 2f;

    [Header("Vision Settings")]
    public float viewRadius = 15f;          // How far the monster can see
    [Range(0f, 360f)]
    public float viewAngle = 90f;           // Field of view angle (e.g., 90 degrees)
    public LayerMask playerLayer;           // Layer to detect the player
    public LayerMask obstacleLayer;         // Walls/obstacles that block vision
    public float sightLoseTimeout = 5f;     // How long it searches after losing sight

    [Header("Area Sweep Settings")]
    public int sweepCount = 3;           // How many points to check before giving up
    public float sweepRadius = 5f;       // How far around the last seen position it searches
    public float sweepWaitTime = 1f;     // How long it pauses at each search point

    private NavMeshAgent agent;
    private MonsterState currentState = MonsterState.Wandering;

    private Transform targetPlayer;
    private Vector3 lastSeenPosition;
    private float lastSeenTime;
    private float waitTimer;
    private int sweepsRemaining;

    [Header("Slowdown Settings")]
    private bool isMonsterSlowed = false;


    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Automatically find the player by tag if not assigned
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) targetPlayer = playerObj.transform;

        ReturnToWandering();
    }
    

    public void ApplySlowdown(float slowMultiplier, float duration)
    {
        if (!isMonsterSlowed)
        {
            StartCoroutine(MonsterSlowdownRoutine(slowMultiplier, duration));
        }
    }

    private System.Collections.IEnumerator MonsterSlowdownRoutine(float slowMultiplier, float duration)
    {
        isMonsterSlowed = true;

        // Temporarily reduce current agent speed
        if (agent != null)
        {
            agent.speed *= slowMultiplier;
        }

        Debug.Log("Monster slowed down!");

        yield return new WaitForSeconds(duration);

        // Restore speed based on whatever state the monster is currently in
        if (agent != null)
        {
            if (currentState == MonsterState.Wandering) agent.speed = wanderSpeed;
            else if (currentState == MonsterState.Investigating) agent.speed = huntSpeed;
            else if (currentState == MonsterState.Sweeping) agent.speed = suspicionSpeed;
        }

        isMonsterSlowed = false;
        Debug.Log("Monster movement returned to normal.");
    }
    private void Update()
    {
        // Continuously check if the player is in sight
        CheckForPlayerVision();

        switch (currentState)
        {
            case MonsterState.Wandering:
                HandleWandering();
                break;
            case MonsterState.Investigating:
                HandleInvestigating();
                break;
            case MonsterState.Sweeping:
                HandleSweeping();
                break;
        }
    }

    private void CheckForPlayerVision()
    {
        if (targetPlayer == null) return;

        // Check if player is within view radius
        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
        if (distanceToPlayer <= viewRadius)
        {
            Vector3 dirToPlayer = (targetPlayer.position - transform.position).normalized;

            // Check if player is within the view angle field of view
            if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2f)
            {
                // Raycast to ensure no walls are blocking line of sight
                float distanceToTarget = Vector3.Distance(transform.position, targetPlayer.position);
                if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distanceToTarget, obstacleLayer))
                {
                    // Player is spotted!
                    SpotPlayer(targetPlayer.position);
                }
            }
        }
    }

    private void SpotPlayer(Vector3 playerPosition)
    {
        lastSeenPosition = playerPosition;
        lastSeenTime = Time.time;
        currentState = MonsterState.Investigating;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(lastSeenPosition);
            agent.speed = huntSpeed; // Sprint directly at the player/last seen spot
        }
    }

    private void HandleWandering()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= wanderWaitTime)
            {
                PickRandomDestination(wanderRadius);
                waitTimer = 0f;
            }
        }
    }

    private void HandleInvestigating()
    {
        // If we currently see the player, keep updating destination and sprinting
        if (targetPlayer != null && Vector3.Distance(transform.position, targetPlayer.position) <= viewRadius)
        {
            Vector3 dirToPlayer = (targetPlayer.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2f)
            {
                if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, Vector3.Distance(transform.position, targetPlayer.position), obstacleLayer))
                {
                    lastSeenPosition = targetPlayer.position;
                    lastSeenTime = Time.time;
                    agent.SetDestination(lastSeenPosition);
                    agent.speed = huntSpeed;
                    return; // Still actively tracking live vision
                }
            }
        }

        // If line of sight is broken, check timeout
        float timeSinceLostSight = Time.time - lastSeenTime;

        if (timeSinceLostSight >= sightLoseTimeout)
        {
            ReturnToWandering();
            return;
        }

        // When it reaches the last known position, begin Area Sweep
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            StartSweeping();
        }
    }

    private void StartSweeping()
    {
        currentState = MonsterState.Sweeping;
        sweepsRemaining = sweepCount;
        waitTimer = 0f;

        agent.speed = suspicionSpeed; // Slow down to carefully inspect the area
        PickRandomDestination(sweepRadius);
    }

    private void HandleSweeping()
    {
        // If it spots the player mid-sweep, break sweep and hunt immediately
        // (Handled automatically by CheckForPlayerVision running every frame in Update)

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= sweepWaitTime)
            {
                sweepsRemaining--;

                if (sweepsRemaining > 0)
                {
                    PickRandomDestination(sweepRadius);
                    waitTimer = 0f;
                }
                else
                {
                    ReturnToWandering();
                }
            }
        }
    }

    private void ReturnToWandering()
    {
        currentState = MonsterState.Wandering;
        waitTimer = 0f;
        if (agent != null) agent.speed = wanderSpeed;

        PickRandomDestination(wanderRadius);
    }

    private void PickRandomDestination(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        Vector3 searchCenter = (currentState == MonsterState.Sweeping) ? lastSeenPosition : transform.position;
        randomDirection += searchCenter;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    // Optional: Draw debug lines in the Scene view to visualize the monster's vision cone
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 leftRayDirection = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 rightRayDirection = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up, leftRayDirection * viewRadius);
        Gizmos.DrawRay(transform.position + Vector3.up, rightRayDirection * viewRadius);
    }
}