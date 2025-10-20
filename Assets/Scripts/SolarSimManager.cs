using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class SolarSimManager : MonoBehaviour
{
    // --- SCENE REFERENCES ---
    [Header("Scene Object References")]
    public GameObject solarPanel;
    public GameObject storageTank;
    public GameObject pumpImpeller;

    // --- AUDIO REFERENCE ---
    [Header("Audio")]
    public AudioClip pumpSoundEffect;
    private AudioSource audioSource;

    // --- SIMULATION PARAMETERS (now in Imperial) ---
    [Header("System Parameters (Controlled by UI)")]
    [Range(10f, 100f)] public float collectorArea_sqFt = 40f;
    [Range(10f, 50f)] public float tankVolume_gal = 20f;
    [Tooltip("Solar Irradiance in British Thermal Units per hour per square foot.")]
    [Range(0f, 350f)] public float solarIrradiance_BTUhrft2 = 315f;

    [Header("Live Simulation State")]
    public float ambientTempF = 60f;

    // --- VISUALS ---
    private Material panelMaterialInstance;
    private Material tankMaterialInstance;
    public Gradient tempGradient;

    // --- A reference to our pure physics class ---
    private SolarThermodynamics physicsModel;

    void Start()
    {
        // --- SETUP AUDIO ---
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = pumpSoundEffect;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        if (!ValidateReferences())
        {
            Debug.LogError("Disabling SolarSimManager due to missing references.");
            this.enabled = false;
            return;
        }

        panelMaterialInstance = solarPanel.GetComponent<Renderer>().material;
        tankMaterialInstance = storageTank.GetComponent<Renderer>().material;

        // Initialize the physics model with converted metric units
        physicsModel = new SolarThermodynamics(FtoC(ambientTempF), SqFtToSqM(collectorArea_sqFt), GalToL(tankVolume_gal));
    }

    void Update()
    {
        // Convert from Imperial public variables to metric for the physics model
        physicsModel.CollectorArea_sqM = SqFtToSqM(collectorArea_sqFt);
        physicsModel.TankVolume_L = GalToL(tankVolume_gal);

        // Convert the irradiance from Imperial to Metric for the physics engine
        float constantHeatInput_Wm2 = BTUhrft2_to_Wm2(solarIrradiance_BTUhrft2);

        // Run the physics step using metric units
        physicsModel.RunPhysicsStep(Time.deltaTime, constantHeatInput_Wm2, 1f, FtoC(ambientTempF));

        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (physicsModel.IsPumpOn)
        {
            pumpImpeller.transform.Rotate(0, 0, -200f * Time.deltaTime);
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        // Visuals are still driven by the internal Celsius temperatures
        panelMaterialInstance.color = tempGradient.Evaluate(Mathf.Clamp01(physicsModel.PanelTempC / 100f));
        tankMaterialInstance.color = tempGradient.Evaluate(Mathf.Clamp01(physicsModel.TankTempC / 100f));
    }

    // This method now accepts gallons and converts internally
    public void SimulateLoad(float loadVolumeGallons)
    {
        physicsModel.SimulateLoad(GalToL(loadVolumeGallons));
    }

    public string GetStatusMessage()
    {
        return physicsModel.IsPumpOn ? "Pump ON - Heating" : "Pump OFF - Standby";
    }

    // --- PUBLIC TEMPERATURE GETTERS (return Fahrenheit) ---
    public float GetPanelTempF() => CtoF(physicsModel.PanelTempC);
    public float GetTankTempF() => CtoF(physicsModel.TankTempC);

    // --- UNIT CONVERSION HELPERS ---
    private float CtoF(float celsius) => celsius * 9f / 5f + 32f;
    private float FtoC(float fahrenheit) => (fahrenheit - 32f) * 5f / 9f;
    private float GalToL(float gallons) => gallons * 3.78541f;
    private float SqFtToSqM(float sqFt) => sqFt / 10.764f;
    private float BTUhrft2_to_Wm2(float btu_hr_ft2) => btu_hr_ft2 * 3.15459f;


    private bool ValidateReferences()
    {
        var missingRefs = new List<string>();
        if (solarPanel == null) missingRefs.Add("Solar Panel");
        if (storageTank == null) missingRefs.Add("Storage Tank");
        if (pumpImpeller == null) missingRefs.Add("Pump Impeller");
        if (tempGradient == null) missingRefs.Add("Temp Gradient");
        if (pumpSoundEffect == null) missingRefs.Add("Pump Sound Effect");

        if (missingRefs.Count > 0)
        {
            Debug.LogError($"CRITICAL ERROR on '{this.gameObject.name}': The following required references are not assigned in the Inspector: " + string.Join(", ", missingRefs));
            return false;
        }
        return true;
    }
}

