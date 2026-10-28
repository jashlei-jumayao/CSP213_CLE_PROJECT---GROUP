using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Component References")]
    public Light robotFlashlight;
    public Camera robotCamera;
    public KeyCode toggleKey = KeyCode.F;

    [Header("Energy Settings")]
    public RobotEnergy energySystem;
    [Tooltip("Drain rate relative to normal movement (0.5 means half the energy drain of moving)")]
    public float flashlightDrainMultiplier = 0.5f;

    [Header("Vision Distances")]
    public float nearsightedDistance = 15f; // Camera clip plane when OFF
    public float flashlightDistance = 50f;  // Camera clip plane when ON

    private bool isFlashlightOn = false;

    private void Start()
    {
        // Auto-find references if unassigned
        if (robotCamera == null) robotCamera = GetComponent<Camera>();
        if (energySystem == null) energySystem = GetComponentInParent<RobotEnergy>();

        // Set default starting state (off and nearsighted)
        SetFlashlightState(false);
    }

    private void Update()
    {
        // --- Toggle Input ---
        if (Input.GetKeyDown(toggleKey))
        {
            // Block turning ON if out of energy
            if (!isFlashlightOn && energySystem != null && !energySystem.HasEnergy())
            {
                return;
            }

            SetFlashlightState(!isFlashlightOn);
        }

        // --- Active Energy Drain ---
        if (isFlashlightOn && energySystem != null)
        {
            if (energySystem.HasEnergy())
            {
                // Consume energy while light is active
                energySystem.ConsumeEnergy(flashlightDrainMultiplier);
            }
            else
            {
                // Shut off and return to nearsighted vision when energy hits 0
                SetFlashlightState(false);
            }
        }
    }

    private void SetFlashlightState(bool state)
    {
        isFlashlightOn = state;

        // Toggle spotlight
        if (robotFlashlight != null)
        {
            robotFlashlight.enabled = isFlashlightOn;
        }

        // Extend or shrink camera rendering distance
        if (robotCamera != null)
        {
            robotCamera.farClipPlane = isFlashlightOn ? flashlightDistance : nearsightedDistance;
        }
    }
}