using UnityEngine; // Required for Mathf functions

// This is a standard C# class, not a MonoBehaviour.
// It contains all the core thermodynamic calculations for the simulation.
public class SolarThermodynamics
{
    // --- System Parameters ---
    public float CollectorArea_sqM { get; set; }
    public float TankVolume_L { get; set; }

    // --- Live State Variables ---
    public float AmbientTempC { get; private set; }
    public float PanelTempC { get; private set; }
    public float TankTempC { get; private set; }
    public bool IsPumpOn { get; private set; }

    // Constructor to initialize the system
    public SolarThermodynamics(float initialTempC, float collectorArea, float tankVolume)
    {
        AmbientTempC = initialTempC;
        PanelTempC = initialTempC;
        TankTempC = initialTempC;
        CollectorArea_sqM = collectorArea;
        TankVolume_L = tankVolume;
        IsPumpOn = false;
    }

    // This method runs one step of the physics simulation.
    public void RunPhysicsStep(float deltaTime, float solarIrradiance, float timeMultiplier)
    {
        // --- Solar Gain Calculation ---
        // Power absorbed by the panel, considering a 70% efficiency factor.
        float powerIn = solarIrradiance * CollectorArea_sqM * 0.7f;
        // Power lost from the panel to the environment.
        float powerLossPanel = 5f * CollectorArea_sqM * (PanelTempC - AmbientTempC);
        // Net power determines temperature change.
        float netPanelPower = powerIn - powerLossPanel;
        // Update panel temperature based on its thermal mass (approximated).
        PanelTempC += netPanelPower * deltaTime / (CollectorArea_sqM * 5f);
        PanelTempC = Mathf.Max(PanelTempC, AmbientTempC); // Can't get colder than ambient.

        // --- Pump Logic ---
        // Turn pump on if the panel is significantly hotter than the tank.
        if (!IsPumpOn && PanelTempC > TankTempC + 8f && solarIrradiance > 0) IsPumpOn = true;
        // Turn pump off if the temperature difference is too small.
        else if (IsPumpOn && PanelTempC < TankTempC + 2f) IsPumpOn = false;

        // --- Tank Temperature Calculation ---
        float waterMass = TankVolume_L; // 1L of water has a mass of ~1kg.
        float specificHeatWater = 4186f; // Joules per kg per degree Celsius.

        // If the pump is on, transfer heat from the panel to the tank.
        if (IsPumpOn)
        {
            float transferPower = (PanelTempC - TankTempC) * 40f * 0.9f; // Heat transfer coefficient and efficiency.
            float deltaTempTank = (transferPower * deltaTime) / (waterMass * specificHeatWater);
            TankTempC += deltaTempTank * timeMultiplier;
        }

        // Tank loses heat to the environment through its surface area.
        float tankSurfaceArea = Mathf.Pow(TankVolume_L / 1000f, 2f / 3f) * 4.8f; // Approximate surface area.
        float powerLossTank = 1.5f * tankSurfaceArea * (TankTempC - AmbientTempC);
        float deltaLossTank = (powerLossTank * deltaTime) / (waterMass * specificHeatWater);
        TankTempC -= deltaLossTank * timeMultiplier;
        TankTempC = Mathf.Max(TankTempC, AmbientTempC); // Can't get colder than ambient.
    }

    // --- Public Method for Simulating a Load ---
    public void SimulateLoad(float loadVolumeLiters)
    {
        float mainsWaterTemp = 10f; // Assume cold water is 10°C.
        float fractionUsed = loadVolumeLiters / TankVolume_L;
        // New temperature is a weighted average of the remaining hot water and the new cold water.
        TankTempC = (TankTempC * (1 - fractionUsed)) + (mainsWaterTemp * fractionUsed);
    }
}

