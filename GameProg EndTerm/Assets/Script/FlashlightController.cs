using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Flashlight Settings")]
    public Light robotFlashlight;         // Drag your Spotlight here
    public KeyCode toggleKey = KeyCode.F; // The key to press

    private bool isFlashlightOn = false;

    private void Start()
    {
        // Start with the flashlight turned off
        if (robotFlashlight != null)
        {
            robotFlashlight.enabled = isFlashlightOn;
        }
    }

    private void Update()
    {
        // Check if the F key was pressed this frame
        if (Input.GetKeyDown(toggleKey) && robotFlashlight != null)
        {
            // Toggle the state (if on, turn off. If off, turn on)
            isFlashlightOn = !isFlashlightOn;

            // Apply it to the actual light component
            robotFlashlight.enabled = isFlashlightOn;
        }
    }
}