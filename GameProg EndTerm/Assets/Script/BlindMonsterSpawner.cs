using UnityEngine;
using UnityEngine.AI;

public class BlindMonsterNoiseSpawner : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    [Tooltip("Assign your single Blind Monster scene object (recommended) or prefab.")]
    public GameObject blindMonster;

    [Header("Noise Threshold Settings")]
    [Tooltip("Total accumulated noise needed from PlayerNoiseEmitter to summon the blind monster.")]
    public float spawnNoiseThreshold = 250f;
    [Tooltip("Slowly decays built-up noise per second when sitting completely still (0 = never decays).")]
    public float idleNoiseDecayRate = 0f;
    [Tooltip("Read-only view of current noise buildup.")]
    public float currentAccumulatedNoise = 0f;

    [Header("Spawn & Wander Proximity")]
    public float minSpawnDistance = 30f;
    public float maxSpawnDistance = 50f;
    public float maxActiveDistance = 85f;
    public float activeDuration = 60f;

    [Header("3D Arrival & Departure Sounds")]
    [Tooltip("Sound played at the monster's spawn point so the player hears it arrive.")]
    public AudioClip arrivalSound;
    [Range(0f, 1f)]
    public float arrivalVolume = 1f;

    [Tooltip("Sound played at the monster's position when it despawns/leaves the area.")]
    public AudioClip departureSound;
    [Range(0f, 1f)]
    public float departureVolume = 1f;

    public float minAudioDistance = 10f;
    public float maxAudioDistance = 90f;

    private GameObject activeMonsterInstance;
    private float encounterTimer;
    private bool isMonsterActive;

    private void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;

        if (blindMonster != null && blindMonster.scene.IsValid())
        {
            blindMonster.SetActive(false);
        }
    }

    private void Update()
    {
        if (blindMonster == null) return;

        if (!isMonsterActive)
        {
            if (idleNoiseDecayRate > 0f && currentAccumulatedNoise > 0f)
            {
                currentAccumulatedNoise = Mathf.Max(0f, currentAccumulatedNoise - idleNoiseDecayRate * Time.deltaTime);
            }
        }
        else
        {
            MonitorBlindMonster();
        }
    }

    public void AddNoise(float amount)
    {
        if (isMonsterActive) return;

        currentAccumulatedNoise += amount;

        if (currentAccumulatedNoise >= spawnNoiseThreshold)
        {
            TrySpawnBlindMonster();
        }
    }

    private void TrySpawnBlindMonster()
    {
        if (!TryFindHiddenNavMeshPoint(out Vector3 spawnPos)) return;

        currentAccumulatedNoise = 0f;
        isMonsterActive = true;
        encounterTimer = 0f;

        if (blindMonster.scene.IsValid())
        {
            activeMonsterInstance = blindMonster;
            activeMonsterInstance.transform.position = spawnPos;
            activeMonsterInstance.SetActive(true);
        }
        else
        {
            activeMonsterInstance = Instantiate(blindMonster, spawnPos, Quaternion.identity);
        }

        if (activeMonsterInstance.TryGetComponent<NavMeshAgent>(out var agent))
        {
            agent.Warp(spawnPos);
        }

        if (activeMonsterInstance.TryGetComponent<MonsterAI>(out var ai))
        {
            ai.SetInitialWanderNear(transform.position, 15f);
        }

        PlayDirectionalSound(arrivalSound, arrivalVolume, spawnPos, "BlindMonsterArrivalCue");
    }

    private void MonitorBlindMonster()
    {
        if (activeMonsterInstance == null)
        {
            DespawnBlindMonster();
            return;
        }

        encounterTimer += Time.deltaTime;
        float dist = Vector3.Distance(transform.position, activeMonsterInstance.transform.position);
        bool isVisible = IsPointVisibleToCamera(activeMonsterInstance.transform.position + Vector3.up * 1.2f);

        bool timedOut = encounterTimer >= activeDuration && !isVisible && dist >= minSpawnDistance;
        bool escaped = dist >= maxActiveDistance && !isVisible;

        if (timedOut || escaped)
        {
            DespawnBlindMonster();
        }
    }

    private void DespawnBlindMonster()
    {
        if (activeMonsterInstance != null)
        {
            // Play the departure cue at the monster's last world position before deactivating/destroying it
            PlayDirectionalSound(departureSound, departureVolume, activeMonsterInstance.transform.position, "BlindMonsterDepartureCue");

            if (blindMonster != null && blindMonster.scene.IsValid())
            {
                activeMonsterInstance.SetActive(false);
            }
            else
            {
                Destroy(activeMonsterInstance);
            }
        }

        activeInstanceCleanup();
    }

    private void activeInstanceCleanup()
    {
        activeMonsterInstance = null;
        isMonsterActive = false;
        encounterTimer = 0f;
        currentAccumulatedNoise = 0f;
    }

    private bool TryFindHiddenNavMeshPoint(out Vector3 result)
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;

        for (int i = 0; i < 15; i++)
        {
            float angle = (i < 10) ? Random.Range(-85f, 85f) : Random.Range(-180f, 180f);
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 candidate = transform.position + (Quaternion.Euler(0f, angle, 0f) * flatForward) * distance;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 6f, NavMesh.AllAreas))
            {
                float actualDist = Vector3.Distance(transform.position, hit.position);
                if (actualDist >= minSpawnDistance && !IsPointVisibleToCamera(hit.position + Vector3.up * 1.2f))
                {
                    result = hit.position;
                    return true;
                }
            }
        }

        result = Vector3.zero;
        return false;
    }

    private bool IsPointVisibleToCamera(Vector3 worldPoint)
    {
        if (playerCamera == null) return false;

        Vector3 viewportPos = playerCamera.WorldToViewportPoint(worldPoint);
        bool inFrustum = viewportPos.z > 0f && viewportPos.x >= -0.05f && viewportPos.x <= 1.05f && viewportPos.y >= -0.05f && viewportPos.y <= 1.05f;
        if (!inFrustum) return false;

        if (Physics.Linecast(playerCamera.transform.position, worldPoint, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (activeMonsterInstance != null && hit.transform.IsChildOf(activeMonsterInstance.transform))
            {
                return true;
            }
            return false;
        }

        return true;
    }

    private void PlayDirectionalSound(AudioClip clip, float volume, Vector3 position, string cueName)
    {
        if (clip == null) return;

        GameObject audioObj = new GameObject(cueName);
        audioObj.transform.position = position;

        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minAudioDistance;
        source.maxDistance = maxAudioDistance;
        source.Play();

        Destroy(audioObj, clip.length + 0.2f);
    }
}