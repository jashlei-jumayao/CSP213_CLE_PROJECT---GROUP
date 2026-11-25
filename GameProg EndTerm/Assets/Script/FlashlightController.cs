using UnityEngine;
using UnityEngine.UI;

public class FlashlightController : MonoBehaviour
{
    [Header("Component References")]
    public Light robotFlashlight;
    public Camera robotCamera;
    public KeyCode toggleKey = KeyCode.F;

    [Header("Energy Settings")]
    public RobotEnergy energySystem;
    [Tooltip("Flat amount of energy consumed immediately each time the flashlight is turned ON.")]
    public float turnOnEnergyCost = 2f;
    [Tooltip("Continuous drain rate relative to normal movement while the flashlight stays ON.")]
    public float flashlightDrainMultiplier = 0.5f;

    [Header("Vision Distances")]
    public float nearsightedDistance = 15f;
    public float flashlightDistance = 50f;

    [Header("Flashlight Exposure Meter")]
    [Tooltip("Auto-detected from the player root if left empty.")]
    public VisionMonsterLightSpawner lightSpawner;
    [Tooltip("Fallback meter settings used only if no VisionMonsterLightSpawner is in the scene.")]
    public float maxMeter = 100f;
    public float meterBuildupPerSecond = 12f;
    public float toggleMeterBurst = 4f;
    public float meterDecayPerSecond = 5f;
    [Range(0f, 100f)]
    public float currentMeterPercent = 0f;

    [Header("Meter UI (Optional)")]
    [Tooltip("Optional parent UI panel/object for the flashlight meter.")]
    public GameObject flashlightMeterUI;
    public Slider flashlightMeterSlider;
    public Image flashlightMeterFill;
    public Gradient meterColorGradient;
    public bool hideMeterWhenEmpty = true;

    [Header("Audio")]
    [Tooltip("Sound clips played at random when turning the flashlight ON.")]
    public AudioClip[] turnOnSounds;
    [Tooltip("Sound clips played at random when turning the flashlight OFF.")]
    public AudioClip[] turnOffSounds;
    [Range(0f, 1f)]
    public float toggleVolume = 1f;
    [Range(0.5f, 2f)]
    public float minPitch = 0.95f;
    [Range(0.5f, 2f)]
    public float maxPitch = 1.15f;
    [Tooltip("Optional: Assign a dedicated AudioSource, or leave empty to auto-create one.")]
    public AudioSource flashlightAudioSource;

    private bool isFlashlightOn = false;
    private float fallbackMeterValue = 0f;

    public bool IsFlashlightOn => isFlashlightOn && (robotFlashlight == null || robotFlashlight.cullingMask != 0);

    private void Start()
    {
        if (robotCamera == null) robotCamera = GetComponent<Camera>();
        if (energySystem == null) energySystem = GetComponentInParent<RobotEnergy>();
        if (lightSpawner == null) lightSpawner = GetComponentInParent<VisionMonsterLightSpawner>();

        if (flashlightAudioSource == null)
        {
            flashlightAudioSource = gameObject.AddComponent<AudioSource>();
            flashlightAudioSource.playOnAwake = false;
            flashlightAudioSource.spatialBlend = 0f;
        }

        // Initialize silently without playing a toggle sound or draining energy on scene load
        SetFlashlightState(false, false);
        UpdateMeterUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (!isFlashlightOn && energySystem != null && !energySystem.HasEnergy())
            {
                return;
            }

            SetFlashlightState(!isFlashlightOn, true);
        }

        if (isFlashlightOn && energySystem != null)
        {
            if (energySystem.HasEnergy())
            {
                energySystem.ConsumeEnergy(flashlightDrainMultiplier);
            }
            else
            {
                SetFlashlightState(false, true);
            }
        }

        UpdateFlashlightMeter();
    }

    private void UpdateFlashlightMeter()
    {
        if (lightSpawner != null)
        {
            float threshold = Mathf.Max(0.01f, lightSpawner.spawnLightThreshold);
            currentMeterPercent = Mathf.Clamp((lightSpawner.currentLightExposure / threshold) * 100f, 0f, 100f);
        }
        else
        {
            if (IsFlashlightOn)
            {
                fallbackMeterValue = Mathf.Min(maxMeter, fallbackMeterValue + meterBuildupPerSecond * Time.deltaTime);
            }
            else if (fallbackMeterValue > 0f)
            {
                fallbackMeterValue = Mathf.Max(0f, fallbackMeterValue - meterDecayPerSecond * Time.deltaTime);
            }

            currentMeterPercent = maxMeter > 0f ? Mathf.Clamp((fallbackMeterValue / maxMeter) * 100f, 0f, 100f) : 0f;
        }

        UpdateMeterUI();
    }

    private void UpdateMeterUI()
    {
        if (flashlightMeterUI != null && hideMeterWhenEmpty)
        {
            bool shouldShow = isFlashlightOn || currentMeterPercent > 0.1f;
            if (flashlightMeterUI.activeSelf != shouldShow)
            {
                flashlightMeterUI.SetActive(shouldShow);
            }
        }

        if (flashlightMeterSlider != null)
        {
            flashlightMeterSlider.minValue = 0f;
            flashlightMeterSlider.maxValue = 100f;
            flashlightMeterSlider.value = currentMeterPercent;
        }

        if (flashlightMeterFill != null && meterColorGradient != null)
        {
            flashlightMeterFill.color = meterColorGradient.Evaluate(currentMeterPercent / 100f);
        }
    }

    private void SetFlashlightState(bool state, bool playAudio)
    {
        bool stateChanged = isFlashlightOn != state;
        isFlashlightOn = state;

        // Deduct the flat turn-on energy cost when switching the flashlight ON
        if (stateChanged && isFlashlightOn && energySystem != null && turnOnEnergyCost > 0f)
        {
            energySystem.DrainEnergy(turnOnEnergyCost);

            // If that 2-energy cost depleted the battery to 0, shut the flashlight back off immediately
            if (!energySystem.HasEnergy())
            {
                isFlashlightOn = false;
            }
        }

        if (robotFlashlight != null)
        {
            robotFlashlight.enabled = isFlashlightOn;
        }

        if (robotCamera != null)
        {
            robotCamera.farClipPlane = isFlashlightOn ? flashlightDistance : nearsightedDistance;
        }

        if (stateChanged && isFlashlightOn && lightSpawner == null)
        {
            fallbackMeterValue = Mathf.Min(maxMeter, fallbackMeterValue + toggleMeterBurst);
        }

        if (playAudio && stateChanged)
        {
            PlayRandomToggleSound(isFlashlightOn ? turnOnSounds : turnOffSounds);
        }
    }

    private void PlayRandomToggleSound(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || flashlightAudioSource == null) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null)
        {
            float low = Mathf.Min(minPitch, maxPitch);
            float high = Mathf.Max(minPitch, maxPitch);
            flashlightAudioSource.pitch = Random.Range(low, high);
            flashlightAudioSource.PlayOneShot(clip, toggleVolume);
        }
    }
}