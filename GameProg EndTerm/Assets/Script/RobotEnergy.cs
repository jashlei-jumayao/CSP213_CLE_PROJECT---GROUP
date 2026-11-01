using UnityEngine;
using TMPro;

public class RobotEnergy : MonoBehaviour
{
    [Header("Energy Settings")]
    public float maxEnergy = 100f;
    public float currentEnergy;
    public float totalMovementTime = 30f;

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
       
        if (isDepleted)
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
        return currentEnergy > 0f;
    }

    public void ConsumeEnergy(float drainMultiplier = 1f)
    {
        if (currentEnergy > 0f)
        {
          
            isDepleted = false;
            emptyTimer = 0f;

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
        if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;

       
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