using UnityEngine;
using TMPro;

public class RobotEnergy : MonoBehaviour
{
    [Header("Energy Settings")]
    public float maxEnergy = 100f;
    public float currentEnergy;
    public float totalMovementTime = 30f;

    [Header("Recovery Settings")]
    public float recoveryDelay = 10f;     // How long to wait after hitting 0
    public float recoveryRate = 1f;       // Energy restored per second after the delay

    [Header("UI References")]
    public TextMeshProUGUI energyText;

    private bool isDepleted = false;      // Tracks if we are currently waiting/recovering
    private float emptyTimer = 0f;        // Tracks how many seconds have passed since hitting 0

    private void Start()
    {
        currentEnergy = maxEnergy;
        UpdateEnergyUI();
    }

    private void Update()
    {
        // If energy hit 0, start the recovery sequence
        if (isDepleted)
        {
            emptyTimer += Time.deltaTime;

            // Once 10 seconds have passed, start adding energy back
            if (emptyTimer >= recoveryDelay)
            {
                currentEnergy += recoveryRate * Time.deltaTime;

                // Stop recovering if it reaches max
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
        return currentEnergy > 0f;
    }

    public void ConsumeEnergy(float drainMultiplier = 1f)
    {
        if (currentEnergy > 0f)
        {
            // If the player starts driving while recovering, cancel the recovery state
            isDepleted = false;
            emptyTimer = 0f;

            float drainRate = maxEnergy / totalMovementTime;

            // Multiply the drain rate if we are sprinting
            currentEnergy -= (drainRate * drainMultiplier) * Time.deltaTime;

            // If we just hit 0, trigger the depleted state to start the 10-second timer
            if (currentEnergy <= 0f)
            {
                currentEnergy = 0f;
                isDepleted = true;
                emptyTimer = 0f; // Reset the timer so it starts counting from 0 exactly now
            }

            UpdateEnergyUI();
        }
    }

    public void RestoreEnergy(float amount)
    {
        currentEnergy += amount;
        if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;

        // If the player burns fuel, they are no longer depleted
        isDepleted = false;
        emptyTimer = 0f;

        UpdateEnergyUI();
    }

    public void UpdateEnergyUI()
    {
        if (energyText != null)
        {
            energyText.text = "Energy: " + Mathf.CeilToInt(currentEnergy).ToString();
        }
    }
}