using UnityEngine;

public class AnimationImpactEffects : MonoBehaviour
{
    [Header("Sound Effects")]
    [Tooltip("Audio clips chosen at random when the animation event fires.")]
    public AudioClip[] impactSounds;
    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(0.7f, 1.3f)]
    public float minPitch = 0.92f;
    [Range(0.7f, 1.3f)]
    public float maxPitch = 1.08f;

    [Header("3D Audio Distance")]
    [Tooltip("0 = 2D full volume anywhere, 1 = 3D positional audio.")]
    [Range(0f, 1f)]
    public float spatialBlend = 1f;
    public float minAudioDistance = 4f;
    public float maxAudioDistance = 45f;

    [Header("Screen Shake Settings")]
    [Tooltip("Maximum camera shake strength when right next to the player.")]
    public float maxShakeIntensity = 0.08f;
    [Tooltip("How long the screen shake lasts (seconds).")]
    public float shakeDuration = 0.15f;
    [Tooltip("Max distance from the player where the screen shake can still be felt (0 = always full strength).")]
    public float maxShakeDistance = 25f;

    private AudioSource audioSource;
    private Transform playerTransform;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = spatialBlend;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = minAudioDistance;
        audioSource.maxDistance = maxAudioDistance;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    // Select this function name inside your Animation Event
    public void TriggerAnimationImpact()
    {
        PlayImpactSound();
        TriggerShake();
    }

    private void PlayImpactSound()
    {
        if (impactSounds == null || impactSounds.Length == 0 || audioSource == null) return;

        AudioClip clip = impactSounds[Random.Range(0, impactSounds.Length)];
        if (clip != null)
        {
            audioSource.spatialBlend = spatialBlend;
            audioSource.pitch = Random.Range(Mathf.Min(minPitch, maxPitch), Mathf.Max(minPitch, maxPitch));
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private void TriggerShake()
    {
        if (CameraShake.Instance == null) return;

        if (maxShakeDistance <= 0f)
        {
            CameraShake.Instance.Shake(shakeDuration, maxShakeIntensity);
            return;
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= maxShakeDistance)
        {
            // Smoothly scales shake strength based on how close the player is
            float proximityFactor = 1f - (dist / maxShakeDistance);
            CameraShake.Instance.Shake(shakeDuration, maxShakeIntensity * proximityFactor);
        }
    }
}