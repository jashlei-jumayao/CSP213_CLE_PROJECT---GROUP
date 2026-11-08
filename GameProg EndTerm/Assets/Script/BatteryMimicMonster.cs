using UnityEngine;

public class MonsterDrainPlayer : MonoBehaviour
{
    [Header("Drain Settings")]
    [Tooltip("Percentage of max energy to drain (0.5 = 50%)")]
    [Range(0f, 1f)]
    public float drainPercentage = 0.5f;

    [Header("Post-Attack Settings")]
    [Tooltip("How long the monster ignores the player and walks away")]
    public float ignoreDuration = 5f;

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

            // 1. Drain Player Energy
            RobotEnergy energyScript = playerObj.GetComponent<RobotEnergy>();
            if (energyScript != null)
            {
                float drainAmount = energyScript.maxEnergy * drainPercentage;
                energyScript.DrainEnergy(drainAmount);
                Debug.Log("Monster drained " + drainAmount + " energy from player.");
            }

            // 2. Make the Monster lose interest and walk away
            if (monsterAI != null)
            {
                monsterAI.LoseInterestForSeconds(ignoreDuration);
                Debug.Log("Monster is satisfied and walking away for " + ignoreDuration + " seconds!");
            }

            nextTouchTime = Time.time + touchCooldown;
        }
    }
}