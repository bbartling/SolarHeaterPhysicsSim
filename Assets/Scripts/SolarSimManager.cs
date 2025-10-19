using UnityEngine;
using System.Collections.Generic;

public class SolarSimManager : MonoBehaviour
{
    // --- SCENE REFERENCES ---
    [Header("Scene Object References")]
    public Light sunLight;
    public GameObject solarPanel;
    public GameObject storageTank;
    public GameObject pumpImpeller;
    public PipeRendererManager pipeRendererManager;

    // --- SIMULATION PARAMETERS ---
    [Header("System Parameters (Controlled by UI)")]
    [Range(1f, 10f)] public float collectorArea_sqM = 4f;
    [Range(20f, 200f)] public float tankVolume_L = 80f;
    [Range(0f, 1f)] public float sunIntensityFactor = 1f;

    // --- PHYSICS & TIME CONFIGURATION ---
    [Header("Physics & Time Configuration")]
    public float simulationDayLengthSeconds = 1200f;
    private float dayProgress = 0.25f; // Start at 6 AM
    private float timeMultiplier;

    [Header("Live Simulation State")]
    public float ambientTempC = 15f;
    public bool useMetric = false;

    // --- VISUALS ---
    private Material panelMaterialInstance;
    private Material tankMaterialInstance;
    public Gradient tempGradient;

    // --- A reference to our pure physics class ---
    private SolarThermodynamics physicsModel;
    private bool isNight = false;

    void Start()
    {
        if (!ValidateReferences())
        {
            Debug.LogError("Disabling SolarSimManager due to missing references.");
            this.enabled = false;
            return;
        }

        timeMultiplier = 24f * 3600f / simulationDayLengthSeconds;
        panelMaterialInstance = solarPanel.GetComponent<Renderer>().material;
        tankMaterialInstance = storageTank.GetComponent<Renderer>().material;

        // Initialize our new physics model with the starting values
        physicsModel = new SolarThermodynamics(ambientTempC, collectorArea_sqM, tankVolume_L);
    }

    void Update()
    {
        // Pass UI slider values to the physics model
        physicsModel.CollectorArea_sqM = collectorArea_sqM;
        physicsModel.TankVolume_L = tankVolume_L;

        // Update the simulation time and sun position
        UpdateTime();

        // Calculate the current solar power
        float solarIrradiance = isNight ? 0f : 1000f * Mathf.Sin(dayProgress * Mathf.PI) * sunIntensityFactor;

        // Tell the physics model to run one step
        physicsModel.RunPhysicsStep(Time.deltaTime, solarIrradiance, timeMultiplier);

        // Use the results from the physics model to update visuals
        UpdateVisuals();
    }

    void UpdateTime()
    {
        dayProgress += (Time.deltaTime / simulationDayLengthSeconds);
        if (dayProgress >= 1f) dayProgress = 0f;

        float sunAngle = Mathf.Lerp(0, 360, dayProgress);
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0);
        isNight = (sunAngle > 180f);
        sunLight.enabled = !isNight;
    }

    void UpdateVisuals()
    {
        // Animate pump based on the physics model's state
        if (physicsModel.IsPumpOn)
        {
            pumpImpeller.transform.Rotate(0, 0, -200f * Time.deltaTime);
        }

        // Update object colors based on temperatures from the physics model
        panelMaterialInstance.color = tempGradient.Evaluate(Mathf.Clamp01(physicsModel.PanelTempC / 100f));
        tankMaterialInstance.color = tempGradient.Evaluate(Mathf.Clamp01(physicsModel.TankTempC / 100f));

        // Send the live state to the pipe manager
        pipeRendererManager.SetLiveState(physicsModel.PanelTempC, physicsModel.TankTempC, physicsModel.IsPumpOn);
    }

    // --- PUBLIC UI METHODS ---
    public void SimulateLoad(float loadVolumeLiters)
    {
        // Tell the physics model to simulate the load
        physicsModel.SimulateLoad(loadVolumeLiters);
    }

    // --- Helper methods for UI text formatting, now using the physics model for data ---
    public string GetStatusMessage()
    {
        if (isNight) return "Nighttime";
        return physicsModel.IsPumpOn ? "Pump ON - Heating" : "Pump OFF - Standby";
    }

    public string GetFormattedTime()
    {
        int hours = Mathf.FloorToInt(dayProgress * 24f);
        int minutes = Mathf.FloorToInt((dayProgress * 24f * 60f) % 60);
        return $"Time: {hours:00}:{minutes:00}";
    }

    public float CtoF(float celsius) => celsius * 9f / 5f + 32f;

    // We now read temperatures directly from the physics model for the UI Manager
    public float GetPanelTempC() => physicsModel != null ? physicsModel.PanelTempC : 0f;
    public float GetTankTempC() => physicsModel != null ? physicsModel.TankTempC : 0f;

    private bool ValidateReferences()
    {
        var missingRefs = new List<string>();
        if (sunLight == null) missingRefs.Add("Sun Light");
        if (solarPanel == null) missingRefs.Add("Solar Panel");
        if (storageTank == null) missingRefs.Add("Storage Tank");
        if (pumpImpeller == null) missingRefs.Add("Pump Impeller");
        if (pipeRendererManager == null) missingRefs.Add("Pipe Renderer Manager");
        if (tempGradient == null) missingRefs.Add("Temp Gradient");

        if (missingRefs.Count > 0)
        {
            Debug.LogError($"CRITICAL ERROR on '{this.gameObject.name}': The following required references are not assigned in the Inspector: " + string.Join(", ", missingRefs));
            return false;
        }
        return true;
    }
}

