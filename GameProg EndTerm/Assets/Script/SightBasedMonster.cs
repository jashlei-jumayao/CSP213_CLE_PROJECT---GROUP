using UnityEngine;
using UnityEngine.AI;

public class MonsterVisionAI : MonoBehaviour
{
    public enum MonsterState { Wandering, Investigating, Sweeping, Retreating }

    [Header("Movement Speeds")]
    public float wanderSpeed = 2.0f;     // Slow, idle pacing
    public float suspicionSpeed = 3.5f;  // Brisk walk to investigate last seen spot
    public float huntSpeed = 7.0f;       // Full sprint to chase the player on sight
    public float retreatSpeed = 7.0f;    // Sprint speed when fleeing after 7 flashes

    [Header("Flashlight Banish Mechanic")]
    [Tooltip("Number of direct flashlight flashes needed to make the monster turn back and despawn.")]
    public int flashesToBanish = 7;
    [Tooltip("Key used to toggle/flash the flashlight.")]
    public KeyCode flashlightKey = KeyCode.F;
    [Tooltip("How wide the flashlight beam cone is when checking if the player aimed at the monster.")]
    public float flashBeamAngle = 35f;
    [Tooltip("Maximum distance at which flashing the light hits the monster.")]
    public float maxFlashDistance = 35f;
    [Tooltip("Minimum delay between counted flashes so a single press isn't double-counted.")]
    public float flashHitCooldown = 0.25f;
    [Tooltip("How long the monster runs away before despawning (despawns sooner if out of sight).")]
    public float retreatDuration = 3.5f;

    [Header("Flash Banish Audio (Optional)")]
    public AudioClip flashFlinchSound;
    public AudioClip banishRetreatSound;
    [Range(0f, 1f)]
    public float flashAudioVolume = 1f;

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
    private AudioSource audioSource;
    private MonsterState currentState = MonsterState.Wandering;

    private Transform targetPlayer;
    private FlashlightController playerFlashlight;
    private VisionMonsterLightSpawner lightSpawner;

    private Vector3 lastSeenPosition;
    private float lastSeenTime;
    private float waitTimer;
    private int sweepsRemaining;

    private int currentFlashHits = 0;
    private float lastFlashHitTime = -999f;
    private float retreatTimer = 0f;

    [Header("Post-Attack Settings")]
    private bool isIgnoringPlayer = false;
    private Coroutine ignoreRoutine;

