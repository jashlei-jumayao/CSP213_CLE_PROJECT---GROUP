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

    [Header("Distances & Forward Arc")]
    public float minSpawnRadius = 20f;
    public float maxSpawnRadius = 45f;
    public float despawnRadius = 60f;
    [Tooltip("Cone angle in front of the player where items can spawn (e.g., 120 = 60° left and right of forward).")]
    [Range(10f, 180f)]
    public float spawnArcAngle = 120f;
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
        // Project player forward onto the horizontal XZ plane
        Vector3 flatForward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;

        // Pick a random direction within the forward arc
        float halfArc = spawnArcAngle * 0.5f;
        float randomAngle = Random.Range(-halfArc, halfArc);
        Vector3 spawnDirection = Quaternion.Euler(0f, randomAngle, 0f) * flatForward;

        float randomDistance = Random.Range(minSpawnRadius, maxSpawnRadius);
        Vector3 randomPoint = player.position + spawnDirection * randomDistance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 5f, NavMesh.AllAreas))
        {
            Vector3 finalSpawnPosition = hit.position;

            // Verify NavMesh snapping didn't pull the spawn point behind the player
            Vector3 dirToHit = Vector3.ProjectOnPlane(finalSpawnPosition - player.position, Vector3.up).normalized;
            if (Vector3.Angle(flatForward, dirToHit) > halfArc)
            {
                return;
            }

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