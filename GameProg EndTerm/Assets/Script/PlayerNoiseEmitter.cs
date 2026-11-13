using UnityEngine;

public class PlayerNoiseEmitter : MonoBehaviour
{
    [Header("References")]
    public Transform noiseOrigin;

    [Header("Noise Radiuses (How far the sound travels)")]
    public float rotateNoise = 8f;
    public float moveNoise = 15f;
    public float sprintNoise = 25f;
    public float fuelBurnNoise = 30f;
    public float recoveryNoise = 20f;

    [Header("Suspicion Values (How much it fills the meter)")]
    public float rotateSuspicion = 5f;
    public float moveSuspicion = 10f;
    public float sprintSuspicion = 25f;
    public float fuelBurnSuspicion = 40f;
    public float recoverySuspicion = 15f;

    [Header("Settings")]
    public float noiseInterval = 0.5f;

    [Header("Edit Mode Preview (Static)")]
    public bool showPreviewInEditor = true;
    public Color rotatePreviewColor = Color.cyan;
    public Color movePreviewColor = Color.green;
    public Color sprintPreviewColor = Color.yellow;
    public Color fuelPreviewColor = Color.red;
    public Color recoveryPreviewColor = Color.magenta;

    private float nextNoiseTime = 0f;

    private void Update()
    {
        if (Time.time >= nextNoiseTime)
        {
            float currentRadius = 0f;
            float currentSuspicionAmount = 0f;

            // Determine both the radius and the suspicion amount based on player input
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    currentRadius = sprintNoise;
                    currentSuspicionAmount = sprintSuspicion;
                }
                else
                {
                    currentRadius = moveNoise;
                    currentSuspicionAmount = moveSuspicion;
                }
            }
            else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
            {
                currentRadius = rotateNoise;
                currentSuspicionAmount = rotateSuspicion;
            }

            if (currentRadius > 0)
            {
                EmitNoise(currentRadius, currentSuspicionAmount);
                nextNoiseTime = Time.time + noiseInterval;
            }
        }
    }

    public void EmitFuelNoise()
    {
        EmitNoise(fuelBurnNoise, fuelBurnSuspicion);
    }

    public void EmitRecoveryNoise()
    {
        EmitNoise(recoveryNoise, recoverySuspicion);
    }

    // --- NEW: Now requires both radius and suspicionAmount ---
    private void EmitNoise(float radius, float suspicionAmount)
    {
        Vector3 noisePosition = (noiseOrigin != null) ? noiseOrigin.position : transform.position;

        Collider[] hitColliders = Physics.OverlapSphere(noisePosition, radius);
        foreach (Collider hit in hitColliders)
        {
            if (hit.CompareTag("Monster"))
            {
                if (hit.TryGetComponent<MonsterAI>(out MonsterAI monsterAI))
                {
                    // Passes the separated suspicion amount to the monster
                    monsterAI.InvestigateSound(noisePosition, radius, suspicionAmount);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying && showPreviewInEditor)
        {
            Vector3 center = (noiseOrigin != null) ? noiseOrigin.position : transform.position;

            Gizmos.color = rotatePreviewColor;
            Gizmos.DrawWireSphere(center, rotateNoise);

            Gizmos.color = movePreviewColor;
            Gizmos.DrawWireSphere(center, moveNoise);

            Gizmos.color = sprintPreviewColor;
            Gizmos.DrawWireSphere(center, sprintNoise);

            Gizmos.color = fuelPreviewColor;
            Gizmos.DrawWireSphere(center, fuelBurnNoise);

            Gizmos.color = recoveryPreviewColor;
            Gizmos.DrawWireSphere(center, recoveryNoise);
        }
    }
}