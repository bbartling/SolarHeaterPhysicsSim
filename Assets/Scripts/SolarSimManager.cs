using UnityEngine;

public class SolarSimManager : MonoBehaviour
{
    // --- SCENE REFERENCES ---
    [Header("Scene Object References")]
    public Light sunLight;
    public GameObject solarPanel;
    public GameObject storageTank;
    public GameObject pumpImpeller;
    public LineRenderer hotPipeRenderer;
    public LineRenderer coldPipeRenderer;

    // --- SIMULATION PARAMETERS (ADJUSTABLE BY UI) ---
    [Header("System Parameters (Controlled by UI)")]
    [Range(1f, 10f)]
    public float collectorArea_sqM = 4f; // in square meters
    [Range(20f, 200f)]
    public float tankVolume_L = 80f; // in Liters
    [Range(0f, 1f)]
    public float sunIntensityFactor = 1f; // 0 = cloudy, 1 = full sun

    // --- PHYSICS & TIME CONSTANTS ---
    [Header("Physics & Time Configuration")]
    public float simulationDayLengthSeconds = 1200f; // 20 minutes for a 24h day
    private float dayProgress = 0.25f; // Start at 6 AM
    private float timeMultiplier;

    // --- TEMPERATURES (in Celsius) ---
    [Header("Live Simulation State (Celsius)")]
    public float ambientTempC = 15f;
    public float panelTempC = 15f;
    public float tankTempC = 15f;

    // --- SYSTEM STATE ---
    private bool isPumpOn = false;
    private bool isNight = false;
    public bool useMetric = false; // Toggled by UI

    // --- MATERIALS for VISUALS ---
    private Material panelMaterialInstance;
    private Material tankMaterialInstance;
    private Material hotPipeMaterialInstance;
    private Material coldPipeMaterialInstance;

    // Color gradient for temperature visualization
    public Gradient tempGradient;

    void Start()
    {
        // Calculate the speed of our simulation time
        timeMultiplier = 24f * 3600f / simulationDayLengthSeconds;

        // Create instances of materials so we don't change the project assets
        panelMaterialInstance = solarPanel.GetComponent<Renderer>().material;
        tankMaterialInstance = storageTank.GetComponent<Renderer>().material;
        hotPipeMaterialInstance = hotPipeRenderer.material;
        coldPipeMaterialInstance = coldPipeRenderer.material;
    }

    void Update()
    {
        // 1. UPDATE TIME
        UpdateTime();

        // 2. RUN SIMULATION PHYSICS
        RunPhysicsStep(Time.deltaTime);

        // 3. UPDATE VISUALS
        UpdateVisuals();
    }

    void UpdateTime()
    {
        dayProgress += (Time.deltaTime / simulationDayLengthSeconds);
        if (dayProgress >= 1f)
        {
            dayProgress = 0f;
        }

        // Rotate the sun
        // A simple sine wave motion across the sky
        float sunAngle = Mathf.Lerp(0, 360, dayProgress);
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0);

