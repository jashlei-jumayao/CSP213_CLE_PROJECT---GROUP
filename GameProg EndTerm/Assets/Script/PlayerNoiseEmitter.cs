using UnityEngine;

public class PlayerNoiseEmitter : MonoBehaviour
{
    [Header("References")]
    public Transform noiseOrigin;

    [Header("Noise Radiuses (Drag sliders to adjust)")]
    [Range(1f, 50f)] public float rotateNoise = 8f;
    [Range(1f, 50f)] public float moveNoise = 15f;
    [Range(1f, 100f)] public float sprintNoise = 25f;
    [Range(1f, 100f)] public float fuelBurnNoise = 30f;

    [Header("Settings")]
    public float noiseInterval = 0.5f;

    [Header("Edit Mode Preview (Static)")]
    public bool showPreviewInEditor = true;
    public Color rotatePreviewColor = Color.cyan;
    public Color movePreviewColor = Color.green;
    public Color sprintPreviewColor = Color.yellow;
    public Color fuelPreviewColor = Color.red;

    private float nextNoiseTime = 0f;

    private void Update()
    {
        if (Time.time >= nextNoiseTime)
        {
            float currentNoise = 0f;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
            {
                currentNoise = Input.GetKey(KeyCode.LeftShift) ? sprintNoise : moveNoise;
            }
            else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
            {
                currentNoise = rotateNoise;
            }

            if (currentNoise > 0)
            {
                EmitNoise(currentNoise);
                nextNoiseTime = Time.time + noiseInterval;
            }
        }
    }

    public void EmitFuelNoise()
    {
        EmitNoise(fuelBurnNoise);
    }

    private void EmitNoise(float radius)
    {
        Vector3 noisePosition = (noiseOrigin != null) ? noiseOrigin.position : transform.position;

        Collider[] hitColliders = Physics.OverlapSphere(noisePosition, radius);
        foreach (Collider hit in hitColliders)
        {
            if (hit.CompareTag("Monster"))
            {
                // Grabs the script directly and passes BOTH the location and the radius
                if (hit.TryGetComponent<MonsterAI>(out MonsterAI monsterAI))
                {
                    monsterAI.InvestigateSound(noisePosition, radius);
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
        }
    }
}