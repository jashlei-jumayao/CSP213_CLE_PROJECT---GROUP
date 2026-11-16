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
    [Tooltip("How fast the monster turns to face its movement direction.")]
    public float turnSpeed = 12f;

    [Header("Movement Speeds (Match Blend Tree Thresholds)")]
    [Tooltip("Matches Armature|Monster Walk threshold (2.0)")]
    public float wanderSpeed = 2.0f;
    public float suspicionSpeed = 3.5f;
    [Tooltip("Matches Armature|Monster Run threshold (7.0)")]
    public float huntSpeed = 7.0f;

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
    public float wanderWaitTime = 2f;

    [Header("Investigation Settings")]
    public float loudNoiseThreshold = 20f;
    public float soundCutoffTimeout = 5f;

    [Header("Area Sweep Settings")]
    public int sweepCount = 3;
    public float sweepRadius = 5f;
    public float sweepWaitTime = 1f;

    private NavMeshAgent agent;
    private AudioSource audioSource;
    private MonsterState currentState = MonsterState.Wandering;
    private Quaternion initialModelLocalRotation;

    private Vector3 lastHeardPosition;
    private float lastSoundTime;
    private float lastAggressionSoundTime = -999f;
    private float waitTimer;
    private int sweepsRemaining;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;

        // Reuse existing AudioSource or create a 3D one on the monster
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
        ReturnToWandering();
    }

    private void Update()
    {
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
    }

    private void LateUpdate()
    {
        RotateTowardMovement();
    }

    private void RotateTowardMovement()
    {
        Vector3 rawDir = agent.desiredVelocity.sqrMagnitude > 0.05f ? agent.desiredVelocity : agent.velocity;
        Vector3 moveDirection = new Vector3(rawDir.x, 0f, rawDir.z);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (modelTransform != null)
        {
            modelTransform.localRotation = Quaternion.Euler(0f, modelYRotationOffset, 0f) * initialModelLocalRotation;
        }
    }

    public void SetInitialWanderNear(Vector3 targetArea, float radius)
    {
        if (agent == null || !agent.isOnNavMesh) return;

        Vector3 randomOffset = Random.insideUnitSphere * radius;
        if (NavMesh.SamplePosition(targetArea + randomOffset, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            agent.speed = wanderSpeed;
            agent.SetDestination(hit.position);
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

            // Play the aggression sound when the meter first fills or when transitioning from Wandering
            if (wasBelowThreshold || currentState == MonsterState.Wandering)
            {
                TryPlayAggressionSound();
            }

            lastHeardPosition = soundLocation;
            lastSoundTime = Time.time;
            currentState = MonsterState.Investigating;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(lastHeardPosition);
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
        if (Time.time - lastSoundTime >= soundCutoffTimeout)
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

        if (agent != null) agent.speed = suspicionSpeed;
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

        Vector3 randomDirection = Random.insideUnitSphere * radius;
        Vector3 searchCenter = (currentState == MonsterState.Sweeping) ? lastHeardPosition : transform.position;
        randomDirection += searchCenter;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}