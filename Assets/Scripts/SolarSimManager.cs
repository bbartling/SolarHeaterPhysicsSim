using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class SolarSimManager : MonoBehaviour
{
    // --- SCENE REFERENCES ---
    [Header("Scene Object References")]
    public Light sunLight;
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

    // --- NEW variables for visual scaling ---
    private Vector3 initialPanelScale;
    private Vector3 initialTankScale;
    private float initialPanelArea;
    private float initialTankVolume;

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

        // --- Store initial values for scaling ---
        initialPanelScale = solarPanel.transform.localScale;
        initialTankScale = storageTank.transform.localScale;
        initialPanelArea = collectorArea_sqFt;
        initialTankVolume = tankVolume_gal;

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
        // --- Safely update the sun light intensity ---
        if (sunLight != null)
        {
            // Normalize the 0-350 BTU range to a 0-1 range for light intensity
            float normalizedIntensity = solarIrradiance_BTUhrft2 / 350f;
            sunLight.intensity = normalizedIntensity;
        }

        // --- NEW: Update object scales based on sliders ---
        UpdateObjectScales();

        // --- Existing Pump Animation and Sound Logic ---
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

    // --- NEW METHOD for scaling objects ---
    void UpdateObjectScales()
    {
        // Scale the panel's area (X and Z axes)
        float panelAreaRatio = collectorArea_sqFt / initialPanelArea;
        float panelScaleRatio = Mathf.Sqrt(panelAreaRatio); // Use Sqrt for area
        solarPanel.transform.localScale = new Vector3(initialPanelScale.x * panelScaleRatio, initialPanelScale.y, initialPanelScale.z * panelScaleRatio);

        // Scale the tank's volume (all axes uniformly)
        float tankVolumeRatio = tankVolume_gal / initialTankVolume;
        float tankScaleRatio = Mathf.Pow(tankVolumeRatio, 1f / 3f); // Use Cube Root for volume
        storageTank.transform.localScale = initialTankScale * tankScaleRatio;
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
        if (sunLight == null) missingRefs.Add("Sun Light");
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

