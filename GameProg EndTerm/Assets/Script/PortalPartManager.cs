using UnityEngine;
using UnityEngine.Events;

public class PortalPartManager : MonoBehaviour
{
    public static PortalPartManager Instance { get; private set; }

    [Header("Part HUD UI Icons (Initially Hidden)")]
    [Tooltip("UI GameObject (e.g., RawImage/Image) that appears on the HUD when Part 1 is gathered.")]
    public GameObject part1UI;
    [Tooltip("UI GameObject that appears on the HUD when Part 2 is gathered.")]
    public GameObject part2UI;
    [Tooltip("UI GameObject that appears on the HUD when Part 3 is gathered.")]
    public GameObject part3UI;

    [Header("Monster Spawner Controllers (Auto-Detected if Empty)")]
    [Tooltip("Assign your player's BlindMonsterNoiseSpawner component.")]
    public BlindMonsterNoiseSpawner blindMonsterSpawner;
    [Tooltip("Assign your player's VisionMonsterLightSpawner component.")]
    public VisionMonsterLightSpawner visionMonsterSpawner;

    [Header("Hidden 3rd Part Unlock")]
    [Tooltip("The 3rd portal part GameObject in the scene. Starts hidden and appears once Part 1 and Part 2 are both collected.")]
    public GameObject thirdPartWorldObject;

    [Header("Audio (Optional)")]
    public AudioClip partCollectSound;
    [Range(0f, 1f)]
    public float collectVolume = 1f;

    [Header("Events (Optional)")]
    public UnityEvent onAllPartsCollected;

    private bool hasPart1 = false;
    private bool hasPart2 = false;
    private bool hasPart3 = false;

    public bool HasPart1 => hasPart1;
    public bool HasPart2 => hasPart2;
    public bool HasPart3 => hasPart3;
    public bool AllPartsCollected => hasPart1 && hasPart2 && hasPart3;
    public int CollectedCount => (hasPart1 ? 1 : 0) + (hasPart2 ? 1 : 0) + (hasPart3 ? 1 : 0);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Hide all 3 part HUD UI elements initially
        if (part1UI != null) part1UI.SetActive(false);
        if (part2UI != null) part2UI.SetActive(false);
        if (part3UI != null) part3UI.SetActive(false);

        // Hide the 3rd portal part in the world until Parts 1 & 2 are collected
        if (thirdPartWorldObject != null && !hasPart3)
        {
            thirdPartWorldObject.SetActive(false);
        }

        CacheMonsterSpawners();
    }

    private void CacheMonsterSpawners()
    {
        if (blindMonsterSpawner == null)
        {
            blindMonsterSpawner = FindFirstObjectByType<BlindMonsterNoiseSpawner>(FindObjectsInactive.Include);
        }

        if (visionMonsterSpawner == null)
        {
            visionMonsterSpawner = FindFirstObjectByType<VisionMonsterLightSpawner>(FindObjectsInactive.Include);
        }
    }

    public void CollectPart(PortalPartPickup.PortalPartType partType)
    {
        switch (partType)
        {
            case PortalPartPickup.PortalPartType.Part1:
                if (hasPart1) return;
                hasPart1 = true;
                if (part1UI != null) part1UI.SetActive(true);
                break;

            case PortalPartPickup.PortalPartType.Part2:
                if (hasPart2) return;
                hasPart2 = true;
                if (part2UI != null) part2UI.SetActive(true);
                break;

            case PortalPartPickup.PortalPartType.Part3:
                if (hasPart3) return;
                hasPart3 = true;
                if (part3UI != null) part3UI.SetActive(true);
                break;
        }

        if (partCollectSound != null)
        {
            Vector3 soundPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(partCollectSound, soundPos, collectVolume);
        }

        UpdateMonsterSpawners();

        // Reveal the hidden 3rd part once both Part 1 and Part 2 have been gathered
        if (hasPart1 && hasPart2 && !hasPart3 && thirdPartWorldObject != null && !thirdPartWorldObject.activeSelf)
        {
            thirdPartWorldObject.SetActive(true);
        }

        if (AllPartsCollected)
        {
            onAllPartsCollected?.Invoke();
        }
    }

    private void UpdateMonsterSpawners()
    {
        CacheMonsterSpawners();

        if (hasPart1 && hasPart2)
        {
            // Both Part 1 and Part 2 collected -> Both controllers active
            if (visionMonsterSpawner != null) visionMonsterSpawner.enabled = true;
            if (blindMonsterSpawner != null) blindMonsterSpawner.enabled = true;
        }
        else if (hasPart1)
        {
            // Only Part 1 collected -> Vision controller active, Blind controller deactivated
            if (visionMonsterSpawner != null) visionMonsterSpawner.enabled = true;

            if (blindMonsterSpawner != null)
            {
                blindMonsterSpawner.enabled = false;
                blindMonsterSpawner.currentAccumulatedNoise = 0f;

                // Hide the blind monster if it happened to be active when Part 1 was picked up
                if (blindMonsterSpawner.blindMonster != null && blindMonsterSpawner.blindMonster.scene.IsValid())
                {
                    blindMonsterSpawner.blindMonster.SetActive(false);
                }
            }
        }
    }
}