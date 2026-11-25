using UnityEngine;

public class CollectibleNote : MonoBehaviour
{
    [Header("Note Data")]
    public string noteTitle = "Torn Page";
    [Tooltip("Drag your PNG/JPG Texture directly here.")]
    public Texture noteImage;

    [Header("UI Prompt (Optional)")]
    [Tooltip("World-space prompt icon/text ('Press E / Click') shown when looking directly at the note.")]
    public GameObject interactPromptUI;

    private bool isCollected = false;
    public bool IsCollected => isCollected;

    private void Start()
    {
        if (interactPromptUI != null)
        {
            interactPromptUI.SetActive(false);
        }
    }

    public void SetPromptVisible(bool visible)
    {
        if (interactPromptUI != null && !isCollected && interactPromptUI.activeSelf != visible)
        {
            interactPromptUI.SetActive(visible);
        }
    }

    public void Collect()
    {
        if (isCollected) return;
        isCollected = true;

        if (interactPromptUI != null)
        {
            interactPromptUI.SetActive(false);
        }

        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.CollectNote(noteTitle, noteImage);
        }
        else
        {
            Debug.LogWarning("[CollectibleNote] No NoteManager found in the scene!");
        }

        gameObject.SetActive(false);
    }
}