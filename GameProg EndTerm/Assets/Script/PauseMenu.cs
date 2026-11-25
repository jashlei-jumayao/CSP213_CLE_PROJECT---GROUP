using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PauseMenu : MonoBehaviour
{
    [Header("Pause UI References")]
    [Tooltip("Assign the dimmed Pause Panel containing your Continue, Quit, and Notes UI.")]
    public GameObject pauseMenuUI;
    public Button continueButton;
    public Button quitButton;

    [Header("Collected Notes Viewer (Right Side of Pause Menu)")]
    [Tooltip("UI RawImage on the right side of the pause menu that displays collected note textures.")]
    public RawImage pauseNoteDisplayImage;
    public Button nextNoteButton;
    public Button prevNoteButton;
    [Tooltip("Optional: Displays 'Note Title (1/4)'.")]
    public TMP_Text noteInfoText;
    [Tooltip("Optional: Text/object shown when no notes have been collected yet.")]
    public GameObject emptyNotesPlaceholder;

    [Header("Scripts to Freeze")]
    public List<MonoBehaviour> scriptsToDisableOnPause = new List<MonoBehaviour>();

    [Header("Settings")]
    public KeyCode pauseKey = KeyCode.Escape;
    public bool pauseAudio = true;

    private bool isPaused = false;
    private int currentNoteIndex = 0;
    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;
    private List<MonoBehaviour> reenableList = new List<MonoBehaviour>();

    private void Start()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(Resume);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        if (nextNoteButton != null)
        {
            nextNoteButton.onClick.AddListener(ShowNextNote);
        }

        if (prevNoteButton != null)
        {
            prevNoteButton.onClick.AddListener(ShowPreviousNote);
        }

        RobotHeadCamera headCam = FindFirstObjectByType<RobotHeadCamera>();
        if (headCam != null && !scriptsToDisableOnPause.Contains(headCam))
        {
            scriptsToDisableOnPause.Add(headCam);
        }
    }

    private void Update()
    {
        // Let NoteManager handle closing the immediate pickup popup first
        if (NoteManager.Instance != null && NoteManager.Instance.IsReadingPopup)
        {
            return;
        }

        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        isPaused = true;

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }

        // Open on the newest collected note
        if (NoteManager.Instance != null && NoteManager.Instance.CollectedCount > 0)
        {
            currentNoteIndex = NoteManager.Instance.CollectedCount - 1;
        }
        else
        {
            currentNoteIndex = 0;
        }
        RefreshNoteViewer();

        reenableList.Clear();
        foreach (MonoBehaviour script in scriptsToDisableOnPause)
        {
            if (script != null && script.enabled)
            {
                script.enabled = false;
                reenableList.Add(script);
            }
        }

        previousLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;

        if (pauseAudio)
        {
            AudioListener.pause = true;
        }
    }

    public void Resume()
    {
        isPaused = false;

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        foreach (MonoBehaviour script in reenableList)
        {
            if (script != null)
            {
                script.enabled = true;
            }
        }
        reenableList.Clear();

        Cursor.lockState = previousLockMode;
        Cursor.visible = previousCursorVisible;

        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    public void ShowNextNote()
    {
        if (NoteManager.Instance == null || NoteManager.Instance.CollectedCount == 0) return;

        currentNoteIndex = (currentNoteIndex + 1) % NoteManager.Instance.CollectedCount;
        NoteManager.Instance.PlayRandomPageFlipSound();
        RefreshNoteViewer();
    }

    public void ShowPreviousNote()
    {
        if (NoteManager.Instance == null || NoteManager.Instance.CollectedCount == 0) return;

        currentNoteIndex--;
        if (currentNoteIndex < 0)
        {
            currentNoteIndex = NoteManager.Instance.CollectedCount - 1;
        }
        NoteManager.Instance.PlayRandomPageFlipSound();
        RefreshNoteViewer();
    }

    private void RefreshNoteViewer()
    {
        int totalNotes = NoteManager.Instance != null ? NoteManager.Instance.CollectedCount : 0;
        bool hasNotes = totalNotes > 0;

        if (emptyNotesPlaceholder != null)
        {
            emptyNotesPlaceholder.SetActive(!hasNotes);
        }

        if (pauseNoteDisplayImage != null)
        {
            pauseNoteDisplayImage.gameObject.SetActive(hasNotes);
        }

        if (nextNoteButton != null) nextNoteButton.interactable = totalNotes > 1;
        if (prevNoteButton != null) prevNoteButton.interactable = totalNotes > 1;

        if (hasNotes)
        {
            NoteManager.NoteEntry entry = NoteManager.Instance.GetNoteAt(currentNoteIndex);
            if (entry != null && pauseNoteDisplayImage != null)
            {
                pauseNoteDisplayImage.texture = entry.noteTexture;

                if (pauseNoteDisplayImage.TryGetComponent<AspectRatioFitter>(out var fitter) && entry.noteTexture.height > 0)
                {
                    fitter.aspectRatio = (float)entry.noteTexture.width / entry.noteTexture.height;
                }
            }

            if (noteInfoText != null && entry != null)
            {
                noteInfoText.text = $"{entry.noteTitle} ({currentNoteIndex + 1}/{totalNotes})";
            }
        }
        else if (noteInfoText != null)
        {
            noteInfoText.text = "No Notes Collected";
        }
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}