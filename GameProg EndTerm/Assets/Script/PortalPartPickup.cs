using UnityEngine;

public class PortalPartPickup : MonoBehaviour
{
    public enum PortalPartType
    {
        Part1,
        Part2,
        Part3
    }

    [Header("Part Settings")]
    public PortalPartType partType = PortalPartType.Part1;
    [Tooltip("Maximum distance from the camera to left-click and pick up this part.")]
    public float interactRange = 5f;

    private bool isCollected = false;
    public bool IsCollected => isCollected;

    private void Update()
    {
        if (isCollected) return;

        // Ignore clicks while paused or reading a note popup
        if (Time.timeScale == 0f || (NoteManager.Instance != null && NoteManager.Instance.IsReadingPopup))
            return;

        if (Input.GetMouseButtonDown(0))
        {
            CheckLeftClickRaycast();
        }
    }

    private void CheckLeftClickRaycast()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactRange, ~0, QueryTriggerInteraction.Collide);

        PortalPartPickup closestPart = null;
        float closestDistance = Mathf.Infinity;

        foreach (RaycastHit hit in hits)
        {
            PortalPartPickup part = hit.collider.GetComponentInParent<PortalPartPickup>();
            if (part != null && !part.IsCollected && hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestPart = part;
            }
        }

        if (closestPart == this)
        {
            Collect();
        }
    }

    public void Collect()
    {
        if (isCollected) return;
        isCollected = true;

        if (PortalPartManager.Instance != null)
        {
            PortalPartManager.Instance.CollectPart(partType);
        }
        else
        {
            Debug.LogWarning("[PortalPartPickup] No PortalPartManager found in the scene!");
        }

        gameObject.SetActive(false);
    }
}