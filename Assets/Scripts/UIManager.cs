using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public Button loadButton;

    [Header("UI Text Fields")]
    public TextMeshProUGUI panelSizeText;
    public TextMeshProUGUI tankSizeText;
    public TextMeshProUGUI sunIntensityText;
    public TextMeshProUGUI tankTempText;
    public TextMeshProUGUI panelTempText;
    public TextMeshProUGUI ambientTempText;
    public TextMeshProUGUI statusText;

    void Start()
    {
        // --- SETUP LISTENERS ---
        panelSizeSlider.onValueChanged.AddListener(OnPanelSliderChanged);
        tankSizeSlider.onValueChanged.AddListener(OnTankSliderChanged);
        sunIntensitySlider.onValueChanged.AddListener(OnSunSliderChanged);
        loadButton.onClick.AddListener(OnLoadButtonClicked);

        InitializeUI();
    }

    void Update()
    {
        UpdateDataDisplays();
    }

    void InitializeUI()
    {
        // Set sliders to match initial sim values
        panelSizeSlider.value = simManager.collectorArea_sqFt;
        tankSizeSlider.value = simManager.tankVolume_gal;
        // Use the new variable name for sun intensity
        sunIntensitySlider.value = simManager.solarIrradiance_BTUhrft2;

        // Update slider text to reflect the default units
        OnPanelSliderChanged(panelSizeSlider.value);
        OnTankSliderChanged(tankSizeSlider.value);
        OnSunSliderChanged(sunIntensitySlider.value);
    }

    // --- SLIDER HANDLERS ---
    public void OnPanelSliderChanged(float value)
    {
        simManager.collectorArea_sqFt = value;
        panelSizeText.text = $"Panel Area: {value:F0} ft²";
    }

    public void OnTankSliderChanged(float value)
    {
        simManager.tankVolume_gal = value;
        tankSizeText.text = $"Tank Volume: {value:F0} gal";
    }

    public void OnSunSliderChanged(float value)
    {
        // Use the new variable name and update the text to show the correct units
        simManager.solarIrradiance_BTUhrft2 = value;
        sunIntensityText.text = $"Sun Intensity: {value:F0} BTU/hr·ft²";
    }


    // --- BUTTON HANDLERS ---
    public void OnLoadButtonClicked()
    {
        simManager.SimulateLoad(10f); // Simulate a 10 Gallon load
    }

    // --- DATA DISPLAY UPDATER ---
    void UpdateDataDisplays()
    {
        statusText.text = $"Status: {simManager.GetStatusMessage()}";

        // Display temperatures in Imperial units
        tankTempText.text = $"Tank Temp: {simManager.GetTankTempF():F1}°F";
        panelTempText.text = $"Panel Temp: {simManager.GetPanelTempF():F1}°F";
        ambientTempText.text = $"Ambient: {simManager.ambientTempF:F1}°F";
    }
}

