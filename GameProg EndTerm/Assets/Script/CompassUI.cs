using UnityEngine;
using UnityEngine.UI;

public class CompassUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player or camera transform to track heading for")]
    public Transform playerTransform;
    [Tooltip("The RawImage or Image component of the sliding compass strip")]
    public RectTransform compassStrip;

    [Header("Settings")]
    [Tooltip("Multiplier to control how fast the compass strip moves as you rotate")]
    public float pixelPerDegree = 3.5f;

    private void Start()
    {
        // Safe fallback if player is not assigned
        if (playerTransform == null && Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null || compassStrip == null) return;

        // Get the player's Y-axis rotation angle (0 to 360 degrees)
        float currentYaw = playerTransform.eulerAngles.y;

        // Calculate the offset position based on rotation
        // Multiplying by pixelPerDegree aligns the movement with rotation speed
        float newXPosition = -currentYaw * pixelPerDegree;

        // Apply the horizontal movement to the compass strip
        compassStrip.anchoredPosition = new Vector2(newXPosition, compassStrip.anchoredPosition.y);
    }
}