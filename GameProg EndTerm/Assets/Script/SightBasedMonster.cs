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

    [Header("Post-Attack Settings")]
    private bool isIgnoringPlayer = false;
    private Coroutine ignoreRoutine;

    // --- Flag to prevent attacking the player when hidden ---
    private bool isPlayerHidden = false;


    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Automatically find the player by tag if not assigned
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) targetPlayer = playerObj.transform;

        ReturnToWandering();
    }

    // --- Call this from the Chest script to instantly reset the monster ---
    public void StopChasingAndWander()
    {
        Debug.Log("Player hid! Vision Monster drops aggro and wanders immediately.");
        isPlayerHidden = true;
        ReturnToWandering();
    }

    // --- Call this from the Chest script when the player exits ---
    public void PlayerExitedChest()
    {
        isPlayerHidden = false;
    }

    // --- NEW: Forces the monster to ignore the player and walk away after an attack ---
    public void LoseInterestForSeconds(float duration)
    {
        if (ignoreRoutine != null) StopCoroutine(ignoreRoutine);
        ignoreRoutine = StartCoroutine(IgnorePlayerRoutine(duration));
    }

    private System.Collections.IEnumerator IgnorePlayerRoutine(float duration)
    {
        Debug.Log("Monster is satisfied and wandering away!");
        isIgnoringPlayer = true;

        ReturnToWandering(); // Instantly drop aggro and pick a random spot to walk to

        yield return new WaitForSeconds(duration);

        isIgnoringPlayer = false;
        Debug.Log("Monster is actively hunting again.");
    }

    private void Update()
    {
        // Check for player vision only if the player is NOT hidden and NOT being ignored
        if (!isPlayerHidden && !isIgnoringPlayer)
        {
            CheckForPlayerVision();
        }

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
        // If we currently see the player (and they aren't hidden/ignored), keep updating destination and sprinting
        if (!isPlayerHidden && !isIgnoringPlayer && targetPlayer != null && Vector3.Distance(transform.position, targetPlayer.position) <= viewRadius)
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
    public void InvestigateHiddenPlayer(Vector3 hideLocation)
    {
        Debug.Log("Player vanished! Monster is searching the area...");

        // Use lastSeenPosition for Vision AI, and lastHeardPosition for Sound AI
        lastSeenPosition = hideLocation; // Change to lastHeardPosition in MonsterAI

        // Instantly transition to sweeping the area around the chest
        currentState = MonsterState.Sweeping;
        sweepsRemaining = sweepCount;
        waitTimer = 0f;

        if (agent != null) agent.speed = suspicionSpeed;

        PickRandomDestination(sweepRadius);
    }
}