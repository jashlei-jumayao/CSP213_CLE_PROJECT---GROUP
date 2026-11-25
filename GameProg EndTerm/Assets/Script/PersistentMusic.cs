using UnityEngine;

public class PersistentMusic : MonoBehaviour
{
    private static PersistentMusic instance;

    private void Awake()
    {
        // If a music player already exists, destroy this duplicate
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Otherwise, make this the official music player and keep it alive
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Keep playing even when PauseMenu sets AudioListener.pause = true
        AudioSource[] musicSources = GetComponentsInChildren<AudioSource>(true);
        foreach (AudioSource source in musicSources)
        {
            source.ignoreListenerPause = true;
        }
    }
}