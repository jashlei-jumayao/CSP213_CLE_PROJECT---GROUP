using UnityEngine;
using TMPro;

public class RobotEnergy : MonoBehaviour
{
    [Header("Energy Settings")]
    public float maxEnergy = 100f;
    public float currentEnergy;
    public float totalMovementTime = 30f;
    public float passiveDrainPerSecond = 0.3333f;

    [Header("Recovery Settings")]
    public float recoveryDelay = 10f;
    public float recoveryRate = 1f;
    [Tooltip("How often to emit noise to alert monsters while recovering (seconds)")]
    public float recoveryNoiseInterval = 0.5f;

    [Header("UI References")]
    public TextMeshProUGUI energyText;
    [Tooltip("Order from Empty to Full: [0] = 0% (Empty), [1] = 1-25%, [2] = 26-50%, [3] = 51-75%, [4] = 76-100%.")]
    public GameObject[] batteryStateImages;

    [Header("Audio References")]
    public AudioClip recoveryStartSound;
    [Tooltip("Sound played once when the battery completely dies and hits 0")]
    public AudioClip powerDownSound;

    private bool isDepleted = false;
    private float emptyTimer = 0f;
    private float noisePulseTimer = 0f;

    private void Start()
    {
        currentEnergy = maxEnergy;
        UpdateEnergyUI();
    }

    private void Update()
    {
        if (!isDepleted && currentEnergy > 0f)
        {
            currentEnergy -= passiveDrainPerSecond * Time.deltaTime;

            if (currentEnergy <= 0f)
            {
                currentEnergy = 0f;
                TriggerDepletion();
            }

            UpdateEnergyUI();
        }
        else if (isDepleted)
        {
            emptyTimer += Time.deltaTime;

            if (emptyTimer >= recoveryDelay)
            {
                if (currentEnergy == 0f && recoveryStartSound != null)
                {
                    AudioSource.PlayClipAtPoint(recoveryStartSound, transform.position, 1.0f);
                }

                currentEnergy += recoveryRate * Time.deltaTime;

                noisePulseTimer += Time.deltaTime;
                if (noisePulseTimer >= recoveryNoiseInterval)
                {
                    if (TryGetComponent<PlayerNoiseEmitter>(out var noiseEmitter))
                    {
                        noiseEmitter.EmitRecoveryNoise();
                    }
                    noisePulseTimer = 0f;
                }

                if (currentEnergy >= maxEnergy)
                {
                    currentEnergy = maxEnergy;
                    isDepleted = false;
                    noisePulseTimer = 0f;
                }

                UpdateEnergyUI();
            }
        }
    }

    private void TriggerDepletion()
    {
        if (!isDepleted)
        {
            isDepleted = true;
            emptyTimer = 0f;
            noisePulseTimer = 0f;

            if (powerDownSound != null)
            {
                AudioSource.PlayClipAtPoint(powerDownSound, transform.position, 1.0f);
            }
        }
    }

    public bool HasEnergy()
    {
        return !isDepleted;
    }

    public bool CanPerformActions()
    {
        return !isDepleted;
    }

    public void ConsumeEnergy(float drainMultiplier = 1f)
    {
        if (!isDepleted)
        {
            float drainRate = maxEnergy / totalMovementTime;

            currentEnergy -= (drainRate * drainMultiplier) * Time.deltaTime;

            if (currentEnergy <= 0f)
            {
                currentEnergy = 0f;
                TriggerDepletion();
            }

            UpdateEnergyUI();
        }
    }

    // Now returns true if energy was successfully restored, or false if blocked by shutdown
    public bool RestoreEnergy(float amount)
    {
        if (isDepleted) return false;

        currentEnergy += amount;

        if (currentEnergy >= maxEnergy)
        {
            currentEnergy = maxEnergy;
        }

        if (currentEnergy > 0f)
        {
            isDepleted = false;
            emptyTimer = 0f;
            noisePulseTimer = 0f;
        }

        UpdateEnergyUI();

        if (TryGetComponent<PlayerNoiseEmitter>(out var noiseEmitter))
        {
            noiseEmitter.EmitFuelNoise();
        }

        return true;
    }

    public void UpdateEnergyUI()
    {
        if (energyText != null)
        {
            energyText.text = "Energy: " + Mathf.CeilToInt(currentEnergy).ToString();
        }

        UpdateBatteryStateObjects();
    }

    private void UpdateBatteryStateObjects()
    {
        if (batteryStateImages == null || batteryStateImages.Length == 0) return;

        int activeIndex = 0;

        if (currentEnergy > 0f && batteryStateImages.Length > 1)
        {
            int chargedStates = batteryStateImages.Length - 1;
            float normalized = maxEnergy > 0f ? Mathf.Clamp01(currentEnergy / maxEnergy) : 0f;
            activeIndex = Mathf.Clamp(Mathf.CeilToInt(normalized * chargedStates), 1, chargedStates);
        }

        for (int i = 0; i < batteryStateImages.Length; i++)
        {
            if (batteryStateImages[i] != null)
            {
                bool shouldBeActive = (i == activeIndex);
                if (batteryStateImages[i].activeSelf != shouldBeActive)
                {
                    batteryStateImages[i].SetActive(shouldBeActive);
                }
            }
        }
    }

    public void DrainEnergy(float amount)
    {
        if (!isDepleted)
        {
            currentEnergy -= amount;

            if (currentEnergy <= 0f)
            {
                currentEnergy = 0f;
                TriggerDepletion();
            }

            UpdateEnergyUI();
        }
    }
}