using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
{
    public enum MonsterState { Wandering, Investigating, Sweeping }

    [Header("Movement Speeds")]
    public float wanderSpeed = 2.0f;     // Slow, idle pacing
    public float suspicionSpeed = 3.5f;  // Brisk walk to check a small noise
    public float huntSpeed = 7.0f;       // Full sprint to chase a loud noise

    [Header("Wander Settings")]
    public float wanderRadius = 15f;
    public float wanderWaitTime = 2f;

    [Header("Investigation Settings")]
    public float loudNoiseThreshold = 20f; // Any noise larger than 20m triggers a sprint
    public float soundCutoffTimeout = 5f;

    [Header("Area Sweep Settings")]
    public int sweepCount = 3;           // How many points to check before giving up
    public float sweepRadius = 5f;       // How far around the noise origin it searches
    public float sweepWaitTime = 1f;     // How long it pauses at each search point

    private NavMeshAgent agent;
    private MonsterState currentState = MonsterState.Wandering;

    private Vector3 lastHeardPosition;
    private float lastSoundTime;
    private float waitTimer;
    private int sweepsRemaining;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        ReturnToWandering();
    }

    private void Update()
    {
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

    // Now accepts the size of the noise as a second parameter
    public void InvestigateSound(Vector3 soundLocation, float noiseRadius)
    {
        lastHeardPosition = soundLocation;
        lastSoundTime = Time.time;
        currentState = MonsterState.Investigating;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(lastHeardPosition);

            // Speed Escalation
            if (noiseRadius >= loudNoiseThreshold)
            {
                agent.speed = huntSpeed;
            }
            else
            {
                agent.speed = suspicionSpeed;
            }
        }
    }

    // --- IMPROVEMENT 3: Investigate the chest instead of getting amnesia ---
    public void InvestigateHiddenPlayer(Vector3 hideLocation)
    {
        Debug.Log("Player vanished! Sound Monster is searching the area...");

        lastHeardPosition = hideLocation;

        // Instantly transition to sweeping the area around the chest
        currentState = MonsterState.Sweeping;
        sweepsRemaining = sweepCount;
        waitTimer = 0f;

        if (agent != null) agent.speed = suspicionSpeed;

        PickRandomDestination(sweepRadius);
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
        float timeSinceLastSound = Time.time - lastSoundTime;

        if (timeSinceLastSound >= soundCutoffTimeout)
        {
            ReturnToWandering();
            return;
        }

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
                    // Pick the next sweep point
                    PickRandomDestination(sweepRadius);
                    waitTimer = 0f;
                }
                else
                {
                    // Finished sweeping, go back to idle wandering
                    ReturnToWandering();
                }
            }
        }
    }

    private void ReturnToWandering()
    {
        currentState = MonsterState.Wandering;
        waitTimer = 0f;

        // This drops the speed down to the slow wandering pace instantly
        if (agent != null) agent.speed = wanderSpeed;

        PickRandomDestination(wanderRadius);
    }

    private void PickRandomDestination(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;

        // If sweeping, center the search on the last heard position. If wandering, center on the monster itself.
        Vector3 searchCenter = (currentState == MonsterState.Sweeping) ? lastHeardPosition : transform.position;
        randomDirection += searchCenter;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}