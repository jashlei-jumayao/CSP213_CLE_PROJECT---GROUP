using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class PlayerMovementAudio : MonoBehaviour
{
    [Header("Audio Clips")]
    [Tooltip("Assign your 2 wheel/footstep sounds here.")]
    public AudioClip[] movementSounds;

    [Header("Movement Settings")]
    public float baseStepDistance = 1.0f;
    public float stepDistanceVariance = 0.3f;
    public float minSpeed = 0.5f;

    [Header("Volume & Fade Settings")]
    [Range(0f, 1f)] public float baseVolume = 0.5f;
    public float minVolumeMod = 0.85f;
    public float maxVolumeMod = 1.15f;
    [Tooltip("How quickly the sound fades out when stopping (in seconds)")]
    public float fadeDuration = 0.2f;

    [Header("Pitch Settings (Low Pitches)")]
    [Range(0.1f, 1.5f)] public float minPitch = 0.5f;
    [Range(0.1f, 1.5f)] public float maxPitch = 0.8f;

    private Rigidbody rb;
    private AudioSource audioSource;
    private Vector3 lastStepPosition;
    private float currentTargetDistance;
    private Coroutine fadeCoroutine;

    private int lastPlayedIndex = -1;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = GetComponentInParent<Rigidbody>();

        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;

        lastStepPosition = transform.position;
        SetNextStepDistance();
    }

    private void Update()
    {
        if (rb == null) return;

        if (rb.linearVelocity.magnitude > minSpeed)
        {
            // If the player starts moving again while fading, interrupt the fade
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
                audioSource.Stop(); // Cut the quiet tail so the next step can trigger immediately
            }

            float distanceMoved = Vector3.Distance(transform.position, lastStepPosition);

            if (distanceMoved >= currentTargetDistance)
            {
                if (!audioSource.isPlaying)
                {
                    PlayMovementSound();
                    lastStepPosition = transform.position;
                    SetNextStepDistance();
                }
            }
        }
        else
        {
            lastStepPosition = transform.position;

            // Trigger the fade out if we are currently playing and not already fading
            if (audioSource.isPlaying && fadeCoroutine == null)
            {
                fadeCoroutine = StartCoroutine(FadeOutAudio());
            }
        }
    }

    private void SetNextStepDistance()
    {
        currentTargetDistance = baseStepDistance + Random.Range(-stepDistanceVariance, stepDistanceVariance);
        currentTargetDistance = Mathf.Max(0.1f, currentTargetDistance);
    }

    private void PlayMovementSound()
    {
        if (movementSounds.Length == 0) return;

        int randomIndex = lastPlayedIndex;
        if (movementSounds.Length > 1)
        {
            while (randomIndex == lastPlayedIndex)
            {
                randomIndex = Random.Range(0, movementSounds.Length);
            }
        }
        else
        {
            randomIndex = 0;
        }
        lastPlayedIndex = randomIndex;

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.volume = baseVolume * Random.Range(minVolumeMod, maxVolumeMod);

        audioSource.clip = movementSounds[randomIndex];
        audioSource.Play();
    }

    private IEnumerator FadeOutAudio()
    {
        float startVolume = audioSource.volume;
        float timeElapsed = 0f;

        while (timeElapsed < fadeDuration)
        {
            timeElapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timeElapsed / fadeDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume; // Reset volume for the next time it plays
        fadeCoroutine = null;
    }
}