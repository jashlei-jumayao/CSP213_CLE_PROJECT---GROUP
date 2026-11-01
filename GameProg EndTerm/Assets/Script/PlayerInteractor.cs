using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 5f;      
    public ResourceTracker robotInventory; 

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();

       
        if (robotInventory == null)
        {
            robotInventory = GetComponentInParent<ResourceTracker>();
        }
    }

    private void Update()
    {
       
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
        {
            InteractWithObject();
        }
    }

    private void InteractWithObject()
    {
       
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        
        if (Physics.Raycast(ray, out hit, interactRange))
        {
         
            ResourcePickup targetItem = hit.collider.GetComponent<ResourcePickup>();

            if (targetItem != null)
            {
               
                targetItem.Collect(robotInventory);
            }
        }
    }
}