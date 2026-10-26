using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 5f;        // How far the robot can reach
    public ResourceTracker robotInventory;  // Reference to the inventory script

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();

        // Auto-find the inventory script on the robot body
        if (robotInventory == null)
        {
            robotInventory = GetComponentInParent<ResourceTracker>();
        }
    }

    private void Update()
    {
        // Fire interaction ray when Left Mouse Button (0) or 'E' is pressed
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
        {
            InteractWithObject();
        }
    }

    private void InteractWithObject()
    {
        // Shoot a ray exactly from the center of the camera forward
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        // If the ray hits something within our interaction range
        if (Physics.Raycast(ray, out hit, interactRange))
        {
            // Check if the object we hit has the ResourcePickup script
            ResourcePickup targetItem = hit.collider.GetComponent<ResourcePickup>();

            if (targetItem != null)
            {
                // We hit an item! Tell it to collect itself and send it our inventory tracker
                targetItem.Collect(robotInventory);
            }
        }
    }
}