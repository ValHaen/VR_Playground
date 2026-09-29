using UnityEngine;
using UnityEngine.UI;
using FMODUnity;

public class FMODAudioManager : MonoBehaviour
{
    [Header("FMOD Settings")]
    public string parameterName = "ReverbWetLevel";

    [Header("UI References")]
    public Slider reverbSlider;
    public Toggle reverbToggle;

    private float lastSliderValue = 0.5f; // Standardmäßig in der Mitte (0.5)

    void Start()
    {
        if (reverbSlider != null)
        {
            // Slider auf 0.0 bis 1.0 festlegen
            reverbSlider.minValue = 0f;
            reverbSlider.maxValue = 1f;
            
            // Startwert vom Slider übernehmen
            lastSliderValue = reverbSlider.value;
            
            reverbSlider.onValueChanged.AddListener(OnSliderChanged);
        }

        if (reverbToggle != null)
        {
            reverbToggle.onValueChanged.AddListener(OnToggleChanged);
            // Initialen FMOD-Wert setzen
            UpdateFMODParameter(reverbToggle.isOn ? lastSliderValue : 0f);
        }
    }

    void OnSliderChanged(float value)
    {
        lastSliderValue = value;
        // Nur senden, wenn Reverb laut Toggle aktiv ist
        if (reverbToggle == null || reverbToggle.isOn)
        {
            UpdateFMODParameter(value);
        }
    }

    void OnToggleChanged(bool isOn)
    {
        // Bei AN -> Sliderwert, bei AUS -> 0.0
        UpdateFMODParameter(isOn ? lastSliderValue : 0f);
    }

    void UpdateFMODParameter(float value)
    {
        if (FMODUnity.RuntimeManager.StudioSystem.isValid())
        {
            FMOD.RESULT result = FMODUnity.RuntimeManager.StudioSystem.setParameterByName(parameterName, value);
            
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogWarning($"FMOD: Fehler bei {parameterName}: {result}");
            }
            else
            {
                Debug.Log($"FMOD: {parameterName} auf {value} gesetzt.");
            }
        }
    }
}