    // --- Flag to prevent attacking the player when hidden ---
    private bool isPlayerHidden = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }

    private void OnEnable()
    {
        currentFlashHits = 0;
        lastFlashHitTime = -999f;
        retreatTimer = 0f;
        isPlayerHidden = false;
        isIgnoringPlayer = false;

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        CachePlayerReferences();
        ReturnToWandering();
    }

    private void Start()
    {
        CachePlayerReferences();
    }

    private void CachePlayerReferences()
    {
        if (targetPlayer == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) targetPlayer = playerObj.transform.root;
        }

        if (targetPlayer != null)
        {
            if (playerFlashlight == null)
                playerFlashlight = targetPlayer.GetComponentInChildren<FlashlightController>(true);
            if (lightSpawner == null)
                lightSpawner = targetPlayer.GetComponentInChildren<VisionMonsterLightSpawner>(true);
        }
    }

    // --- Call this from the Chest script to instantly reset the monster ---
    public void StopChasingAndWander()
    {
        Debug.Log("Player hid! Vision Monster drops aggro and wanders immediately.");
        isPlayerHidden = true;
        if (currentState != MonsterState.Retreating)
        {
            ReturnToWandering();
        }
    }

    // --- Call this from the Chest script when the player exits ---
    public void PlayerExitedChest()
    {
        isPlayerHidden = false;
    }

    // --- Forces the monster to ignore the player and walk away after an attack ---
    public void LoseInterestForSeconds(float duration)
    {
        if (currentState == MonsterState.Retreating) return;
        if (ignoreRoutine != null) StopCoroutine(ignoreRoutine);
        ignoreRoutine = StartCoroutine(IgnorePlayerRoutine(duration));
    }

    private System.Collections.IEnumerator IgnorePlayerRoutine(float duration)
    {
        Debug.Log("Monster is satisfied and wandering away!");
        isIgnoringPlayer = true;

        ReturnToWandering();

        yield return new WaitForSeconds(duration);

        isIgnoringPlayer = false;
        Debug.Log("Monster is actively hunting again.");
    }

    private void Update()
    {
        if (currentState == MonsterState.Retreating)
        {
            HandleRetreating();
            return;
        }

        CheckForFlashlightHit();

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

    private void CheckForFlashlightHit()
    {
        if (isPlayerHidden || Time.timeScale == 0f) return;
        if (!Input.GetKeyDown(flashlightKey)) return;
        if (Time.time - lastFlashHitTime < flashHitCooldown) return;

        CachePlayerReferences();

        // Only count the press if the flashlight actually turned ON (has energy & isn't already on)
        if (playerFlashlight == null || !playerFlashlight.IsFlashlightOn) return;

        Transform beamOrigin = playerFlashlight.robotFlashlight != null
            ? playerFlashlight.robotFlashlight.transform
            : (Camera.main != null ? Camera.main.transform : null);

        if (beamOrigin == null) return;

        Vector3 monsterTargetPoint = transform.position + Vector3.up * 1.2f;
        Vector3 toMonster = monsterTargetPoint - beamOrigin.position;
        float distance = toMonster.magnitude;

        if (distance > maxFlashDistance) return;

        float beamAngle = playerFlashlight.robotFlashlight != null
            ? Mathf.Max(flashBeamAngle, playerFlashlight.robotFlashlight.spotAngle * 0.6f)
            : flashBeamAngle;

        if (Vector3.Angle(beamOrigin.forward, toMonster.normalized) > beamAngle) return;

        // Ensure no walls or obstacles block the flashlight beam
        if (Physics.Raycast(beamOrigin.position, toMonster.normalized, distance, obstacleLayer)) return;

        RegisterFlashHit();
    }

    private void RegisterFlashHit()
    {
        lastFlashHitTime = Time.time;
        currentFlashHits++;

        Debug.Log($"Vision Monster flashed! ({currentFlashHits}/{flashesToBanish})");

        if (currentFlashHits >= flashesToBanish)
        {
            StartRetreatAndDespawn();
        }
        else if (flashFlinchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(flashFlinchSound, flashAudioVolume);
        }
    }

    private void StartRetreatAndDespawn()
    {
        currentState = MonsterState.Retreating;
        retreatTimer = 0f;

        if (banishRetreatSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(banishRetreatSound, flashAudioVolume);
        }

        // Pick a destination directly away from the player
        Vector3 awayDir = targetPlayer != null
            ? (transform.position - targetPlayer.position).normalized
            : -transform.forward;
        awayDir.y = 0f;
        if (awayDir.sqrMagnitude < 0.01f) awayDir = -transform.forward;

        Vector3 retreatTarget = transform.position + awayDir * 25f;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = retreatSpeed;
            if (NavMesh.SamplePosition(retreatTarget, out NavMeshHit hit, 15f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                agent.SetDestination(retreatTarget);
            }
        }
        else
        {
            transform.rotation = Quaternion.LookRotation(awayDir);
        }
    }

    private void HandleRetreating()
    {
        retreatTimer += Time.deltaTime;

        // Keep running away if the agent already reached its first retreat point
        if (agent != null && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            Vector3 awayDir = targetPlayer != null
                ? (transform.position - targetPlayer.position).normalized
                : transform.forward;
            awayDir.y = 0f;
            if (NavMesh.SamplePosition(transform.position + awayDir * 20f, out NavMeshHit hit, 15f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }

        // Despawn once it has turned and retreated for a few seconds (or sooner if out of player's camera view)
        bool isVisible = IsVisibleToMainCamera();
        if ((retreatTimer >= 1.25f && !isVisible) || retreatTimer >= retreatDuration)
        {
            DespawnSelf();
        }
    }

    private bool IsVisibleToMainCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Vector3 vp = cam.WorldToViewportPoint(transform.position + Vector3.up * 1.2f);
        if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) return false;

        Vector3 toMonster = (transform.position + Vector3.up * 1.2f) - cam.transform.position;
        if (Physics.Raycast(cam.transform.position, toMonster.normalized, toMonster.magnitude, obstacleLayer))
        {
            return false;
        }

        return true;
    }

    private void DespawnSelf()
    {
        currentFlashHits = 0;
        currentState = MonsterState.Wandering;

        if (lightSpawner != null)
        {
            lightSpawner.ForceDespawnVisionMonster();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void CheckForPlayerVision()
    {
        if (targetPlayer == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
        if (distanceToPlayer <= viewRadius)
        {
            Vector3 dirToPlayer = (targetPlayer.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2f)
            {
                float distanceToTarget = Vector3.Distance(transform.position, targetPlayer.position);
                if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distanceToTarget, obstacleLayer))
                {
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
            agent.speed = huntSpeed;
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
                    return;
                }
            }
        }

        float timeSinceLostSight = Time.time - lastSeenTime;

        if (timeSinceLostSight >= sightLoseTimeout)
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
        if (agent != null) agent.speed = wanderSpeed;

        PickRandomDestination(wanderRadius);
    }

    private void PickRandomDestination(float radius)
    {
        if (agent == null || !agent.isOnNavMesh) return;

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
        if (currentState == MonsterState.Retreating) return;

        Debug.Log("Player vanished! Monster is searching the area...");

        lastSeenPosition = hideLocation;

        currentState = MonsterState.Sweeping;
        sweepsRemaining = sweepCount;
        waitTimer = 0f;

        if (agent != null) agent.speed = suspicionSpeed;

        PickRandomDestination(sweepRadius);
    }
}