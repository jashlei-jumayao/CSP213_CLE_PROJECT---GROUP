using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class MonsterCatchPlayer : MonoBehaviour
{
    [Header("Jumpscare Video")]
    [Tooltip("Drag your jumpscare VideoClip directly here. Everything else is built automatically at runtime.")]
    public VideoClip jumpscareClip;

    [Header("Audio & Fallback")]
    [Tooltip("Optional: Drag an AudioClip (MP3/WAV/OGG) here to play alongside the video.")]
    public AudioClip extraJumpscareAudio;
    [Range(0f, 1f)]
    public float extraAudioVolume = 1f;
    [Tooltip("Fallback delay before restarting if no VideoClip is assigned.")]
    public float fallbackRestartDelay = 2.0f;

    private bool isCatching = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isCatching) return;

        Transform rootTransform = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform.root;

        if (rootTransform.CompareTag("Player") || other.CompareTag("Player"))
        {
            // Ignore catch if player is safely hiding inside a chest
            PlayerNoiseEmitter noiseScript = rootTransform.GetComponentInChildren<PlayerNoiseEmitter>();
            if (noiseScript != null && !noiseScript.enabled)
            {
                return;
            }

            StartCoroutine(PlayJumpscareAndRestart(rootTransform));
        }
    }

    private IEnumerator PlayJumpscareAndRestart(Transform playerRoot)
    {
        isCatching = true;

        // 1. Freeze player controls and stop the spawner from despawning the monster mid-scare
        if (playerRoot.TryGetComponent<WheeledRobotMovement>(out var movement))
        {
            movement.enabled = false;
        }

        BlindMonsterNoiseSpawner spawner = playerRoot.GetComponentInChildren<BlindMonsterNoiseSpawner>();
        if (spawner != null)
        {
            spawner.enabled = false;
        }

        // Play the extra AudioClip in 2D (full volume regardless of distance)
        if (extraJumpscareAudio != null)
        {
            AudioSource extraSource = gameObject.AddComponent<AudioSource>();
            extraSource.clip = extraJumpscareAudio;
            extraSource.volume = extraAudioVolume;
            extraSource.spatialBlend = 0f;
            extraSource.playOnAwake = false;
            extraSource.Play();
        }

        if (jumpscareClip != null)
        {
            // 2. Build a top-most fullscreen Canvas that draws over every HUD and camera
            GameObject overlayObj = new GameObject("RuntimeJumpscareCanvas");
            Canvas canvas = overlayObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767; // Maximum possible UI layer priority
            overlayObj.AddComponent<CanvasScaler>();

            // Solid black backdrop so no 3D gameplay peeks around the edges
            GameObject bgObj = new GameObject("BlackBackdrop");
            bgObj.transform.SetParent(overlayObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = Color.black;
            StretchFullScreen(bgImage.rectTransform);

            // RawImage that displays the video frames
            GameObject screenObj = new GameObject("VideoScreen");
            screenObj.transform.SetParent(overlayObj.transform, false);
            RawImage rawImage = screenObj.AddComponent<RawImage>();
            rawImage.color = Color.white;
            StretchFullScreen(rawImage.rectTransform);

            // 3. Create a matching RenderTexture and VideoPlayer dynamically
            int width = jumpscareClip.width > 0 ? (int)jumpscareClip.width : 1920;
            int height = jumpscareClip.height > 0 ? (int)jumpscareClip.height : 1080;
            RenderTexture rt = new RenderTexture(width, height, 0);
            rt.Create();
            rawImage.texture = rt;

            VideoPlayer vp = overlayObj.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.isLooping = false;
            vp.clip = jumpscareClip;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = rt;
            vp.aspectRatio = VideoAspectRatio.FitInside;
            vp.audioOutputMode = VideoAudioOutputMode.Direct;

            bool videoFinished = false;
            vp.loopPointReached += (source) => videoFinished = true;

            vp.Prepare();
            float prepTimer = 3f;
            while (!vp.isPrepared && prepTimer > 0f)
            {
                prepTimer -= Time.unscaledDeltaTime;
                yield return null;
            }

            vp.Play();
            yield return null;

            // Wait until the clip finishes playing
            float maxDuration = (float)jumpscareClip.length + 1.0f;
            float elapsed = 0f;
            while (!videoFinished && elapsed < maxDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            rt.Release();
        }
        else
        {
            float waitTime = (extraJumpscareAudio != null) ? extraJumpscareAudio.length : fallbackRestartDelay;
            yield return new WaitForSecondsRealtime(waitTime);
        }

        // 4. Reload the level
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void StretchFullScreen(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}