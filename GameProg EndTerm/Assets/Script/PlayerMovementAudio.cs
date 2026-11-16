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

    [Header("Sprint Boost Settings")]
    public float sprintVolumeMultiplier = 1.3f;
    public float sprintPitchMultiplier = 1.15f;
    public float sprintStepDistanceMultiplier = 0.75f;

    [Header("Rotation Settings")]
    [Range(0f, 1f)] public float rotationVolumeMultiplier = 0.4f;
    public float rotationStepAngle = 20f;

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
    private float lastStepAngle;
    private float currentTargetDistance;
    private Coroutine fadeCoroutine;

    private int lastPlayedIndex = -1;
    private bool wasSprintingLastFrame = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = GetComponentInParent<Rigidbody>();

        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;

        lastStepPosition = transform.position;
        lastStepAngle = transform.eulerAngles.y;
        SetNextStepDistance();
    }

    private void Update()
    {
        if (rb == null) return;

        bool isMoving = rb.linearVelocity.magnitude > minSpeed;
        bool isSprinting = isMoving && Input.GetKey(KeyCode.LeftShift);
        bool isTurning = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.01f || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);

        // REAL-TIME SPRINT BOOST: If sprinting changes mid-clip, instantly scale the currently playing audio
        if (audioSource.isPlaying && isSprinting != wasSprintingLastFrame)
        {
            if (isSprinting)
            {
                audioSource.pitch *= sprintPitchMultiplier;
                audioSource.volume = Mathf.Clamp01(audioSource.volume * sprintVolumeMultiplier);
            }
            else
            {
                audioSource.pitch /= sprintPitchMultiplier;
                audioSource.volume = Mathf.Clamp01(audioSource.volume / sprintVolumeMultiplier);
            }
        }
        wasSprintingLastFrame = isSprinting;

        if (isMoving || isTurning)
        {
            // If the player starts moving/turning again while fading, interrupt the fade
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
                audioSource.Stop();
            }

            // 1. Process Movement Steps
            if (isMoving)
            {
                float distanceMoved = Vector3.Distance(transform.position, lastStepPosition);
                float requiredDistance = currentTargetDistance * (isSprinting ? sprintStepDistanceMultiplier : 1f);

                if (distanceMoved >= requiredDistance)
                {
                    if (!audioSource.isPlaying)
                    {
                        PlaySound(1f, isSprinting);
                        lastStepPosition = transform.position;
                        SetNextStepDistance();
                    }
                }
            }
            else
            {
                lastStepPosition = transform.position;
            }

            // 2. Process Rotation Ticks (Obeys !audioSource.isPlaying so it never interlaps)
            if (isTurning)
            {
                float angleMoved = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, lastStepAngle));

                if (angleMoved >= rotationStepAngle)
                {
                    if (!audioSource.isPlaying)
                    {
                        PlaySound(rotationVolumeMultiplier, isSprinting);
                        lastStepAngle = transform.eulerAngles.y;
                    }
                }
            }
            else
            {
                lastStepAngle = transform.eulerAngles.y;
            }
        }
        else
        {
            lastStepPosition = transform.position;
            lastStepAngle = transform.eulerAngles.y;
            wasSprintingLastFrame = false;

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

    private void PlaySound(float volumeScale, bool isSprinting)
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

        float pitch = Random.Range(minPitch, maxPitch);
        float volume = baseVolume * Random.Range(minVolumeMod, maxVolumeMod) * volumeScale;

        if (isSprinting)
        {
            pitch *= sprintPitchMultiplier;
            volume *= sprintVolumeMultiplier;
        }

        audioSource.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        audioSource.volume = Mathf.Clamp01(volume);

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