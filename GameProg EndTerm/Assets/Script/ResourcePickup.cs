using UnityEngine;

public class ResourcePickup : MonoBehaviour
{
    public enum ResourceType { Leaf, Coal }
    public ResourceType resourceType;

    [Header("Amount Settings")]
    [Tooltip("Check this to give a random amount within the Min/Max range.")]
    public bool useRandomAmount = false;

    [Tooltip("The exact amount given if 'Use Random Amount' is unchecked.")]
    public int fixedAmount = 1;

    [Tooltip("The minimum amount given if 'Use Random Amount' is checked.")]
    public int minAmount = 1;

    [Tooltip("The maximum amount given if 'Use Random Amount' is checked.")]
    public int maxAmount = 3;

    public void Collect(ResourceTracker tracker)
    {
        int finalAmount = fixedAmount;

        if (useRandomAmount)
        {
            // Note: Random.Range with integers is exclusive on the max value, 
            // so we add 1 to ensure the maxAmount can actually be rolled.
            finalAmount = Random.Range(minAmount, maxAmount + 1);
        }

        tracker.AddResource(resourceType.ToString(), finalAmount);
        Destroy(gameObject);
    }
}