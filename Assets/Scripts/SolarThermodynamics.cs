// SolarThermodynamics.cs  (non-MonoBehaviour)
using UnityEngine;

public class SolarThermodynamics
{
    // --- System Design Parameters ---

        
        // The surface area of the solar collector panel, in square meters (m²).
        public float A = 4f;

        // The total volume of the hot water storage tank, in Liters (L).
        public float V = 80f;

        // The efficiency of the solar panel in converting solar energy to heat (0.0 to 1.0).
        public float eta = 0.70f;

        // The heat loss coefficient of the panel (in Watts per Kelvin), representing insulation quality.
        public float Up = 4f;

        // The heat loss coefficient of the tank (in Watts per Kelvin), representing insulation quality.
        public float Ut = 4f;

        // The mass flow rate of water moved by the pump when active, in kilograms per second (kg/s).
        public float mDot = 0.05f;


        // --- Control Logic Parameters ---
        // The target temperature for the water in the tank, in Celsius. The pump will turn off if this is reached.
        public float setpointC = 55f;

        // The temperature difference (Panel > Tank) required to turn the pump ON.
        public float dTon = 8f;

        
        // The temperature difference (Panel > Tank) at which the pump will turn OFF.
        public float dToff = 3f;

        // --- Live State Variables ---

        // The current ambient (outside) temperature, in Celsius.
        public float Ta = 15f;

        // The current temperature of the solar panel, in Celsius.
        public float Tp;

        // The current temperature of the water tank, in Celsius.
        public float Tt;
        
        // The current state of the circulation pump (true = ON, false = OFF).
        public bool pump;

        // --- Physical Constants ---

        // The specific heat capacity of water, in Joules per kilogram Kelvin (J/kg·K).
        const float Cp = 4186f;

        // The total thermal mass of the solar panel itself, in Joules per Kelvin (J/K).
        const float Cp_panel = 15000f;

    public SolarThermodynamics(float TinitC, float area, float volumeL)
    { Tp = TinitC; Tt = TinitC; Ta = TinitC; A = area; V = volumeL; }

    // ---- core step ----
    public void Step(float dt, float G, float timeMult)
    {
        float dts = Mathf.Max(1e-3f, dt * timeMult);

        bool tankSatisfied = (Tt >= setpointC);
        if (!pump && !tankSatisfied && (Tp >= Tt + dTon) && G > 10f) pump = true;
        if (pump && (tankSatisfied || Tp <= Tt + dToff || G <= 10f)) pump = false;

        float Q_toTank = pump ? mDot * Cp * Mathf.Max(0f, Tp - Tt) : 0f;

        // --- Panel Energy Balance ---
        // The panel's temperature changes based on energy gained from the sun (Qin),
        // minus energy lost to the environment (Qloss), minus energy transferred to the tank (Q_toTank).
        float Qin = eta * G * A;
        float Qloss = Up * (Tp - Ta);
        float QnetP = Qin - Mathf.Max(0f, Qloss) - Q_toTank;
        Tp += (QnetP * dts) / Mathf.Max(1f, Cp_panel);
        Tp = Mathf.Max(Ta, Tp);

        // --- Tank Energy Balance ---
        // The tank's temperature changes based on energy received from the panel (Q_toTank),
        // minus its own heat loss to the environment (QlossT).
        float mw = Mathf.Max(1f, V);
        float QlossT = Ut * (Tt - Ta);
        float QnetT = Q_toTank - Mathf.Max(0f, QlossT);
        Tt += (QnetT * dts) / (mw * Cp);
        Tt = Mathf.Max(Ta, Tt);
    }

    public void RunPhysicsStep(float dt, float G, float timeMult, float ambientC)
    {
        Ta = ambientC;
        Step(dt, G, timeMult);
    }

    public float PanelTempC => Tp;
    public float TankTempC => Tt;
    public bool IsPumpOn => pump;

    public void SimulateLoad(float liters)
    {
        float cold = 10f;
        float f = Mathf.Clamp01(liters / Mathf.Max(1f, V));
        Tt = Tt * (1f - f) + cold * f;
    }

    public float CollectorArea_sqM
    {
        get => A;
        set => A = value;
    }
    public float TankVolume_L
    {
        get => V;
        set => V = value;
    }

}
