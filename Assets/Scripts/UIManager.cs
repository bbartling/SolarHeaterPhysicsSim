using UnityEngine;
using UnityEngine.UI;
using TMPro; // Make sure to import TextMeshPro

public class UIManager : MonoBehaviour
{
    // --- REFERENCES ---
    [Header("Simulation Manager")]
    public SolarSimManager simManager;

    [Header("UI Sliders")]
    public Slider panelSizeSlider;
    public Slider tankSizeSlider;
    public Slider sunIntensitySlider;

    [Header("UI Buttons")]
    public Button unitToggleButton;
    public Button loadButton;

    [Header("UI Text Fields")]
    public TextMeshProUGUI panelSizeText;
    public TextMeshProUGUI tankSizeText;
    public TextMeshProUGUI sunIntensityText;
    public TextMeshProUGUI unitButtonText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI tankTempText;
    public TextMeshProUGUI panelTempText;
    public TextMeshProUGUI ambientTempText;
    public TextMeshProUGUI statusText;

    void Start()
    {
        // --- SETUP SLIDER LISTENERS ---
        // This makes a method run every time a slider's value changes.
        panelSizeSlider.onValueChanged.AddListener(OnPanelSliderChanged);
        tankSizeSlider.onValueChanged.AddListener(OnTankSliderChanged);
        sunIntensitySlider.onValueChanged.AddListener(OnSunSliderChanged);

        // --- SETUP BUTTON LISTENERS (Also done in Inspector, but can be done here too) ---
        // unitToggleButton.onClick.AddListener(OnUnitToggleClicked);
        // loadButton.onClick.AddListener(OnLoadButtonClicked);

        // Initialize UI with default values
        InitializeUI();
    }

    void Update()
    {
        // Update all the text fields every frame with the latest data from the simulation
        UpdateDataDisplays();
    }

    void InitializeUI()
    {
        // Set sliders to match the initial values in the sim manager
        panelSizeSlider.value = simManager.collectorArea_sqM;
        tankSizeSlider.value = simManager.tankVolume_L;
        sunIntensitySlider.value = simManager.sunIntensityFactor;

        // Update the text next to the sliders
        OnPanelSliderChanged(panelSizeSlider.value);
        OnTankSliderChanged(tankSizeSlider.value);
        OnSunSliderChanged(sunIntensitySlider.value);
    }

    // --- SLIDER HANDLERS ---
    public void OnPanelSliderChanged(float value)
    {
        simManager.collectorArea_sqM = value;
        if (simManager.useMetric)
        {
            panelSizeText.text = $"Panel Area: {value:F1} m²";
        }
        else
        {
            float area_sqFt = value * 10.764f;
            panelSizeText.text = $"Panel Area: {area_sqFt:F0} ft²";
        }
    }

    public void OnTankSliderChanged(float value)
    {
        simManager.tankVolume_L = value;
        if (simManager.useMetric)
        {
            tankSizeText.text = $"Tank Volume: {value:F0} L";
        }
        else
        {
            float vol_gal = value * 0.264172f;
            tankSizeText.text = $"Tank Volume: {vol_gal:F0} gal";
        }
    }

    public void OnSunSliderChanged(float value)
    {
        simManager.sunIntensityFactor = value;
        sunIntensityText.text = $"Sun Intensity: {(value * 100):F0}%";
    }


    // --- BUTTON HANDLERS ---
    public void OnUnitToggleClicked()
    {
        simManager.useMetric = !simManager.useMetric;
        unitButtonText.text = simManager.useMetric ? "Switch to Imperial" : "Switch to Metric";

        // Refresh slider text to reflect the new units
        OnPanelSliderChanged(panelSizeSlider.value);
        OnTankSliderChanged(tankSizeSlider.value);
    }

    public void OnLoadButtonClicked()
    {
        // Simulate a 40 Liter load (a typical shower)
        simManager.SimulateLoad(40f);
    }

    // --- DATA DISPLAY UPDATER ---
    void UpdateDataDisplays()
    {
        timeText.text = simManager.GetFormattedTime();
        statusText.text = $"Status: {simManager.GetStatusMessage()}";

        // Display temperatures in the selected unit
        if (simManager.useMetric)
        {
            tankTempText.text = $"Tank Temp: {simManager.tankTempC:F1}°C";
            panelTempText.text = $"Panel Temp: {simManager.panelTempC:F1}°C";
            ambientTempText.text = $"Ambient: {simManager.ambientTempC:F1}°C";
        }
        else // Imperial
        {
            tankTempText.text = $"Tank Temp: {simManager.CtoF(simManager.tankTempC):F1}°F";
            panelTempText.text = $"Panel Temp: {simManager.CtoF(simManager.panelTempC):F1}°F";
            ambientTempText.text = $"Ambient: {simManager.CtoF(simManager.ambientTempC):F1}°F";
        }
    }
}
