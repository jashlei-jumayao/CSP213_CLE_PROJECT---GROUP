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

    [Header("Audio")]
    public AudioClip burnSound;
    [Range(0f, 1f)]
    public float burnVolume = 1f;
    [Tooltip("Optional: Assign a dedicated AudioSource, or leave empty to create one automatically.")]
    public AudioSource burnAudioSource;

    private RobotEnergy energySystem;

    private void Start()
    {
        energySystem = GetComponent<RobotEnergy>();

        if (burnAudioSource == null)
        {
            burnAudioSource = gameObject.AddComponent<AudioSource>();
            burnAudioSource.playOnAwake = false;
            burnAudioSource.loop = false;
            burnAudioSource.spatialBlend = 0f;
        }

        UpdateIcons();
    }

    private void Update()
    {
        if (energySystem == null) return;
        if (energySystem.currentEnergy >= energySystem.maxEnergy) return;

        if (Input.GetKeyDown(KeyCode.Alpha1) && leaves > 0)
        {
            // Only consume the leaf if the energy system accepts it
            if (energySystem.RestoreEnergy(leafEnergyValue))
            {
                leaves--;
                UpdateIcons();
                PlayBurnSound();
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) && coal > 0)
        {
            // Only consume the coal if the energy system accepts it
            if (energySystem.RestoreEnergy(coalEnergyValue))
            {
                coal--;
                UpdateIcons();
                PlayBurnSound();
            }
        }
    }

    private void PlayBurnSound()
    {
        if (burnSound == null || burnAudioSource == null) return;

        // Only start playing if the previous burn sound has completely finished
        if (!burnAudioSource.isPlaying)
        {
            burnAudioSource.clip = burnSound;
            burnAudioSource.volume = burnVolume;
            burnAudioSource.Play();
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