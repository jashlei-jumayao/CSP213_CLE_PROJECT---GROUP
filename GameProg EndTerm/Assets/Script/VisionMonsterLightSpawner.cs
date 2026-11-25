using UnityEngine;
using UnityEngine.AI;

public class VisionMonsterLightSpawner : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    [Tooltip("Assign your single Vision Monster scene object (recommended) or prefab.")]
    public GameObject visionMonster;
    [Tooltip("Assign your robot's flashlight Light component (auto-detected if left empty).")]
    public Light robotFlashlight;

    [Header("Light Exposure Threshold Settings")]
    [Tooltip("Total accumulated light exposure needed to summon the vision monster.")]
    public float spawnLightThreshold = 100f;
    [Tooltip("How much light exposure builds up per second while the flashlight is turned ON.")]
    public float lightBuildupPerSecond = 12f;
    [Tooltip("Extra instant exposure added each time the player clicks the flashlight ON (discourages rapid flickering).")]
    public float flashlightToggleBurst = 4f;
    [Tooltip("How fast the meter cools down per second while the flashlight is OFF (0 = never decays).")]
    public float darkDecayRate = 5f;
    [Tooltip("Read-only view of current light exposure buildup.")]
    public float currentLightExposure = 0f;

    [Header("Front Spawn & Proximity Settings")]
    [Tooltip("Minimum distance in front of the player where the monster can appear.")]
    public float minSpawnDistance = 12f;
    [Tooltip("Maximum distance in front of the player where the monster can appear.")]
    public float maxSpawnDistance = 25f;
    [Tooltip("Cone angle in front of the player's view to pick a spawn spot (smaller = dead center in front).")]
    [Range(0f, 60f)]
    public float frontSpawnConeAngle = 20f;
    [Tooltip("If true, avoids spawning directly inside a solid wall/obstacle behind a doorway.")]
    public bool requireClearLineOfSight = true;
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
    private bool wasFlashlightOn;

    private void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;

        if (robotFlashlight == null)
        {
            FlashlightController controller = GetComponentInChildren<FlashlightController>(true);
            if (controller != null && controller.robotFlashlight != null)
            {
                robotFlashlight = controller.robotFlashlight;
            }
            else if (playerCamera != null)
            {
                robotFlashlight = playerCamera.GetComponentInChildren<Light>(true);
            }
        }

        if (visionMonster != null && visionMonster.scene.IsValid())
        {
            visionMonster.SetActive(false);
        }
    }

    private void Update()
    {
        if (visionMonster == null) return;

        bool isFlashlightOn = IsFlashlightCurrentlyOn();

        if (!isMonsterActive)
        {
            if (isFlashlightOn)
            {
                if (!wasFlashlightOn && flashlightToggleBurst > 0f)
                {
                    currentLightExposure += flashlightToggleBurst;
                }

                currentLightExposure += lightBuildupPerSecond * Time.deltaTime;

                if (currentLightExposure >= spawnLightThreshold)
                {
                    currentLightExposure = spawnLightThreshold;
                    TrySpawnVisionMonster();
                }
            }
            else if (darkDecayRate > 0f && currentLightExposure > 0f)
            {
                currentLightExposure = Mathf.Max(0f, currentLightExposure - darkDecayRate * Time.deltaTime);
            }
        }
        else
        {
            MonitorVisionMonster();
        }

        wasFlashlightOn = isFlashlightOn;
    }

    private bool IsFlashlightCurrentlyOn()
    {
        if (robotFlashlight == null) return false;
        return robotFlashlight.enabled && robotFlashlight.gameObject.activeInHierarchy && robotFlashlight.cullingMask != 0;
    }

    public void AddLightExposure(float amount)
    {
        if (isMonsterActive) return;

        currentLightExposure += amount;

        if (currentLightExposure >= spawnLightThreshold)
        {
            currentLightExposure = spawnLightThreshold;
            TrySpawnVisionMonster();
        }
    }

    private void TrySpawnVisionMonster()
    {
        if (!TryFindFrontNavMeshPoint(out Vector3 spawnPos)) return;

        currentLightExposure = 0f;
        isMonsterActive = true;
        encounterTimer = 0f;

        // Face the monster directly toward the player upon appearing in front of them
        Vector3 lookDir = transform.position - spawnPos;
        lookDir.y = 0f;
        Quaternion spawnRot = lookDir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(lookDir.normalized) : Quaternion.identity;

        if (visionMonster.scene.IsValid())
        {
            activeMonsterInstance = visionMonster;
            activeMonsterInstance.transform.SetPositionAndRotation(spawnPos, spawnRot);
            activeMonsterInstance.SetActive(true);
        }
        else
        {
            activeMonsterInstance = Instantiate(visionMonster, spawnPos, spawnRot);
        }

        if (activeMonsterInstance.TryGetComponent<NavMeshAgent>(out var agent))
        {
            agent.Warp(spawnPos);
        }

        activeMonsterInstance.SendMessage("SetInitialWanderNear", transform.position, SendMessageOptions.DontRequireReceiver);

        PlayDirectionalSound(arrivalSound, arrivalVolume, spawnPos, "VisionMonsterArrivalCue");
    }

    private void MonitorVisionMonster()
    {
        if (activeMonsterInstance == null)
        {
            DespawnVisionMonster();
            return;
        }

        encounterTimer += Time.deltaTime;
        float dist = Vector3.Distance(transform.position, activeMonsterInstance.transform.position);
        bool isVisible = IsPointVisibleToCamera(activeMonsterInstance.transform.position + Vector3.up * 1.2f);

        bool timedOut = encounterTimer >= activeDuration && !isVisible && dist >= minSpawnDistance;
        bool escaped = dist >= maxActiveDistance && !isVisible;

        if (timedOut || escaped)
        {
            DespawnVisionMonster();
        }
    }

    private void DespawnVisionMonster()
    {
        if (activeMonsterInstance != null)
        {
            PlayDirectionalSound(departureSound, departureVolume, activeMonsterInstance.transform.position, "VisionMonsterDepartureCue");

            if (visionMonster != null && visionMonster.scene.IsValid())
            {
                activeMonsterInstance.SetActive(false);
            }
            else
            {
                Destroy(activeMonsterInstance);
            }
        }

        ActiveInstanceCleanup();
    }

    public void ForceDespawnVisionMonster()
    {
        if (!isMonsterActive && activeMonsterInstance == null) return;
        DespawnVisionMonster();
    }

    private void ActiveInstanceCleanup()
    {
        activeMonsterInstance = null;
        isMonsterActive = false;
        encounterTimer = 0f;
        currentLightExposure = 0f;
    }

    private bool TryFindFrontNavMeshPoint(out Vector3 result)
    {
        Transform forwardRef = playerCamera != null ? playerCamera.transform : transform;
        Vector3 flatForward = Vector3.ProjectOnPlane(forwardRef.forward, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;

        Vector3 eyeOrigin = playerCamera != null ? playerCamera.transform.position : (transform.position + Vector3.up * 1.2f);

        // Pass 1: Try directly in front of the player within frontSpawnConeAngle and with clear line of sight
        for (int i = 0; i < 15; i++)
        {
            float angle = (i == 0) ? 0f : Random.Range(-frontSpawnConeAngle, frontSpawnConeAngle);
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 candidate = transform.position + (Quaternion.Euler(0f, angle, 0f) * flatForward) * distance;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                Vector3 toHit = (hit.position - transform.position);
                toHit.y = 0f;
                float actualDist = toHit.magnitude;

                if (actualDist >= minSpawnDistance * 0.75f && Vector3.Dot(flatForward, toHit.normalized) > 0.5f)
                {
                    if (!requireClearLineOfSight || HasClearLineOfSight(eyeOrigin, hit.position + Vector3.up * 1.2f))
                    {
                        result = hit.position;
                        return true;
                    }
                }
            }
        }

        // Pass 2: Fallback in front even if a tree/prop partially blocks line of sight
        for (int i = 0; i < 10; i++)
        {
            float angle = Random.Range(-frontSpawnConeAngle * 1.5f, frontSpawnConeAngle * 1.5f);
            float distance = Random.Range(minSpawnDistance * 0.7f, maxSpawnDistance);
            Vector3 candidate = transform.position + (Quaternion.Euler(0f, angle, 0f) * flatForward) * distance;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 6f, NavMesh.AllAreas))
            {
                Vector3 toHit = (hit.position - transform.position);
                toHit.y = 0f;
                if (toHit.sqrMagnitude > 4f && Vector3.Dot(flatForward, toHit.normalized) > 0.35f)
                {
                    result = hit.position;
                    return true;
                }
            }
        }

        result = Vector3.zero;
        return false;
    }

    private bool HasClearLineOfSight(Vector3 origin, Vector3 targetPoint)
    {
        if (Physics.Linecast(origin, targetPoint, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (activeMonsterInstance != null && hit.transform.IsChildOf(activeMonsterInstance.transform))
            {
                return true;
            }
            return false;
        }
        return true;
    }

    private bool IsPointVisibleToCamera(Vector3 worldPoint)
    {
        if (playerCamera == null) return false;

        Vector3 viewportPos = playerCamera.WorldToViewportPoint(worldPoint);
        bool inFrustum = viewportPos.z > 0f && viewportPos.x >= -0.05f && viewportPos.x <= 1.05f && viewportPos.y >= -0.05f && viewportPos.y <= 1.05f;
        if (!inFrustum) return false;

        return HasClearLineOfSight(playerCamera.transform.position, worldPoint);
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