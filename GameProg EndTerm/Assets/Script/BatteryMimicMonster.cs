using UnityEngine;

public class MonsterDrainPlayer : MonoBehaviour
{
    [Header("Drain Settings")]
    [Tooltip("Percentage of max energy to drain (0.5 = 50%)")]
    [Range(0f, 1f)]
    public float drainPercentage = 0.5f;

    [Header("Monster Slowdown Settings")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.3f; // 0.3 means monster speed drops to 30% (heavy slug pace)
    public float slowDuration = 10f;    // How long the monster is slowed in seconds

    [Header("Cooldown to prevent instant multi-hits")]
    public float touchCooldown = 1.5f;
    private float nextTouchTime = 0f;

    private MonsterVisionAI monsterAI;

    private void Start()
    {
        // Find the MonsterVisionAI on this monster (looking up the parent hierarchy)
        monsterAI = GetComponentInParent<MonsterVisionAI>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < nextTouchTime) return;

        Transform rootTransform = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform.root;

        if (rootTransform.CompareTag("Player") || other.CompareTag("Player"))
        {
            GameObject playerObj = rootTransform.gameObject;

            // 1. Drain 50% Player Energy
            RobotEnergy energyScript = playerObj.GetComponent<RobotEnergy>();
            if (energyScript != null)
            {
                float drainAmount = energyScript.maxEnergy * drainPercentage;
                energyScript.DrainEnergy(drainAmount);
                Debug.Log("Monster drained " + drainAmount + " energy from player.");
            }

            // 2. Slow down the Monster for 10 seconds
            if (monsterAI != null)
            {
                monsterAI.ApplySlowdown(slowMultiplier, slowDuration);
                Debug.Log("Monster applied 10s slowdown to itself!");
            }

            nextTouchTime = Time.time + touchCooldown;
        }
    }
}