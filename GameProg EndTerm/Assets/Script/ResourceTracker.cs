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
    public float leafEnergyValue = 5f;  // Leaves give a little energy
    public float coalEnergyValue = 20f;  // Coal gives a lot of energy

    private RobotEnergy energySystem;

    private void Start()
    {
        // Auto-find the energy script on the robot
        energySystem = GetComponent<RobotEnergy>();
        UpdateIcons();
    }

    private void Update()
    {
        if (energySystem == null) return;

        // Prevent burning fuel if energy is already full
        if (energySystem.currentEnergy >= energySystem.maxEnergy) return;

        // Press '1' to burn a Leaf
        if (Input.GetKeyDown(KeyCode.Alpha1) && leaves > 0)
        {
            leaves--;                                  // Remove 1 leaf from inventory
            energySystem.RestoreEnergy(leafEnergyValue); // Give energy
            UpdateIcons();                             // Update the UI numbers
        }

        // Press '2' to burn a Coal
        if (Input.GetKeyDown(KeyCode.Alpha2) && coal > 0)
        {
            coal--;                                    // Remove 1 coal from inventory
            energySystem.RestoreEnergy(coalEnergyValue); // Give energy
            UpdateIcons();                             // Update the UI numbers
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