        // Check if it's night
        // The sun is "down" when its angle is between 180 and 360 degrees
        isNight = (sunAngle > 180f);
        sunLight.enabled = !isNight;
    }

    void RunPhysicsStep(float deltaTime)
    {
        // --- SOLAR GAIN CALCULATION ---
        float solarIrradiance = 0f;
        if (!isNight)
        {
            // Simulate peak intensity at noon (dayProgress = 0.5)
            float noonFactor = Mathf.Sin(dayProgress * Mathf.PI); // This gives a nice curve from 0 to 1 and back to 0
            solarIrradiance = 1000f * noonFactor * sunIntensityFactor; // Max 1000 W/m^2
        }

        // Energy absorbed by panel (Simplified)
        // Q_in = Irradiance * Area * Efficiency
        float efficiency = 0.7f; // Assume 70% efficiency
        float powerIn = solarIrradiance * collectorArea_sqM * efficiency;

        // Heat loss from panel to ambient
        // Q_out = U_value * Area * (T_panel - T_ambient)
        float panelLossCoeff = 5f; // W/(m^2 * C)
        float powerLossPanel = panelLossCoeff * collectorArea_sqM * (panelTempC - ambientTempC);

        // Net change in panel energy
        float netPanelPower = powerIn - powerLossPanel;
        panelTempC += netPanelPower * deltaTime / (collectorArea_sqM * 5f); // Simplified thermal mass

        // Clamp temperature to something reasonable
        panelTempC = Mathf.Max(panelTempC, ambientTempC);


        // --- PUMP LOGIC ---
        // Turn pump ON if panel is 8C hotter than tank
        if (!isPumpOn && panelTempC > tankTempC + 8f && !isNight)
        {
            isPumpOn = true;
        }
        // Turn pump OFF if panel is less than 2C hotter than tank
        else if (isPumpOn && panelTempC < tankTempC + 2f)
        {
            isPumpOn = false;
        }


        // --- TANK TEMPERATURE CALCULATION ---
        float waterMass = tankVolume_L; // 1L of water is ~1kg
        float specificHeatWater = 4186f; // J/(kg*C)

        // Heat transfer from panel to tank (only when pump is on)
        if (isPumpOn)
        {
            float transferEfficiency = 0.9f;
            float transferPower = (panelTempC - tankTempC) * 40f * transferEfficiency; // Simplified heat transfer rate
            float deltaTempTank = (transferPower * deltaTime) / (waterMass * specificHeatWater);
            tankTempC += deltaTempTank * timeMultiplier; // Apply time multiplier here for faster change
        }

        // Heat loss from tank to ambient
        float tankLossCoeff = 1.5f; // W/(m^2*C)
        float tankSurfaceArea = Mathf.Pow(tankVolume_L / 1000f, 2f / 3f) * 4.8f; // Cube root approximation for surface area
        float powerLossTank = tankLossCoeff * tankSurfaceArea * (tankTempC - ambientTempC);
        float deltaLossTank = (powerLossTank * deltaTime) / (waterMass * specificHeatWater);
        tankTempC -= deltaLossTank * timeMultiplier;

        // Ensure tank temp doesn't drop below ambient
        tankTempC = Mathf.Max(tankTempC, ambientTempC);
    }

    void UpdateVisuals()
    {
        // Animate pump
        if (isPumpOn)
        {
            pumpImpeller.transform.Rotate(0, 0, -200f * Time.deltaTime);
        }

        // Update colors based on temperature (0C to 100C range)
        panelMaterialInstance.color = tempGradient.Evaluate(Mathf.Clamp01(panelTempC / 100f));
        tankMaterialInstance.color = tempGradient.Evaluate(Mathf.Clamp01(tankTempC / 100f));

        if (isPumpOn)
        {
            // When pump is on, hot pipe is panel temp, cold pipe is tank temp
            hotPipeMaterialInstance.color = tempGradient.Evaluate(Mathf.Clamp01(panelTempC / 100f));
            coldPipeMaterialInstance.color = tempGradient.Evaluate(Mathf.Clamp01(tankTempC / 100f));
        }
        else
        {
            // When off, pipes slowly cool to ambient
            hotPipeMaterialInstance.color = Color.Lerp(hotPipeMaterialInstance.color, tempGradient.Evaluate(Mathf.Clamp01(ambientTempC / 100f)), Time.deltaTime);
            coldPipeMaterialInstance.color = Color.Lerp(coldPipeMaterialInstance.color, tempGradient.Evaluate(Mathf.Clamp01(ambientTempC / 100f)), Time.deltaTime);
        }
    }

    // --- PUBLIC METHODS FOR UI ---

    public void SimulateLoad(float loadVolumeLiters)
    {
        // A "load" means we use some hot water, which is replaced by cold mains water.
        float mainsWaterTemp = 10f; // Assume 10C
        float fractionUsed = loadVolumeLiters / tankVolume_L;

        // New temperature is a weighted average of remaining hot water and new cold water
        tankTempC = (tankTempC * (1 - fractionUsed)) + (mainsWaterTemp * fractionUsed);
        Debug.Log($"Simulated load. New tank temp: {tankTempC}°C");
    }

    public string GetStatusMessage()
    {
        if (isNight) return "Nighttime";
        if (isPumpOn) return "Pump ON - Heating";
        return "Pump OFF - Standby";
    }

    public string GetFormattedTime()
    {
        int hours = Mathf.FloorToInt(dayProgress * 24f);
        int minutes = Mathf.FloorToInt((dayProgress * 24f * 60f) % 60);
        return $"Time: {hours:00}:{minutes:00}";
    }

    // Helper function to convert Celsius to Fahrenheit
    public float CtoF(float celsius)
    {
        return celsius * 9f / 5f + 32f;
    }
}
