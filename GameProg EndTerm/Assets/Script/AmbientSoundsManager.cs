using UnityEngine;
using System.Collections;

public class AmbientSoundPlayer : MonoBehaviour
{
    [Header("Audio Clips")]
    [Tooltip("Each clip runs on its own independent timer so multiple sounds can overlap.")]
    public AudioClip[] ambientSounds;

    [Header("Timing Settings (Seconds)")]
    [Tooltip("Minimum wait time before a clip can play again (45s).")]
    public float minDelay = 45f;
    [Tooltip("Maximum wait time before a clip can play again (120s = 2 mins).")]
    public float maxDelay = 120f;
    [Tooltip("If true, all clips play once at Start before starting their independent random timers.")]
    public bool playOnStart = false;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float volume = 0.6f;
    [Tooltip("0 = 2D global ambience (heard equally everywhere), 1 = 3D positional audio.")]
    [Range(0f, 1f)] public float spatialBlend = 0f;

    private void Start()
    {
        if (ambientSounds == null || ambientSounds.Length == 0) return;

        for (int i = 0; i < ambientSounds.Length; i++)
        {
            AudioClip clip = ambientSounds[i];
            if (clip == null) continue;

            GameObject sourceObj = new GameObject($"AmbientChannel_{clip.name}");
            sourceObj.transform.SetParent(transform, false);

            AudioSource source = sourceObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.playOnAwake = false;
            source.loop = false;
            source.pitch = 1f;
            source.volume = volume;
            source.spatialBlend = spatialBlend;

            StartCoroutine(IndependentClipRoutine(clip, source));
        }
    }

    private IEnumerator IndependentClipRoutine(AudioClip clip, AudioSource source)
    {
        if (!playOnStart)
        {
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
        }

        while (true)
        {
            source.volume = volume;
            source.Play();

            yield return new WaitForSeconds(clip.length);
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
        }
    }
}