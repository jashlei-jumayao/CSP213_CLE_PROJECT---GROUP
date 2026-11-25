using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MonsterAI : MonoBehaviour
{
    public enum MonsterState { Wandering, Investigating, Sweeping }

    [Header("Visual Orientation Fix")]
    [Tooltip("Assign the child Armature object (the one with -90 on X). Leave empty to auto-grab the first child.")]
    public Transform modelTransform;
    [Tooltip("90 fixes walking to the right; use -90 if it faces the opposite side.")]
    public float modelYRotationOffset = 90f;
    [Tooltip("How fast the monster turns in degrees per second (lower = heavier, smoother turns).")]
    public float turnSpeed = 180f;

    [Header("Movement Speeds (Match Blend Tree Thresholds)")]
    [Tooltip("Matches Armature|Monster Walk threshold (2.0)")]
    public float wanderSpeed = 2.0f;
    public float suspicionSpeed = 3.5f;
    [Tooltip("Matches Armature|Monster Run threshold (7.0)")]
    public float huntSpeed = 7.0f;

    [Header("Continuous Movement Settings")]
    [Tooltip("How close to a waypoint before immediately picking the next one so it never slows down.")]
    public float waypointSwitchDistance = 0.75f;
    [Tooltip("If the monster's speed drops below this for too long, pick a new path immediately.")]
    public float stuckTimeout = 0.5f;

    [Header("Suspicion Meter Settings")]
    [Tooltip("How much noise it takes to trigger an investigation (e.g., 100)")]
    public float suspicionThreshold = 100f;
    [Tooltip("How fast the monster calms down if it hears nothing (points per second)")]
    public float suspicionDecayRate = 15f;
    [Tooltip("Current suspicion level (visible for debugging)")]
    public float currentSuspicion = 0f;

    [Header("Aggression Audio")]
    [Tooltip("Sound played when the suspicion meter reaches full and the monster locks onto a sound.")]
    public AudioClip aggressionSound;
    [Range(0f, 1f)]
    public float aggressionVolume = 1f;
    [Tooltip("Minimum seconds before the aggression roar can trigger again.")]
    public float aggressionSoundCooldown = 8f;
    public float minAudioDistance = 8f;
    public float maxAudioDistance = 65f;

    [Header("Wander Settings")]
    public float wanderRadius = 15f;

    [Header("Investigation Settings")]
    public float loudNoiseThreshold = 20f;
    public float soundCutoffTimeout = 5f;

    [Header("Area Sweep Settings")]
    public int sweepCount = 3;
    public float sweepRadius = 5f;

    private NavMeshAgent agent;
    private AudioSource audioSource;
    private MonsterState currentState = MonsterState.Wandering;
    private Quaternion initialModelLocalRotation;

    private Vector3 lastHeardPosition;
    private float lastSoundTime;
    private float lastAggressionSoundTime = -999f;
    private float stuckTimer = 0f;
    private int sweepsRemaining;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.autoBraking = false;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = minAudioDistance;
        audioSource.maxDistance = maxAudioDistance;

        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.freezeRotation = true;
        }

        if (modelTransform == null && transform.childCount > 0)
        {
            modelTransform = transform.GetChild(0);
        }

        if (modelTransform != null)
        {
            initialModelLocalRotation = modelTransform.localRotation;
        }
    }

    private void OnEnable()
    {
        lastAggressionSoundTime = -999f;
        stuckTimer = 0f;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
        ReturnToWandering();
    }

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (agent.isStopped)
        {
            agent.isStopped = false;
        }

        if (currentState == MonsterState.Wandering && currentSuspicion > 0f)
        {
            currentSuspicion -= suspicionDecayRate * Time.deltaTime;
            currentSuspicion = Mathf.Max(0f, currentSuspicion);
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

        EnsureNeverStopped();
    }

    private void LateUpdate()
    {
        RotateTowardMovement();
    }

    private void EnsureNeverStopped()
    {
        if (agent.pathPending) return;

        if (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            PickFallbackDestination();
            return;
        }

        if (agent.velocity.sqrMagnitude < 0.05f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTimeout)
            {
                stuckTimer = 0f;
                PickFallbackDestination();
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    private void PickFallbackDestination()
    {
        if (currentState == MonsterState.Sweeping)
        {
            PickRandomDestination(sweepRadius);
        }
        else
        {
            currentState = MonsterState.Wandering;
            agent.speed = wanderSpeed;
            PickRandomDestination(wanderRadius);
        }
    }

    private bool HasReachedWaypoint()
    {
        if (agent.pathPending) return false;
        if (!agent.hasPath) return true;

        float threshold = Mathf.Max(agent.stoppingDistance, waypointSwitchDistance);
        return agent.remainingDistance <= threshold;
    }

    private void RotateTowardMovement()
    {
        Vector3 rawDir = agent.velocity.sqrMagnitude > 0.15f ? agent.velocity : agent.desiredVelocity;
        Vector3 moveDirection = new Vector3(rawDir.x, 0f, rawDir.z);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (modelTransform != null)
        {
            modelTransform.localRotation = Quaternion.Euler(0f, modelYRotationOffset, 0f) * initialModelLocalRotation;
        }
    }

    public void SetInitialWanderNear(Vector3 targetArea, float radius)
    {
        if (agent == null || !agent.isOnNavMesh) return;

        for (int i = 0; i < 10; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * radius;
            if (NavMesh.SamplePosition(targetArea + randomOffset, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                agent.speed = wanderSpeed;
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    public void InvestigateSound(Vector3 soundLocation, float noiseRadius)
    {
        InvestigateSound(soundLocation, noiseRadius, noiseRadius);
    }

    public void InvestigateSound(Vector3 soundLocation, float noiseRadius, float suspicionAmount)
    {
        bool wasBelowThreshold = currentSuspicion < suspicionThreshold;
        currentSuspicion += suspicionAmount;

        if (currentSuspicion >= suspicionThreshold)
        {
            currentSuspicion = suspicionThreshold;

            if (wasBelowThreshold || currentState == MonsterState.Wandering)
            {
                TryPlayAggressionSound();
            }

            lastHeardPosition = soundLocation;
            lastSoundTime = Time.time;
            currentState = MonsterState.Investigating;

            if (agent != null && agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(lastHeardPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
                else
                {
                    agent.SetDestination(lastHeardPosition);
                }

                agent.speed = (noiseRadius >= loudNoiseThreshold) ? huntSpeed : suspicionSpeed;
            }
        }
    }

    private void TryPlayAggressionSound()
    {
        if (aggressionSound == null || audioSource == null) return;
        if (Time.time - lastAggressionSoundTime < aggressionSoundCooldown) return;

        audioSource.minDistance = minAudioDistance;
        audioSource.maxDistance = maxAudioDistance;
        audioSource.PlayOneShot(aggressionSound, aggressionVolume);
        lastAggressionSoundTime = Time.time;
    }

    public void InvestigateHiddenPlayer(Vector3 hideLocation)
    {
        lastHeardPosition = hideLocation;
        currentSuspicion = suspicionThreshold;

        currentState = MonsterState.Sweeping;
        sweepsRemaining = sweepCount;

        if (agent != null) agent.speed = suspicionSpeed;

        PickRandomDestination(sweepRadius);
    }

    private void HandleWandering()
    {
        if (HasReachedWaypoint())
        {
            PickRandomDestination(wanderRadius);
        }
    }

    private void HandleInvestigating()
    {
        if (Time.time - lastSoundTime >= soundCutoffTimeout)
        {
            ReturnToWandering();
            return;
        }

        if (HasReachedWaypoint())
        {
            StartSweeping();
        }
    }

    private void StartSweeping()
    {
        currentState = MonsterState.Sweeping;
        sweepsRemaining = sweepCount;

        if (agent != null) agent.speed = suspicionSpeed;
        PickRandomDestination(sweepRadius);
    }

    private void HandleSweeping()
    {
        if (HasReachedWaypoint())
        {
            sweepsRemaining--;

            if (sweepsRemaining > 0)
            {
                PickRandomDestination(sweepRadius);
            }
            else
            {
                ReturnToWandering();
            }
        }
    }

    private void ReturnToWandering()
    {
        currentState = MonsterState.Wandering;
        currentSuspicion = 0f;

        if (agent != null)
        {
            agent.speed = wanderSpeed;
            if (agent.isOnNavMesh)
            {
                PickRandomDestination(wanderRadius);
            }
        }
    }

    private void PickRandomDestination(float radius)
    {
        if (agent == null || !agent.isOnNavMesh) return;

        Vector3 searchCenter = (currentState == MonsterState.Sweeping) ? lastHeardPosition : transform.position;

        for (int i = 0; i < 12; i++)
        {
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(radius * 0.4f, radius);
            Vector3 candidate = searchCenter + new Vector3(circle.x, 0f, circle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                if ((hit.position - transform.position).sqrMagnitude > 1.5f)
                {
                    agent.SetDestination(hit.position);
                    return;
                }
            }
        }

        if (NavMesh.SamplePosition(transform.position + Random.insideUnitSphere * (wanderRadius * 2f), out NavMeshHit fallbackHit, wanderRadius * 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(fallbackHit.position);
        }
    }
}