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

    [Header("UI References")]
    public TextMeshProUGUI energyText;

    private bool isDepleted = false;
    private float emptyTimer = 0f;

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
                isDepleted = true;
                emptyTimer = 0f;
            }

            UpdateEnergyUI();
        }
        else if (isDepleted)
        {
            emptyTimer += Time.deltaTime;

            if (emptyTimer >= recoveryDelay)
            {
                currentEnergy += recoveryRate * Time.deltaTime;

                if (currentEnergy >= maxEnergy)
                {
                    currentEnergy = maxEnergy;
                    isDepleted = false;
                }

                UpdateEnergyUI();
            }
        }
    }

    public bool HasEnergy()
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
                isDepleted = true;
                emptyTimer = 0f;
            }

            UpdateEnergyUI();

        }
    }

    public void RestoreEnergy(float amount)
    {
        currentEnergy += amount;

        if (currentEnergy >= maxEnergy)
        {
            currentEnergy = maxEnergy;
            isDepleted = false;
            emptyTimer = 0f;
        }

        UpdateEnergyUI();

        if (TryGetComponent<PlayerNoiseEmitter>(out var noiseEmitter))
        {
            noiseEmitter.EmitFuelNoise();
        }
    }

    public void UpdateEnergyUI()
    {
        if (energyText != null)
        {
            energyText.text = "Energy: " + Mathf.CeilToInt(currentEnergy).ToString();
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
                isDepleted = true;
                emptyTimer = 0f;
            }

            UpdateEnergyUI();
        }
    }
}