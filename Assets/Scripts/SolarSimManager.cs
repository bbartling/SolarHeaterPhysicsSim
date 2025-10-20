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
    public PipeRendererManager pipeRendererManager;

    // --- AUDIO REFERENCE ---
    [Header("Audio")]
    public AudioClip pumpSoundEffect;
    private AudioSource audioSource;


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
    public bool useMetric = false; // Default to Imperial

    // --- VISUALS ---
    private Material panelMaterialInstance;
    private Material tankMaterialInstance;
    public Gradient tempGradient;

    // --- A reference to our pure physics class ---
    private SolarThermodynamics physicsModel;
    private bool isNight = false;

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

        timeMultiplier = 24f * 3600f / simulationDayLengthSeconds;
        panelMaterialInstance = solarPanel.GetComponent<Renderer>().material;
        tankMaterialInstance = storageTank.GetComponent<Renderer>().material;

        physicsModel = new SolarThermodynamics(ambientTempC, collectorArea_sqM, tankVolume_L);
    }

    void Update()
    {
        physicsModel.CollectorArea_sqM = collectorArea_sqM;
        physicsModel.TankVolume_L = tankVolume_L;

        UpdateTime();

        float solarIrradiance = isNight ? 0f : 1000f * Mathf.Sin(dayProgress * Mathf.PI) * sunIntensityFactor;

        physicsModel.RunPhysicsStep(Time.deltaTime, solarIrradiance, timeMultiplier, ambientTempC);

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

        panelMaterialInstance.color = tempGradient.Evaluate(Mathf.Clamp01(physicsModel.PanelTempC / 100f));
        tankMaterialInstance.color = tempGradient.Evaluate(Mathf.Clamp01(physicsModel.TankTempC / 100f));

        pipeRendererManager.SetLiveState(physicsModel.PanelTempC, physicsModel.TankTempC, physicsModel.IsPumpOn);
    }

    public void SimulateLoad(float loadVolumeLiters)
    {
        physicsModel.SimulateLoad(loadVolumeLiters);
    }

    public string GetStatusMessage()
    {
        if (isNight) return "Nighttime";
        return physicsModel.IsPumpOn ? "Pump ON - Heating" : "Pump OFF - Standby";
    }

    // Helper function for converting Celsius to Fahrenheit
    public float CtoF(float celsius) => celsius * 9f / 5f + 32f;

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
        if (pumpSoundEffect == null) missingRefs.Add("Pump Sound Effect");

        if (missingRefs.Count > 0)
        {
            Debug.LogError($"CRITICAL ERROR on '{this.gameObject.name}': The following required references are not assigned in the Inspector: " + string.Join(", ", missingRefs));
            return false;
        }
        return true;
    }
}