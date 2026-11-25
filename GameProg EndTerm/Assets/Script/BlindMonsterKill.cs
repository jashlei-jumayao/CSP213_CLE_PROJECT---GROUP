using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using System.Collections;

public class MonsterCatchPlayer : MonoBehaviour
{
    [Header("Jumpscare Video")]
    [Tooltip("Drag your jumpscare VideoClip directly here.")]
    public VideoClip jumpscareClip;
    [Range(0f, 1f)]
    [Tooltip("Volume of the audio embedded inside the video file itself.")]
    public float videoAudioVolume = 1f;

    [Header("Additional Audio & Fallback")]
    [Tooltip("Drag an AudioClip (MP3/WAV/OGG) here to play alongside the video.")]
    public AudioClip extraJumpscareAudio;
    [Range(0f, 1f)]
    public float extraAudioVolume = 1f;
    [Tooltip("Fallback delay before showing Game Over if no VideoClip is assigned.")]
    public float fallbackRestartDelay = 2.0f;

    [Header("Game Over & Pause UI Panels")]
    [Tooltip("Assign your Game Over UI Panel here (starts hidden automatically and turns on over the frozen video frame).")]
    public GameObject gameOverPanel;
    [Tooltip("Delay in seconds after the jumpscare finishes before the Game Over panel pops up.")]
    public float gameOverDelay = 3.0f;
    [Tooltip("Optional: Assign your Pause Menu UI Panel so it is hidden on Game Over.")]
    public GameObject pauseMenuPanel;

    [Header("Scripts to Disable on Catch")]
    [Tooltip("Assign your Pause Menu controller script here so the player cannot press Esc during Game Over.")]
    public MonoBehaviour pauseMenuScript;
    [Tooltip("Optional: Any extra player scripts (e.g., mouse look, flashlight) to disable when caught.")]
    public MonoBehaviour[] extraScriptsToDisable;

    private bool isCatching = false;
    private RenderTexture activeRenderTexture;

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (activeRenderTexture != null)
        {
            activeRenderTexture.Release();
            Destroy(activeRenderTexture);
            activeRenderTexture = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCatching) return;

        Transform rootTransform = other.attachedRigidbody != null
            ? other.attachedRigidbody.transform
            : other.transform.root;

