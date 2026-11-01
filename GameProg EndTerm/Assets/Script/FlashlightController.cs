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
    public float nearsightedDistance = 15f; 
    public float flashlightDistance = 50f;

    private bool isFlashlightOn = false;

    private void Start()
    {
      
        if (robotCamera == null) robotCamera = GetComponent<Camera>();
        if (energySystem == null) energySystem = GetComponentInParent<RobotEnergy>();

       
        SetFlashlightState(false);
    }

    private void Update()
    {
       
        if (Input.GetKeyDown(toggleKey))
        {
           
            if (!isFlashlightOn && energySystem != null && !energySystem.HasEnergy())
            {
                return;
            }

            SetFlashlightState(!isFlashlightOn);
        }


        if (isFlashlightOn && energySystem != null)
        {
            if (energySystem.HasEnergy())
            { 
                energySystem.ConsumeEnergy(flashlightDrainMultiplier);
            }
            else
            {
                
                SetFlashlightState(false);
            }
        }
    }

    private void SetFlashlightState(bool state)
    {
        isFlashlightOn = state;


        if (robotFlashlight != null)
        {
            robotFlashlight.enabled = isFlashlightOn;
        }


        if (robotCamera != null)
        {
            robotCamera.farClipPlane = isFlashlightOn ? flashlightDistance : nearsightedDistance;
        }
    }
}