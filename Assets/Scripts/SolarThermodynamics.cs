// SolarThermodynamics.cs  (non-MonoBehaviour)
using UnityEngine;

public class SolarThermodynamics
{
    public float A = 4f;       // collector area (m²)
    public float V = 80f;      // tank volume (L ≈ kg)
    public float eta = 0.70f;  // flat efficiency
    public float Up = 4f;      // panel UA (W/K)
    public float Ut = 4f;      // tank UA (W/K)
    public float mDot = 0.05f; // kg/s when pump ON
    public float setpointC = 55f;
    public float dTon = 8f, dToff = 3f;

    // State
    public float Ta = 15f;     // ambient
    public float Tp, Tt;       // panel, tank
    public bool pump;

    const float Cp = 4186f;        // J/(kg·K)
    const float Cp_panel = 15000f; // J/K

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

        float Qin = eta * G * A;
        float Qloss = Up * (Tp - Ta);
        float QnetP = Qin - Mathf.Max(0f, Qloss) - Q_toTank;
        Tp += (QnetP * dts) / Mathf.Max(1f, Cp_panel);
        Tp = Mathf.Max(Ta, Tp);

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
