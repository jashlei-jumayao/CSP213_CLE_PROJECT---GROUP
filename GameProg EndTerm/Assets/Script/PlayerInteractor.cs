using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("How far the camera's aiming laser reaches")]
    public float interactRange = 5f;
    public ResourceTracker robotInventory;

    [Header("References")]
    public Camera mainCamera;

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (robotInventory == null)
        {
            robotInventory = GetComponent<ResourceTracker>();
            if (robotInventory == null)
                robotInventory = GetComponentInParent<ResourceTracker>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            InteractWithObject();
        }
    }

    private void InteractWithObject()
    {
        if (mainCamera == null) return;

        // Shoot a ray exactly where the center of the camera is looking
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);

        // RaycastAll shoots through EVERYTHING in its path up to the interactRange
        RaycastHit[] hits = Physics.RaycastAll(ray, interactRange, ~0, QueryTriggerInteraction.Collide);

        ResourcePickup closestItem = null;
        float closestDistance = Mathf.Infinity;

        // Check every single object the ray pierced through
        foreach (RaycastHit hit in hits)
        {
            // See if the pierced object is a ResourcePickup
            ResourcePickup item = hit.collider.GetComponentInParent<ResourcePickup>();

            if (item != null)
            {
                // If the ray hit multiple items, only pick up the closest one
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestItem = item;
                }
            }
        }

        // Collect the item we successfully aimed at
        if (closestItem != null)
        {
            closestItem.Collect(robotInventory);
        }
    }
}