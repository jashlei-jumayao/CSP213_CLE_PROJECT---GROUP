using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class DrainEnergyOnTouch : MonoBehaviour
{
    [Header("Energy Drain Settings")]
    [Tooltip("Amount of energy drained from RobotEnergy when touching the player.")]
    public float energyDrainAmount = 25f;

    [Header("Audio Settings")]
    [Tooltip("Sound clip played when the player touches this object and loses energy.")]
    public AudioClip drainSound;
    [Range(0f, 1f)]
    public float audioVolume = 1f;
    [Tooltip("0 = 2D full volume anywhere, 1 = 3D positional audio.")]
    [Range(0f, 1f)]
    public float spatialBlend = 0f;

    [Header("Retreat & Despawn Settings")]
    [Tooltip("Root GameObject of this monster (auto-detected from transform.root if left empty).")]
    public GameObject monsterRootObject;
    [Tooltip("Sprint speed while running away from the player.")]
    public float retreatSpeed = 7.5f;
    [Tooltip("How long the monster retreats before disappearing (seconds).")]
    public float retreatDuration = 5.0f;

    private bool hasTriggered = false;
    private AudioSource audioSource;
    private Coroutine retreatRoutine;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = spatialBlend;
        audioSource.ignoreListenerPause = true;
    }

    private void OnEnable()
    {
        hasTriggered = false;
        if (retreatRoutine != null)
        {
            StopCoroutine(retreatRoutine);
            retreatRoutine = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDrainAndRetreat(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDrainAndRetreat(other);
    }

    private void TryDrainAndRetreat(Collider other)
    {
        if (hasTriggered) return;

        Transform rootTransform = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform.root;

        if (rootTransform.CompareTag("Player") || other.CompareTag("Player"))
        {
            // Ignore if the player is safely hiding inside a chest
            PlayerNoiseEmitter noiseScript = rootTransform.GetComponentInChildren<PlayerNoiseEmitter>();
            if (noiseScript != null && !noiseScript.enabled)
            {
                return;
            }

            hasTriggered = true;

            // 1. Drain 25 energy
            RobotEnergy energySystem = rootTransform.GetComponentInChildren<RobotEnergy>();
            if (energySystem != null)
            {
                energySystem.DrainEnergy(energyDrainAmount);
            }

            // 2. Play sound effect
            if (drainSound != null && audioSource != null)
            {
                audioSource.spatialBlend = spatialBlend;
                audioSource.PlayOneShot(drainSound, audioVolume);
            }

            // 3. Retreat for 5 seconds, then disappear
            retreatRoutine = StartCoroutine(RetreatAndDespawnRoutine(rootTransform));
        }
    }

    private IEnumerator RetreatAndDespawnRoutine(Transform playerRoot)
    {
        GameObject monsterObj = monsterRootObject != null ? monsterRootObject : transform.root.gameObject;

        // Pause AI scripts so they don't overwrite the retreat destination with a chase
        MonsterVisionAI visionAI = monsterObj.GetComponentInChildren<MonsterVisionAI>(true);
        bool hadVisionAI = visionAI != null && visionAI.enabled;
        if (hadVisionAI) visionAI.enabled = false;

        MonsterAI blindAI = monsterObj.GetComponentInChildren<MonsterAI>(true);
        bool hadBlindAI = blindAI != null && blindAI.enabled;
        if (hadBlindAI) blindAI.enabled = false;

        NavMeshAgent agent = monsterObj.GetComponentInChildren<NavMeshAgent>(true);

        Vector3 awayDir = monsterObj.transform.position - playerRoot.position;
        awayDir.y = 0f;
        awayDir = awayDir.sqrMagnitude > 0.001f ? awayDir.normalized : -monsterObj.transform.forward;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = retreatSpeed;
            SetRetreatDestination(agent, monsterObj.transform.position, awayDir);
        }
        else
        {
            monsterObj.transform.rotation = Quaternion.LookRotation(awayDir);
        }

        // Run away for the full 5 seconds
        float elapsed = 0f;
        while (elapsed < retreatDuration)
        {
            elapsed += Time.deltaTime;

            awayDir = monsterObj.transform.position - playerRoot.position;
            awayDir.y = 0f;
            if (awayDir.sqrMagnitude > 0.001f) awayDir.Normalize();
            else awayDir = monsterObj.transform.forward;

            if (agent != null && agent.isOnNavMesh)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
                {
                    SetRetreatDestination(agent, monsterObj.transform.position, awayDir);
                }
            }
            else
            {
                monsterObj.transform.position += awayDir * (retreatSpeed * Time.deltaTime);
            }

            yield return null;
        }

        // Re-enable AI scripts before hiding so the next spawn starts in its normal state
        if (visionAI != null && hadVisionAI) visionAI.enabled = true;
        if (blindAI != null && hadBlindAI) blindAI.enabled = true;

        DespawnMonster(playerRoot, monsterObj);
    }

    private void SetRetreatDestination(NavMeshAgent agent, Vector3 origin, Vector3 awayDir)
    {
        Vector3 target = origin + awayDir * 25f;
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 15f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.SetDestination(target);
        }
    }

    private void DespawnMonster(Transform playerRoot, GameObject monsterObj)
    {
        VisionMonsterLightSpawner visionSpawner = playerRoot.GetComponentInChildren<VisionMonsterLightSpawner>(true);
        if (visionSpawner != null && visionSpawner.visionMonster == monsterObj)
        {
            visionSpawner.ForceDespawnVisionMonster();
            return;
        }

        if (monsterObj != null)
        {
            monsterObj.SetActive(false);
        }
    }
}