using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
{
    public enum MonsterState { Wandering, Investigating, Sweeping }

    [Header("Movement Speeds")]
    public float wanderSpeed = 2.0f;
    public float suspicionSpeed = 3.5f;
    public float huntSpeed = 7.0f;

    [Header("Suspicion Meter Settings")]
    [Tooltip("How much noise it takes to trigger an investigation (e.g., 100)")]
    public float suspicionThreshold = 100f;
    [Tooltip("How fast the monster calms down if it hears nothing (points per second)")]
    public float suspicionDecayRate = 15f;
    [Tooltip("Current suspicion level (visible for debugging)")]
    public float currentSuspicion = 0f;

    [Header("Wander Settings")]
    public float wanderRadius = 15f;
    public float wanderWaitTime = 2f;

    [Header("Investigation Settings")]
    public float loudNoiseThreshold = 20f;
    public float soundCutoffTimeout = 5f;

    [Header("Area Sweep Settings")]
    public int sweepCount = 3;
    public float sweepRadius = 5f;
    public float sweepWaitTime = 1f;

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
        // Slowly drain suspicion over time if the monster is just wandering
        if (currentState == MonsterState.Wandering && currentSuspicion > 0f)
        {
            currentSuspicion -= suspicionDecayRate * Time.deltaTime;
            currentSuspicion = Mathf.Max(0f, currentSuspicion); // Prevent it from going below 0
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

    // --- FIX 1: Redirect 2-parameter calls to the main 3-parameter method ---
    public void InvestigateSound(Vector3 soundLocation, float noiseRadius)
    {
        // Fallback: uses noiseRadius as suspicionAmount if not specified
        InvestigateSound(soundLocation, noiseRadius, noiseRadius);
    }

    // --- MAIN SOUND INVESTIGATION (3 parameters) ---
    public void InvestigateSound(Vector3 soundLocation, float noiseRadius, float suspicionAmount)
    {
        // 1. Add the specific suspicion amount
        currentSuspicion += suspicionAmount;

        // 2. Only investigate if the meter overflows
        if (currentSuspicion >= suspicionThreshold)
        {
            // Cap the suspicion so it doesn't grow infinitely
            currentSuspicion = suspicionThreshold;

            lastHeardPosition = soundLocation;
            lastSoundTime = Time.time;
            currentState = MonsterState.Investigating;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(lastHeardPosition);

                // Speed Escalation based on how far the noise reached
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
    }

    // --- FIX 2: Restored method required by ChestHidingSpot ---
    public void InvestigateHiddenPlayer(Vector3 hideLocation)
    {
        Debug.Log("Player vanished! Sound Monster is searching the area...");

        lastHeardPosition = hideLocation;
        currentSuspicion = suspicionThreshold; // Max out suspicion since it saw them hide

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

        agent.speed = suspicionSpeed;
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

        // Reset the meter so the player has a clean slate after escaping
        currentSuspicion = 0f;

        if (agent != null) agent.speed = wanderSpeed;

        PickRandomDestination(wanderRadius);
    }

    private void PickRandomDestination(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;

        Vector3 searchCenter = (currentState == MonsterState.Sweeping) ? lastHeardPosition : transform.position;
        randomDirection += searchCenter;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}