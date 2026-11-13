using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[System.Serializable]
public class ResourceDrop
{
    public string name;
    public GameObject prefab;

    [Tooltip("Higher number = spawns more often. E.g., Leaf = 100, Coal = 10")]
    public int spawnWeight = 10;
}

public class DynamicResourceSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public ResourceDrop[] availableResources;

    [Header("Spawn Limits")]
    public int maxResourcesInWorld = 15;
    public float spawnInterval = 1.0f;

    [Header("Distances & Spacing")]
    public float minSpawnRadius = 20f;
    public float maxSpawnRadius = 45f;
    public float despawnRadius = 60f;
    [Tooltip("Minimum physical distance between spawned items")]
    public float minDistanceBetweenItems = 5f;

    [Header("Spawn Height Adjustment")]
    [Tooltip("Adjust this to raise or lower items along the Y-axis so they don't spawn buried in the ground")]
    public float spawnYOffset = 0.5f;

    [Header("Collision Checks")]
    public float itemRadius = 1.0f;
    public LayerMask obstacleLayers;

    private List<GameObject> activeResources = new List<GameObject>();
    private float timer = 0f;

    private void Update()
    {
        if (player == null) return;

        CleanupFarResources();

        if (activeResources.Count < maxResourcesInWorld)
        {
            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                TrySpawnResource();
                timer = 0f;
            }
        }
    }

    private void TrySpawnResource()
    {
        Vector2 randomCircle = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(minSpawnRadius, maxSpawnRadius);
        Vector3 randomPoint = player.position + new Vector3(randomCircle.x, 0, randomCircle.y) * randomDistance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 5f, NavMesh.AllAreas))
        {
            Vector3 finalSpawnPosition = hit.position;

            // Spacing Check: Ensure it is not too close to other spawned resources
            foreach (GameObject resource in activeResources)
            {
                if (resource != null)
                {
                    float distanceToOther = Vector3.Distance(finalSpawnPosition, resource.transform.position);
                    if (distanceToOther < minDistanceBetweenItems)
                    {
                        return; // Abort spawn, too close to another item
                    }
                }
            }

            // Obstacle Check: Ensure no physical colliders are overlapping
            if (!Physics.CheckSphere(finalSpawnPosition, itemRadius, obstacleLayers))
            {
                // Adds the adjustable Y-axis offset to the final spawn position
                Spawn(finalSpawnPosition + (Vector3.up * spawnYOffset));
            }
        }
    }

    private void Spawn(Vector3 position)
    {
        if (availableResources.Length == 0) return;

        int totalWeight = 0;
        foreach (ResourceDrop drop in availableResources)
        {
            totalWeight += drop.spawnWeight;
        }

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;
        GameObject prefabToSpawn = null;

        foreach (ResourceDrop drop in availableResources)
        {
            currentWeight += drop.spawnWeight;
            if (randomValue < currentWeight)
            {
                prefabToSpawn = drop.prefab;
                break;
            }
        }

        if (prefabToSpawn != null)
        {
            GameObject newResource = Instantiate(prefabToSpawn, position, Quaternion.identity);
            activeResources.Add(newResource);
        }
    }

    private void CleanupFarResources()
    {
        for (int i = activeResources.Count - 1; i >= 0; i--)
        {
            GameObject resource = activeResources[i];

            if (resource == null)
            {
                activeResources.RemoveAt(i);
                continue;
            }

            float distanceToPlayer = Vector3.Distance(player.position, resource.transform.position);
            if (distanceToPlayer > despawnRadius)
            {
                Destroy(resource);
                activeResources.RemoveAt(i);
            }
        }
    }
}