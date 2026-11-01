using UnityEngine;
using TMPro;

public class ResourceTracker : MonoBehaviour
{
    [Header("Inventory")]
    public int leaves = 0;
    public int coal = 0;

    [Header("UI Text References")]
    public TextMeshProUGUI leafCounterText;
    public TextMeshProUGUI coalCounterText;

    [Header("Fuel Values")]
    public float leafEnergyValue = 5f; 
    public float coalEnergyValue = 20f; 

    private RobotEnergy energySystem;

    private void Start()
    {

        energySystem = GetComponent<RobotEnergy>();
        UpdateIcons();
    }

    private void Update()
    {
        if (energySystem == null) return;
        if (energySystem.currentEnergy >= energySystem.maxEnergy) return;


        if (Input.GetKeyDown(KeyCode.Alpha1) && leaves > 0)
        {
            leaves--;                                  
            energySystem.RestoreEnergy(leafEnergyValue);
            UpdateIcons();                             
        }


        if (Input.GetKeyDown(KeyCode.Alpha2) && coal > 0)
        {
            coal--;                                   
            energySystem.RestoreEnergy(coalEnergyValue);
            UpdateIcons();                             
        }
    }

    public void AddResource(string type, int amount)
    {
        if (type == "Leaf") leaves += amount;
        if (type == "Coal") coal += amount;

        UpdateIcons();
    }

    private void UpdateIcons()
    {
        if (leafCounterText != null) leafCounterText.text = leaves.ToString();
        if (coalCounterText != null) coalCounterText.text = coal.ToString();
    }
}