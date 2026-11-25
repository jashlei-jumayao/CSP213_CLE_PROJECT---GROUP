using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class NoteManager : MonoBehaviour
{
    public static NoteManager Instance { get; private set; }

    [System.Serializable]
    public class NoteEntry
    {
        public string noteTitle;
        public Texture noteTexture;
    }

    [Header("Immediate Pickup Popup UI")]
    [Tooltip("Panel that pops up on screen immediately when the player picks up a note.")]
    public GameObject pickupPopupPanel;
    [Tooltip("UI RawImage inside the popup that displays the collected note's Texture.")]
    public RawImage pickupNoteImage;
    [Tooltip("Optional: Text showing the note's title.")]
    public TMP_Text pickupTitleText;
    [Tooltip("Optional: Close button on the pickup popup.")]
    public Button closePopupButton;

    [Header("Scripts to Disable While Reading")]
    [Tooltip("Optional: Drag your Flashlight script or any other player scripts here (auto-detects scripts with 'Flashlight' in the name).")]
    public List<MonoBehaviour> extraScriptsToDisable = new List<MonoBehaviour>();

    [Header("Controls & Audio")]
    public KeyCode closePopupKey = KeyCode.E;
    public bool allowLeftClickToClose = true;
    [Tooltip("Assign multiple paper/page flip sound clips; one will be chosen at random.")]
    public AudioClip[] pageFlipSounds;
    [Range(0f, 1f)]
    public float pageFlipVolume = 1f;

    private List<NoteEntry> collectedNotes = new List<NoteEntry>();
    private bool isReadingPopup = false;
    private int openedOnFrame = -1;

    private AudioSource uiAudioSource;
    private Transform playerRoot;
    private Rigidbody playerRb;
    private MonoBehaviour movementScript;
    private MonoBehaviour cameraScript;
    private AudioSource[] playerAudioSources;
    private List<MonoBehaviour> scriptsToRestore = new List<MonoBehaviour>();

    private CursorLockMode prevLockMode;
    private bool prevCursorVisible;

    public bool IsReadingPopup => isReadingPopup;
    public int CollectedCount => collectedNotes.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Create a dedicated 2D UI AudioSource that still plays even when AudioListener.pause is true in the Pause Menu
        uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;
        uiAudioSource.spatialBlend = 0f;
        uiAudioSource.ignoreListenerPause = true;
    }

    private void Start()
    {
        if (pickupPopupPanel != null)
        {
            pickupPopupPanel.SetActive(false);
        }

        if (closePopupButton != null)
        {
            closePopupButton.onClick.AddListener(ClosePickupPopup);
        }

        CachePlayerReferences();
    }

    private void CachePlayerReferences()
    {
        if (playerRoot != null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerRoot = player.transform.root;
            playerRb = playerRoot.GetComponent<Rigidbody>();
            movementScript = playerRoot.GetComponent<WheeledRobotMovement>();
            cameraScript = playerRoot.GetComponentInChildren<RobotHeadCamera>();
            playerAudioSources = playerRoot.GetComponentsInChildren<AudioSource>(true);

            MonoBehaviour[] allPlayerScripts = playerRoot.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour script in allPlayerScripts)
            {
                if (script != null && script.GetType().Name.ToLower().Contains("flashlight"))
                {
                    if (!extraScriptsToDisable.Contains(script))
                    {
                        extraScriptsToDisable.Add(script);
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (!isReadingPopup) return;

        if (Time.frameCount == openedOnFrame) return;

        bool pressedCloseKey = Input.GetKeyDown(closePopupKey) || Input.GetKeyDown(KeyCode.Escape);
        bool pressedLeftClick = allowLeftClickToClose && Input.GetMouseButtonDown(0);

        if (pressedCloseKey || pressedLeftClick)
        {
            ClosePickupPopup();
        }
    }

    public void CollectNote(string title, Texture noteTexture)
    {
        if (noteTexture == null) return;

        NoteEntry newEntry = new NoteEntry
        {
            noteTitle = string.IsNullOrWhiteSpace(title) ? $"Note #{collectedNotes.Count + 1}" : title,
            noteTexture = noteTexture
        };

        collectedNotes.Add(newEntry);

        OpenPickupPopup(newEntry);
        PlayRandomPageFlipSound();
    }

    public void PlayRandomPageFlipSound()
    {
        if (pageFlipSounds == null || pageFlipSounds.Length == 0) return;

        AudioClip clip = pageFlipSounds[Random.Range(0, pageFlipSounds.Length)];
        if (clip == null) return;

        if (uiAudioSource != null)
        {
            uiAudioSource.PlayOneShot(clip, pageFlipVolume);
        }
        else
        {
            Vector3 soundPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(clip, soundPos, pageFlipVolume);
        }
    }

    private void OpenPickupPopup(NoteEntry entry)
    {
        if (pickupPopupPanel == null || pickupNoteImage == null) return;

        CachePlayerReferences();

        isReadingPopup = true;
        openedOnFrame = Time.frameCount;

        pickupNoteImage.texture = entry.noteTexture;

        if (pickupNoteImage.TryGetComponent<AspectRatioFitter>(out var fitter) && entry.noteTexture.height > 0)
        {
            fitter.aspectRatio = (float)entry.noteTexture.width / entry.noteTexture.height;
        }

        if (pickupTitleText != null)
        {
            pickupTitleText.text = entry.noteTitle;
        }

        pickupPopupPanel.SetActive(true);

        scriptsToRestore.Clear();
        DisableAndTrack(movementScript);
        DisableAndTrack(cameraScript);

        foreach (MonoBehaviour extra in extraScriptsToDisable)
        {
            DisableAndTrack(extra);
        }

        if (playerRb != null && !playerRb.isKinematic)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        if (playerAudioSources != null)
        {
            foreach (AudioSource src in playerAudioSources)
            {
                if (src != null && src.isPlaying)
                {
                    src.Stop();
                }
            }
        }

        prevLockMode = Cursor.lockState;
        prevCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }

    private void DisableAndTrack(MonoBehaviour script)
    {
        if (script != null && script.enabled)
        {
            script.enabled = false;
            scriptsToRestore.Add(script);
        }
    }

    public void ClosePickupPopup()
    {
        if (!isReadingPopup) return;

        isReadingPopup = false;

        if (pickupPopupPanel != null)
        {
            pickupPopupPanel.SetActive(false);
        }

        Cursor.lockState = prevLockMode;
        Cursor.visible = prevCursorVisible;

        Time.timeScale = 1f;

        StartCoroutine(ReenableScriptsNextFrame());
    }

    private IEnumerator ReenableScriptsNextFrame()
    {
        yield return null;

        foreach (MonoBehaviour script in scriptsToRestore)
        {
            if (script != null)
            {
                script.enabled = true;
            }
        }
        scriptsToRestore.Clear();
    }

    public NoteEntry GetNoteAt(int index)
    {
        if (index < 0 || index >= collectedNotes.Count) return null;
        return collectedNotes[index];
    }
}