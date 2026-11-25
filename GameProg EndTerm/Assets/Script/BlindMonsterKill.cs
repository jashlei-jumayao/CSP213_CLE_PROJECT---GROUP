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
    [Tooltip("Drag an AudioClip (MP3/WAV/OGG) here to play alongside the video (or as backup if Unity can't decode the MP4's audio).")]
    public AudioClip extraJumpscareAudio;
    [Range(0f, 1f)]
    public float extraAudioVolume = 1f;
    [Tooltip("Fallback delay before showing Game Over if no VideoClip is assigned.")]
    public float fallbackRestartDelay = 2.0f;

    [Header("Game Over & Pause UI Panels")]
    [Tooltip("Assign your Game Over UI Panel here (starts hidden automatically and turns on after the jumpscare).")]
    public GameObject gameOverPanel;
    [Tooltip("Optional: Assign your Pause Menu UI Panel so it is guaranteed hidden on Game Over.")]
    public GameObject pauseMenuPanel;

    private bool isCatching = false;

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCatching) return;

        Transform rootTransform = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform.root;

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

        // Ensure global Unity audio is unmuted and unpaused
        AudioListener.pause = false;
        if (AudioListener.volume <= 0.01f) AudioListener.volume = 1f;

        // 1. Completely lock down player movement, camera look, actions, and pausing
        DisableAllPlayerAndPauseControls(playerRoot);

        // Prepare extra AudioSource
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

        GameObject overlayObj = new GameObject("RuntimeJumpscareCanvas");
        Canvas canvas = overlayObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
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
            RenderTexture rt = new RenderTexture(width, height, 0);
            rt.Create();
            rawImage.texture = rt;

            VideoPlayer vp = overlayObj.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.isLooping = false;
            vp.source = VideoSource.VideoClip;
            vp.clip = jumpscareClip;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = rt;
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

            vp.Stop();
            rt.Release();
        }
        else
        {
            if (extraSource != null) extraSource.Play();

            float waitTime = (extraJumpscareAudio != null) ? extraJumpscareAudio.length : fallbackRestartDelay;
            yield return new WaitForSecondsRealtime(waitTime);
        }

        // 2. Remove video overlay, pause time, unlock cursor, and turn on your Game Over panel
        if (overlayObj != null)
        {
            Destroy(overlayObj);
        }

        ShowGameOverScreen();
    }

    private void DisableAllPlayerAndPauseControls(Transform playerRoot)
    {
        // Stop rigidbody movement immediately so the robot doesn't keep sliding
        Rigidbody[] bodies = playerRoot.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rb in bodies)
        {
            if (!rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // Disable all player gameplay scripts (movement, mouse look, flashlight, noise, spawners, etc.)
        MonoBehaviour[] playerScripts = playerRoot.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour script in playerScripts)
        {
            if (script != null && script != this)
            {
                script.enabled = false;
            }
        }

        // Disable any Pause script in the scene so pressing Esc cannot open Pause over Game Over
        MonoBehaviour[] allSceneScripts = FindObjectsOfType<MonoBehaviour>(true);
        foreach (MonoBehaviour script in allSceneScripts)
        {
            if (script == null || script == this) continue;

            string typeName = script.GetType().Name.ToLowerInvariant();
            if (typeName.Contains("pause"))
            {
                script.enabled = false;
            }
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    private void ShowGameOverScreen()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EnsureEventSystemExists();

        if (gameOverPanel != null)
        {
            Canvas parentCanvas = gameOverPanel.GetComponentInParent<Canvas>(true);
            if (parentCanvas != null)
            {
                parentCanvas.gameObject.SetActive(true);
            }

            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
        }
    }

    private void EnsureEventSystemExists()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
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