        if (rootTransform.CompareTag("Player") || other.CompareTag("Player"))
        {
            PlayerNoiseEmitter noiseScript = rootTransform.GetComponentInChildren<PlayerNoiseEmitter>();
            if (noiseScript != null && !noiseScript.enabled)
            {
                return;
            }

            StartCoroutine(PlayJumpscareAndShowGameOver(rootTransform));
        }
    }

    private IEnumerator PlayJumpscareAndShowGameOver(Transform playerRoot)
    {
        isCatching = true;

        AudioListener.pause = false;
        if (AudioListener.volume <= 0.01f) AudioListener.volume = 1f;

        // 1. Safely disable player movement, spawners, and pause controls
        DisablePlayerAndPauseControls(playerRoot);

        AudioSource extraSource = null;
        if (extraJumpscareAudio != null)
        {
            extraSource = gameObject.AddComponent<AudioSource>();
            extraSource.clip = extraJumpscareAudio;
            extraSource.volume = extraAudioVolume;
            extraSource.spatialBlend = 0f;
            extraSource.playOnAwake = false;
            extraSource.ignoreListenerPause = true;
        }

        int videoSortingOrder = 100;
        if (gameOverPanel != null)
        {
            Canvas gameOverCanvas = gameOverPanel.GetComponentInParent<Canvas>(true);
            if (gameOverCanvas != null && gameOverCanvas.sortingOrder > 0)
            {
                videoSortingOrder = gameOverCanvas.sortingOrder - 1;
            }
        }

        GameObject overlayObj = new GameObject("RuntimeJumpscareCanvas");
        Canvas canvas = overlayObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = videoSortingOrder;
        overlayObj.AddComponent<CanvasScaler>();

        GameObject bgObj = new GameObject("BlackBackdrop");
        bgObj.transform.SetParent(overlayObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = Color.black;
        StretchFullScreen(bgImage.rectTransform);

        if (jumpscareClip != null)
        {
            GameObject screenObj = new GameObject("VideoScreen");
            screenObj.transform.SetParent(overlayObj.transform, false);
            RawImage rawImage = screenObj.AddComponent<RawImage>();
            rawImage.color = Color.white;
            StretchFullScreen(rawImage.rectTransform);

            int width = jumpscareClip.width > 0 ? (int)jumpscareClip.width : 1920;
            int height = jumpscareClip.height > 0 ? (int)jumpscareClip.height : 1080;
            activeRenderTexture = new RenderTexture(width, height, 0);
            activeRenderTexture.Create();
            rawImage.texture = activeRenderTexture;

            VideoPlayer vp = overlayObj.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.isLooping = false;
            vp.source = VideoSource.VideoClip;
            vp.clip = jumpscareClip;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = activeRenderTexture;
            vp.aspectRatio = VideoAspectRatio.FitInside;

            vp.audioOutputMode = VideoAudioOutputMode.Direct;
            vp.controlledAudioTrackCount = 1;
            vp.EnableAudioTrack(0, true);

            bool videoFinished = false;
            vp.loopPointReached += (source) => videoFinished = true;

            vp.Prepare();
            float prepTimer = 3f;
            while (!vp.isPrepared && prepTimer > 0f)
            {
                prepTimer -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (vp.audioTrackCount > 0)
            {
                vp.SetDirectAudioMute(0, false);
                vp.SetDirectAudioVolume(0, videoAudioVolume);
            }

            vp.Play();
            if (extraSource != null)
            {
                extraSource.Play();
            }
            yield return null;

            float maxDuration = (float)jumpscareClip.length + 1.0f;
            float elapsed = 0f;
            while (!videoFinished && elapsed < maxDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Hold on the final frame of the video
            vp.Pause();
        }
        else
        {
            if (extraSource != null) extraSource.Play();

            float waitTime = (extraJumpscareAudio != null) ? extraJumpscareAudio.length : fallbackRestartDelay;
            yield return new WaitForSecondsRealtime(waitTime);
        }

        // 2. Wait 3 seconds while holding on the frozen frame before popping up Game Over
        if (gameOverDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(gameOverDelay);
        }

        // 3. Turn on the Game Over panel in front of the frozen frame
        ShowGameOverScreen(videoSortingOrder);
    }

    private void DisablePlayerAndPauseControls(Transform playerRoot)
    {
        if (playerRoot.TryGetComponent<Rigidbody>(out var rb) && !rb.isKinematic)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }

        if (playerRoot.TryGetComponent<WheeledRobotMovement>(out var movement))
        {
            movement.enabled = false;
        }

        BlindMonsterNoiseSpawner blindSpawner = playerRoot.GetComponentInChildren<BlindMonsterNoiseSpawner>();
        if (blindSpawner != null) blindSpawner.enabled = false;

        VisionMonsterLightSpawner visionSpawner = playerRoot.GetComponentInChildren<VisionMonsterLightSpawner>();
        if (visionSpawner != null) visionSpawner.enabled = false;

        if (pauseMenuScript != null)
        {
            pauseMenuScript.enabled = false;
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        if (extraScriptsToDisable != null)
        {
            foreach (MonoBehaviour script in extraScriptsToDisable)
            {
                if (script != null && script != this)
                {
                    script.enabled = false;
                }
            }
        }
    }

    private void ShowGameOverScreen(int videoSortingOrder)
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

#if UNITY_6000_0_OR_NEWER
        bool hasEventSystem = FindFirstObjectByType<EventSystem>() != null;
#else
        bool hasEventSystem = FindObjectOfType<EventSystem>() != null;
#endif
        if (!hasEventSystem)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        if (gameOverPanel != null)
        {
            Canvas parentCanvas = gameOverPanel.GetComponentInParent<Canvas>(true);
            if (parentCanvas != null)
            {
                parentCanvas.gameObject.SetActive(true);

                if (parentCanvas.sortingOrder <= videoSortingOrder)
                {
                    parentCanvas.sortingOrder = videoSortingOrder + 1;
                }
            }

            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
        }
    }

    private void StretchFullScreen(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}