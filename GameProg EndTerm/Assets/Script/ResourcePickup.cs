using UnityEngine;

public class ResourcePickup : MonoBehaviour
{
    public enum ResourceType { Leaf, Coal }
    public ResourceType resourceType;
    public int amount = 1;

    // This is now called by the Camera when you click/press E
    public void Collect(ResourceTracker tracker)
    {
        tracker.AddResource(resourceType.ToString(), amount);
        Destroy(gameObject);
    }
}