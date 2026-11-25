using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("How far the camera's aiming laser reaches")]
    public float interactRange = 5f;
    public ResourceTracker robotInventory;

    [Header("Audio")]
    [Tooltip("Sound clips played at random whenever the player interacts with / picks up an item.")]
    public AudioClip[] interactSounds;
    [Range(0f, 1f)]
    public float interactVolume = 1f;
    [Tooltip("Lowest pitch value (1.0 = normal pitch).")]
    [Range(0.5f, 2f)]
    public float minPitch = 1.0f;
    [Tooltip("Highest random pitch increase.")]
    [Range(1f, 3f)]
    public float maxPitch = 1.25f;
    [Tooltip("Optional: Assign a dedicated AudioSource, or leave empty to auto-create one.")]
    public AudioSource interactAudioSource;

    [Header("References")]
    public Camera mainCamera;

    private CollectibleNote currentlyLookedAtNote;

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (robotInventory == null)
        {
            robotInventory = GetComponent<ResourceTracker>();
            if (robotInventory == null)
                robotInventory = GetComponentInParent<ResourceTracker>();
        }

        if (interactAudioSource == null)
        {
            interactAudioSource = gameObject.AddComponent<AudioSource>();
            interactAudioSource.playOnAwake = false;
            interactAudioSource.spatialBlend = 0f;
            interactAudioSource.ignoreListenerPause = true;
        }
    }

    private void Update()
    {
        // Do not interact or show prompts while paused or while reading a note popup
        if (Time.timeScale == 0f || (NoteManager.Instance != null && NoteManager.Instance.IsReadingPopup))
        {
            ClearLookedAtNote();
            return;
        }

        UpdateLookTarget();

        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            InteractWithObject();
        }
    }

    private void UpdateLookTarget()
    {
        if (mainCamera == null) return;

        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactRange, ~0, QueryTriggerInteraction.Collide);

        CollectibleNote closestNote = null;
        float closestDistance = Mathf.Infinity;

        foreach (RaycastHit hit in hits)
        {
            CollectibleNote note = hit.collider.GetComponentInParent<CollectibleNote>();
            if (note != null && !note.IsCollected && hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestNote = note;
            }
        }

        if (closestNote != currentlyLookedAtNote)
        {
            ClearLookedAtNote();
            currentlyLookedAtNote = closestNote;
            if (currentlyLookedAtNote != null)
            {
                currentlyLookedAtNote.SetPromptVisible(true);
            }
        }
    }

    private void InteractWithObject()
    {
        if (mainCamera == null) return;

        // Shoot a ray exactly where the center of the camera is looking
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);

        // RaycastAll shoots through EVERYTHING in its path up to the interactRange
        RaycastHit[] hits = Physics.RaycastAll(ray, interactRange, ~0, QueryTriggerInteraction.Collide);

        ResourcePickup closestResource = null;
        CollectibleNote closestNote = null;
        float closestDistance = Mathf.Infinity;

        // Check every single object the ray pierced through
        foreach (RaycastHit hit in hits)
        {
            ResourcePickup item = hit.collider.GetComponentInParent<ResourcePickup>();
            if (item != null && hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestResource = item;
                closestNote = null;
            }

            CollectibleNote note = hit.collider.GetComponentInParent<CollectibleNote>();
            if (note != null && !note.IsCollected && hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestNote = note;
                closestResource = null;
            }
        }

        // Collect whichever interactable object was closest along the ray
        if (closestNote != null)
        {
            ClearLookedAtNote();
            PlayRandomInteractSound();
            closestNote.Collect();
        }
        else if (closestResource != null)
        {
            PlayRandomInteractSound();
            closestResource.Collect(robotInventory);
        }
    }

    private void PlayRandomInteractSound()
    {
        if (interactSounds == null || interactSounds.Length == 0 || interactAudioSource == null) return;

        AudioClip clip = interactSounds[Random.Range(0, interactSounds.Length)];
        if (clip != null)
        {
            float low = Mathf.Min(minPitch, maxPitch);
            float high = Mathf.Max(minPitch, maxPitch);
            interactAudioSource.pitch = Random.Range(low, high);
            interactAudioSource.PlayOneShot(clip, interactVolume);
        }
    }

    private void ClearLookedAtNote()
    {
        if (currentlyLookedAtNote != null)
        {
            currentlyLookedAtNote.SetPromptVisible(false);
            currentlyLookedAtNote = null;
        }
    }
}