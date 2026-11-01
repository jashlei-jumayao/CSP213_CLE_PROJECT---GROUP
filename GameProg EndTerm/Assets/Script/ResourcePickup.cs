using UnityEngine;

public class ResourcePickup : MonoBehaviour
{
    public enum ResourceType { Leaf, Coal }
    public ResourceType resourceType;
    public int amount = 1;

    public void Collect(ResourceTracker tracker)
    {
        tracker.AddResource(resourceType.ToString(), amount);
        Destroy(gameObject);
    }
}